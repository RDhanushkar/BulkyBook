using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCardVM ShoppingCardVM { get; set; }
        public int OrderTotal { get; set; }
        public CardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        //[AllowAnonymous] it can be access by Un Authorize use(coudn't login)
        public IActionResult Index()
        {
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCardVM = new ShoppingCardVM
            {
                ListCard = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product"),
                //facific users shpping item must be fiters for this function 
                //we could change getall() method into filter
                OrderHeader = new()
            };
            foreach(var card in ShoppingCardVM.ListCard)
            {
                card.Price = GetPriceBasedOnQuantity(card.Count, card.Product.Price,
                    card.Product.Price50, card.Product.Price100);
                ShoppingCardVM.OrderHeader.OrderTotal += (card.Price * card.Count);
            }
			return View(ShoppingCardVM);
        }

		public IActionResult Summary()
		{
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCardVM = new ShoppingCardVM
            {
                ListCard = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product"),
                OrderHeader = new()

            };
            ShoppingCardVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(
                u => u.Id == claim.Value);
            ShoppingCardVM.OrderHeader.Name = ShoppingCardVM.OrderHeader.ApplicationUser.Name;
            ShoppingCardVM.OrderHeader.PhoneNumber = ShoppingCardVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCardVM.OrderHeader.StreetAddress = ShoppingCardVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCardVM.OrderHeader.City = ShoppingCardVM.OrderHeader.ApplicationUser.City;
            ShoppingCardVM.OrderHeader.State = ShoppingCardVM.OrderHeader.ApplicationUser.State;
            ShoppingCardVM.OrderHeader.PostalCode = ShoppingCardVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var card in ShoppingCardVM.ListCard)
            {
                card.Price = GetPriceBasedOnQuantity(card.Count, card.Product.Price,
                    card.Product.Price50, card.Product.Price100);
                ShoppingCardVM.OrderHeader.OrderTotal += (card.Price * card.Count);
            }
            return View(ShoppingCardVM);
            return View();
		}

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
		public IActionResult SummaryPOST(ShoppingCardVM ShoppingCardVM)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCardVM.ListCard = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product");

            ShoppingCardVM.OrderHeader.OrderDate = System.DateTime.Now;
            ShoppingCardVM.OrderHeader.ApplicationUserId = claim.Value;

			foreach (var card in ShoppingCardVM.ListCard)
			{
				card.Price = GetPriceBasedOnQuantity(card.Count, card.Product.Price,
					card.Product.Price50, card.Product.Price100);
				ShoppingCardVM.OrderHeader.OrderTotal += (card.Price * card.Count);
			}

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u=>u.Id == claim.Value);
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
				ShoppingCardVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCardVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
            else
            {
				ShoppingCardVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCardVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
            
            _unitOfWork.OrderHeader.Add(ShoppingCardVM.OrderHeader);
            _unitOfWork.Save();

			foreach (var card in ShoppingCardVM.ListCard)
			{
                OrderDetail orderDetail = new()
                {
                    ProductId = card.ProductId,
                    OrderId = ShoppingCardVM.OrderHeader.Id,
                    Price = card.Price,
                    Count = card.Count,
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
			}

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {

                var domain = "https://localhost:44332/";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                {
                    "card",
                },

                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = domain + $"customer/card/OrderConfirmation?id={ShoppingCardVM.OrderHeader.Id}",
                    CancelUrl = domain + $"customer/card/index",
                };

                foreach (var item in ShoppingCardVM.ListCard)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title,
                            },
                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                Session session = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCardVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            else
            {
                return RedirectToAction("OrderConfirmation", "Card", new { id = ShoppingCardVM.OrderHeader.Id });
            }
		}


        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader =  _unitOfWork.OrderHeader.GetFirstOrDefault(u=>u.Id==id);

            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment) 
            {
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}

            List<ShoppingCard> shoppingCards = _unitOfWork.ShoppingCard.GetAll(u=>u.ApplicationUserId == 
            orderHeader.ApplicationUserId).ToList();
			_unitOfWork.ShoppingCard.RemoveRange(shoppingCards);
			_unitOfWork.Save();
			return View(id);
		}

		public IActionResult Plus(int cardId)
		{
			var card = _unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cardId);
			_unitOfWork.ShoppingCard.IncrementCount(card, 1);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));

		}
        public IActionResult Minus(int cardId)
        {
            var card = _unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cardId);
            if (card.Count <= 1)
            {
                _unitOfWork.ShoppingCard.Remove(card);
            }
            else
            {
                _unitOfWork.ShoppingCard.DecrementCount(card, 1);
            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));

        }
        public IActionResult Remove(int cardId)
        {
            var card = _unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cardId);
            _unitOfWork.ShoppingCard.Remove(card);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));

        }

        private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if(quantity <= 50)
            {
                return price;
            }
            else
            {
                if(quantity <= 100)
                {
                    return price50;
                }
				return price100;
			}
        }
    }
}
