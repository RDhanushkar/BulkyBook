using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.DependencyResolver;
using System.Diagnostics;
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
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult UpdateOrderDetail()
		{
			//var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderMV.OrderHeader.Id, tracked:false);
			//orderHeaderFromDb.Name = OrderMV.OrderHeader.Name;
			//orderHeaderFromDb.PhoneNumber = OrderMV.OrderHeader.PhoneNumber;
			//orderHeaderFromDb.StreetAddress = OrderMV.OrderHeader.StreetAddress;
			//orderHeaderFromDb.City = OrderMV.OrderHeader.City;
			//orderHeaderFromDb.State = OrderMV.OrderHeader.State;
			//orderHeaderFromDb.PostalCode = OrderMV.OrderHeader.PostalCode;
			//if (OrderMV.OrderHeader.Carrier != null)
			//{
			//	orderHeaderFromDb.Carrier = OrderMV.OrderHeader.Carrier;
			//}
			//if (OrderMV.OrderHeader.TrackingNumber != null)
			//{
			//	orderHeaderFromDb.TrackingNumber = OrderMV.OrderHeader.TrackingNumber;
			//}
			////_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
			//_unitOfWork.Save();
			//TempData["Success"] = "Order Details Updated Successfully.";
			//return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id });
			var orderHEaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderMV.OrderHeader.Id, tracked: false);
			orderHEaderFromDb.Name = OrderMV.OrderHeader.Name;
			orderHEaderFromDb.PhoneNumber = OrderMV.OrderHeader.PhoneNumber;
			orderHEaderFromDb.StreetAddress = OrderMV.OrderHeader.StreetAddress;
			orderHEaderFromDb.City = OrderMV.OrderHeader.City;
			orderHEaderFromDb.State = OrderMV.OrderHeader.State;
			orderHEaderFromDb.PostalCode = OrderMV.OrderHeader.PostalCode;
			if (OrderMV.OrderHeader.Carrier != null)
			{
				orderHEaderFromDb.Carrier = OrderMV.OrderHeader.Carrier;
			}
			if (OrderMV.OrderHeader.TrackingNumber != null)
			{
				orderHEaderFromDb.TrackingNumber = OrderMV.OrderHeader.TrackingNumber;
			}
			_unitOfWork.OrderHeader.Update(orderHEaderFromDb);
			_unitOfWork.Save();
			TempData["Success"] = "Order Details Updated Successfully.";
			return RedirectToAction("Details", "Order", new { orderId = orderHEaderFromDb.Id });

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
