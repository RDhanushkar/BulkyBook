using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Linq;
using System.Security.Claims;


namespace BulkyBookWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		public OrderMV OrderMV { get; set; }
		public OrderController(IUnitOfWork unitOfWork) 
		{
			_unitOfWork = unitOfWork;
		}
		public IActionResult Index()
		{
			return View();
		}
        public IActionResult Details(int orderId)
        {
			OrderMV = new OrderMV()
			{
				OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u=>u.Id == orderId, includeProperties:"ApplicationUser"),
				OrderDetail = _unitOfWork.OrderDetail.GetAll(u=>u.OrderId == orderId, includeProperties:"Product"),
			};
            return View(OrderMV);
        }

		[ActionName("Details")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Details_Pay_Now(int orderId, OrderMV orderMV)
		{

            OrderMV = new OrderMV()
            {
                OrderHeader = orderMV.OrderHeader,
                OrderDetail = orderMV.OrderDetail
            };

            orderMV.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderMV.OrderHeader.Id, includeProperties: "ApplicationUser");
			orderMV.OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == orderMV.OrderHeader.Id, includeProperties: "Product");


			var domain = "https://localhost:44332/";
			var options = new SessionCreateOptions
			{
				PaymentMethodTypes = new List<string>
				{
					"card",
				},

				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
				SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={orderMV.OrderHeader.Id}",
				CancelUrl = domain + $"admin/order/details?orderId={orderMV.OrderHeader.Id}",
			};

			foreach (var item in orderMV.OrderDetail)
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
			_unitOfWork.OrderHeader.UpdateStripePaymentId(orderMV.OrderHeader.Id, session.Id, session.PaymentIntentId);
			_unitOfWork.Save();
			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
	
		}

		public IActionResult PaymentConfirmation(int orderHeaderId)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderHeaderId);

			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}

			
			return View(orderHeaderId);
		}


		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult UpdateOrderDetail(OrderMV orderMV)
		{
			OrderMV = new OrderMV()
			{
				OrderHeader = orderMV.OrderHeader,
				OrderDetail = orderMV.OrderDetail
			};
			var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderMV.OrderHeader.Id, tracked:false);
			if (orderHeaderFromDb != null)
			{
				orderHeaderFromDb.Name = orderMV.OrderHeader.Name;
				orderHeaderFromDb.PhoneNumber = orderMV.OrderHeader.PhoneNumber;
				orderHeaderFromDb.StreetAddress = orderMV.OrderHeader.StreetAddress;
				orderHeaderFromDb.City = orderMV.OrderHeader.City;
				orderHeaderFromDb.State = orderMV.OrderHeader.State;
				orderHeaderFromDb.PostalCode = orderMV.OrderHeader.PostalCode;
				if (orderMV.OrderHeader.Carrier != null)
				{
					orderHeaderFromDb.Carrier = orderMV.OrderHeader.Carrier;
				}
				if (orderMV.OrderHeader.TrackingNumber != null)
				{
					orderHeaderFromDb.TrackingNumber = orderMV.OrderHeader.TrackingNumber;
				}
				_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
				_unitOfWork.Save();
				TempData["Success"] = "Order Details Updated Successfully.";
				return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id });
			}
			else
			{
				// handle the case where the orderHeaderFromDb is null
				return NotFound();
			}

		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult StartProcessing(OrderMV orderMV)
		{
			OrderMV = new OrderMV()
			{
				OrderHeader = orderMV.OrderHeader,
				OrderDetail = orderMV.OrderDetail
			};
			_unitOfWork.OrderHeader.UpdateStatus(orderMV.OrderHeader.Id, SD.StatusInProcess);
			_unitOfWork.Save();
			TempData["Success"] = "Order Stauts Updated Successfully.";
			return RedirectToAction("Details", "Order", new { orderId = orderMV.OrderHeader.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult ShipOrder(OrderMV orderMV)
		{
			OrderMV = new OrderMV()
			{
				OrderHeader = orderMV.OrderHeader,
				OrderDetail = orderMV.OrderDetail
			};
			var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderMV.OrderHeader.Id, tracked: false);
			orderHeader.TrackingNumber = orderMV.OrderHeader.TrackingNumber;
			orderHeader.Carrier = orderMV.OrderHeader.Carrier;
			orderHeader.OrderStatus = SD.StatusShipped;
			orderHeader.ShippingDate = DateTime.Now;
			if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
				orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
			}
			_unitOfWork.OrderHeader.Update(orderHeader);
			_unitOfWork.Save();
			TempData["Success"] = "Order Shipped Successfully.";
			return RedirectToAction("Details", "Order", new { orderId = orderMV.OrderHeader.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		[ValidateAntiForgeryToken]
		public IActionResult CancelOrder(OrderMV orderMV)
		{
			OrderMV = new OrderMV()
			{
				OrderHeader = orderMV.OrderHeader,
				OrderDetail = orderMV.OrderDetail
			};
			var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderMV.OrderHeader.Id, tracked: false);
			if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
			{
				var options = new RefundCreateOptions
				{
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = orderHeader.PaymentIntentId
				};
				var service = new RefundService();
				Refund refund = service.Create(options);

				_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);

			}
			else
			{
				_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
			}
			_unitOfWork.Save();
			TempData["Success"] = "Order Cancel Successfully.";
			return RedirectToAction("Details", "Order", new { orderId = orderMV.OrderHeader.Id });
		}


		#region API CALLS
		[HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orderHeaders;
			if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
			{
                orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            }
			else
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                orderHeaders = _unitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId == claim.Value, includeProperties: "ApplicationUser");
            }
			

            switch (status)
            {
				case "pending":
					orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
					break;
				case "inprocess":
					orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
					break;
				case "completed":
					orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
					break;
                case "approved":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
				default:
					break;
			}

			return Json(new { data = orderHeaders });
		}
		#endregion
	}
}
