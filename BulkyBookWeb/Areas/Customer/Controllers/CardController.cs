﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                includeProperties: "Product")
                //facific users shpping item must be fiters for this function 
                //we could change getall() method into filter
            };
            foreach(var card in ShoppingCardVM.ListCard)
            {
                card.Price = GetPriceBasedOnQuantity(card.Count, card.Product.Price,
                    card.Product.Price50, card.Product.Price100);
                ShoppingCardVM.CardTotal += (card.Price * card.Count);
            }
			return View(ShoppingCardVM);
        }

		public IActionResult Summary()
		{
            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //ShoppingCardVM = new ShoppingCardVM
            //{
            //	ListCard = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == claim.Value,
            //	includeProperties: "Product")
            //	//facific users shpping item must be fiters for this function 
            //	//we could change getall() method into filter
            //};
            //foreach (var card in ShoppingCardVM.ListCard)
            //{
            //	card.Price = GetPriceBasedOnQuantity(card.Count, card.Product.Price,
            //		card.Product.Price50, card.Product.Price100);
            //	ShoppingCardVM.CardTotal += (card.Price * card.Count);
            //}
            //return View(ShoppingCardVM);
            return View();
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
