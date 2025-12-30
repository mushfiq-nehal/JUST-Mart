using JustMart.DataAccess.Repository.IRepository;
using JustMart.Models;
using JustMart.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace JustMartWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index(int? categoryId, string searchTerm)
        {
            // Get all categories for the category cards
            var categories = _unitOfWork.Category.GetAll().ToList();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;
            
            // Filter products by category if specified
            IEnumerable<Product> productList;
            if (categoryId.HasValue)
            {
                productList = _unitOfWork.Product.GetAll(
                    u => u.CategoryId == categoryId.Value,
                    includeProperties: "Category,ProductImages");
            }
            else
            {
                productList = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductImages");
            }
            
            // Filter by search term if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                productList = productList.Where(p => 
                    p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Author.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                ViewBag.SearchTerm = searchTerm;
            }
            
            // Check if user is logged in and is a company user
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                var user = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
                ViewBag.IsCompanyUser = user?.CompanyId != null && user.CompanyId > 0;
            }
            else
            {
                ViewBag.IsCompanyUser = false;
            }
            
            return View(productList);
        }

        [HttpGet]
        public IActionResult GetProducts(int? categoryId, string searchTerm)
        {
            // Filter products by category if specified
            IEnumerable<Product> productList;
            if (categoryId.HasValue)
            {
                productList = _unitOfWork.Product.GetAll(
                    u => u.CategoryId == categoryId.Value,
                    includeProperties: "Category,ProductImages");
            }
            else
            {
                productList = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductImages");
            }
            
            // Filter by search term if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                productList = productList.Where(p => 
                    p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Author.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }
            
            return PartialView("_ProductsPartial", productList);
        }

        [HttpGet]
        public IActionResult SearchProducts(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new List<object>());
            }

            var products = _unitOfWork.Product.GetAll(includeProperties: "Category,ProductImages")
                .Where(p => 
                    p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Author.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(p => new {
                    id = p.Id,
                    title = p.Title,
                    author = p.Author,
                    listPrice = p.ListPrice,
                    discountPrice = p.DiscountPrice,
                    categoryName = p.Category.Name,
                    productImages = p.ProductImages.Select(img => new { imageUrl = img.ImageUrl }).ToList()
                })
                .ToList();

            return Json(products);
        }

        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new() {
                Product = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Category,ProductImages"),
                Count = 1,
                ProductId = productId
            };
            
            // Check if user is logged in and is a company user
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                var user = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
                ViewBag.IsCompanyUser = user?.CompanyId != null && user.CompanyId > 0;
            }
            else
            {
                ViewBag.IsCompanyUser = false;
            }
            
            return View(cart);
        }

        public IActionResult GetProductDetailsModal(int productId)
        {
            ShoppingCart cart = new() {
                Product = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Category,ProductImages"),
                Count = 1,
                ProductId = productId
            };
            
            // Check if user is logged in and is a company user
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                var user = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
                ViewBag.IsCompanyUser = user?.CompanyId != null && user.CompanyId > 0;
            }
            else
            {
                ViewBag.IsCompanyUser = false;
            }
            
            return PartialView("_DetailsModal", cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart) 
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId= userId;

            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u=>u.ApplicationUserId == userId &&
            u.ProductId==shoppingCart.ProductId);

            if (cartFromDb != null) {
                //shopping cart exists
                cartFromDb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
            }
            else {
                //add cart record
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart,
                _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count());
            }

            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                var cartCount = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count();
                return Json(new { success = true, message = "Cart updated successfully", cartCount = cartCount });
            }

            TempData["success"] = "Cart updated successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count();
                return Json(new { count = count });
            }
            return Json(new { count = 0 });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}