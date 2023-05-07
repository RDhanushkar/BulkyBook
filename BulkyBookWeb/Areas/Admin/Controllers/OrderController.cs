using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;


namespace BulkyBookWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles ="Admin")]
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
