using JustMart.DataAccess.Repository.IRepository;
using JustMart.DataAccess.Data;
using JustMart.Models;
using JustMart.Models.ViewModels;
using JustMart.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Data;

namespace JustMartWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IBlobStorageService? _blobStorageService;
        
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, IBlobStorageService? blobStorageService = null)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _blobStorageService = blobStorageService;
        }
        public IActionResult Index() 
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
           
            return View(objProductList);
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()
            };
            if (id == null || id == 0)
            {
                //create
                return View(productVM);
            }
            else
            {
                //update
                productVM.Product = _unitOfWork.Product.Get(u=>u.Id==id,includeProperties:"ProductImages");
                return View(productVM);
            }
            
        }
        [HttpPost]
        public async Task<IActionResult> Upsert(ProductVM productVM, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (productVM.Product.Id == 0) {
                    _unitOfWork.Product.Add(productVM.Product);
                }
                else {
                    _unitOfWork.Product.Update(productVM.Product);
                }

                _unitOfWork.Save();

                if (files != null && files.Count > 0)
                {
                    // Use Azure Blob Storage if configured, otherwise fall back to local storage
                    if (_blobStorageService != null)
                    {
                        // Upload to Azure Blob Storage
                        var imageUrls = await _blobStorageService.UploadMultipleImagesAsync(files);
                        
                        foreach(var imageUrl in imageUrls) 
                        {
                            ProductImage productImage = new() {
                                ImageUrl = imageUrl,
                                ProductId = productVM.Product.Id,
                            };

                            if (productVM.Product.ProductImages == null)
                                productVM.Product.ProductImages = new List<ProductImage>();

                            productVM.Product.ProductImages.Add(productImage);
                        }
                    }
                    else
                    {
                        // Fall back to local storage
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        
                        foreach(IFormFile file in files) 
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string productPath = @"images\products\product-" + productVM.Product.Id;
                            string finalPath = Path.Combine(wwwRootPath, productPath);

                            if (!Directory.Exists(finalPath))
                                Directory.CreateDirectory(finalPath);

                            using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create)) {
                                file.CopyTo(fileStream);
                            }

                            ProductImage productImage = new() {
                                ImageUrl = @"\" + productPath + @"\" + fileName,
                                ProductId = productVM.Product.Id,
                            };

                            if (productVM.Product.ProductImages == null)
                                productVM.Product.ProductImages = new List<ProductImage>();

                            productVM.Product.ProductImages.Add(productImage);
                        }
                    }

                    _unitOfWork.Product.Update(productVM.Product);
                    _unitOfWork.Save();
                }

                TempData["success"] = "Product created/updated successfully";
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productVM);
            }
        }


        public async Task<IActionResult> DeleteImage(int imageId) {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            int productId = imageToBeDeleted.ProductId;
            
            if (imageToBeDeleted != null) {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl)) {
                    // Check if it's a cloud URL or local path
                    if (imageToBeDeleted.ImageUrl.StartsWith("http"))
                    {
                        // Delete from Azure Blob Storage
                        if (_blobStorageService != null)
                        {
                            await _blobStorageService.DeleteImageAsync(imageToBeDeleted.ImageUrl);
                        }
                    }
                    else
                    {
                        // Delete from local storage
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                                       imageToBeDeleted.ImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(oldImagePath)) {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                }

                _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                _unitOfWork.Save();

                TempData["success"] = "Deleted successfully";
            }

            return RedirectToAction(nameof(Upsert), new { id = productId });
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }


        [HttpDelete]
        public async Task<IActionResult> Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            // Delete images from storage
            var productImages = _unitOfWork.ProductImage.GetAll(u => u.ProductId == id).ToList();
            
            foreach (var image in productImages)
            {
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    if (image.ImageUrl.StartsWith("http"))
                    {
                        // Delete from Azure Blob Storage
                        if (_blobStorageService != null)
                        {
                            await _blobStorageService.DeleteImageAsync(image.ImageUrl);
                        }
                    }
                    else
                    {
                        // Delete from local storage
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                }
            }

            // Delete product folder from local storage if exists
            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath)) {
                Directory.Delete(finalPath, true);
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}
