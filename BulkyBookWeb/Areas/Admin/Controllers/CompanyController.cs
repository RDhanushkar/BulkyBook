
using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using NuGet.Packaging.Signing;


namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            
        }
        public IActionResult Index()
        {
            //IEnumerable<CoverType> objCoverTypeList = _unitOfWork.CoverType.GetAll();
            return View(); 
        }

        
        public IActionResult Upsert(int? id)
        {
            Company company = new();

            if (id == null || id == 0)
            {
                return View(company);
            }
            else
            {
                company = _unitOfWork.Company.GetFirstOrDefault(u=>u.Id == id);
                return View(company);
                
            }

            
        }

        //POST Method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj,  IFormFile? file)
        {

            if (ModelState.IsValid)
            {
                if(obj.Id == 0)
                {
                    _unitOfWork.Company.Add(obj);
                    TempData["success"] = "Company Created Successfuly.";
                }
                else
                {
                    _unitOfWork.Company.Update(obj);
                }
                _unitOfWork.Save();
                TempData["success"] = "Company Updated Successfuly.";
                return RedirectToAction("Index");
            }
            return View(obj);

        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var companyList = _unitOfWork.Company.GetAll();
            return Json(new { data = companyList });
        }

        //POST Method
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Company.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return Json(new { success= false, message = "Error while deleting" });
            }

            _unitOfWork.Company.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful." });
        }

        #endregion
    }
}
