using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		public OrderController(IUnitOfWork unitOfWork) 
		{
			_unitOfWork = unitOfWork;
		}
		public IActionResult Index()
		{
			return View();
		}
		#region API CALLS
		[HttpGet]
		public IActionResult GetAll()
		{
			IEnumerable<OrderHeader> orderHeader;
			orderHeader = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
			return Json(new { data = orderHeader });
		}
		#endregion
	}
}
