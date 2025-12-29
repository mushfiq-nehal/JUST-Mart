using JustMart.DataAccess.Repository.IRepository;
using JustMart.Models;
using JustMart.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JustMartWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CouponController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CouponController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Coupon> objCouponList = _unitOfWork.Coupon.GetAll().ToList();
            return View(objCouponList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Coupon obj)
        {
            if (ModelState.IsValid)
            {
                // Check if coupon code already exists
                var existingCoupon = _unitOfWork.Coupon.Get(c => c.Code.ToUpper() == obj.Code.ToUpper());
                if (existingCoupon != null)
                {
                    ModelState.AddModelError("Code", "Coupon code already exists");
                    return View(obj);
                }

                obj.Code = obj.Code.ToUpper();
                _unitOfWork.Coupon.Add(obj);
                _unitOfWork.Save();
                TempData["success"] = "Coupon created successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Coupon? couponFromDb = _unitOfWork.Coupon.Get(u => u.Id == id);

            if (couponFromDb == null)
            {
                return NotFound();
            }
            return View(couponFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Coupon obj)
        {
            if (ModelState.IsValid)
            {
                // Check if coupon code already exists for other coupons
                var existingCoupon = _unitOfWork.Coupon.Get(c => c.Code.ToUpper() == obj.Code.ToUpper() && c.Id != obj.Id);
                if (existingCoupon != null)
                {
                    ModelState.AddModelError("Code", "Coupon code already exists");
                    return View(obj);
                }

                obj.Code = obj.Code.ToUpper();
                _unitOfWork.Coupon.Update(obj);
                _unitOfWork.Save();
                TempData["success"] = "Coupon updated successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Coupon> objCouponList = _unitOfWork.Coupon.GetAll().ToList();
            return Json(new { data = objCouponList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var couponToBeDeleted = _unitOfWork.Coupon.Get(u => u.Id == id);
            if (couponToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.Coupon.Remove(couponToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
