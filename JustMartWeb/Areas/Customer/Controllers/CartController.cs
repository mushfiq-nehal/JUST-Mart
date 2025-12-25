using JustMart.DataAccess.Repository.IRepository;
using JustMart.Models;
using JustMart.Models.ViewModels;
using JustMart.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JustMartWeb.Areas.Customer.Controllers {

    [Area("customer")]
    [Authorize]
    public class CartController : Controller {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly SSLCommerzService _sslCommerzService;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, SSLCommerzService sslCommerzService) {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _sslCommerzService = sslCommerzService;
        }


        public IActionResult Index() {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new() {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader= new()
            };

            IEnumerable<ProductImage> productImages = _unitOfWork.ProductImage.GetAll();

            foreach (var cart in ShoppingCartVM.ShoppingCartList) {
                cart.Product.ProductImages = productImages.Where(u => u.ProductId == cart.Product.Id).ToList();
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary() {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new() {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;



            foreach (var cart in ShoppingCartVM.ShoppingCartList) {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
		public async Task<IActionResult> SummaryPOST() {
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product");

			ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);


			foreach (var cart in ShoppingCartVM.ShoppingCartList) {
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

            if (applicationUser.CompanyId.GetValueOrDefault() == 0) {
				//it is a regular customer 
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
            else {
				//it is a company user
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
			_unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
			_unitOfWork.Save();
            foreach(var cart in ShoppingCartVM.ShoppingCartList) {
                OrderDetail orderDetail = new() {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

			if (applicationUser.CompanyId.GetValueOrDefault() == 0) {
                //it is a regular customer - initiate SSLCommerz payment
                var domain = $"{Request.Scheme}://{Request.Host}";
                
                var sslRequest = new SSLCommerzRequest
                {
                    TotalAmount = (decimal)ShoppingCartVM.OrderHeader.OrderTotal,
                    Currency = "BDT",
                    TransactionId = "JUST_" + ShoppingCartVM.OrderHeader.Id.ToString(),
                    SuccessUrl = $"{domain}/customer/cart/PaymentSuccess",
                    FailUrl = $"{domain}/customer/cart/PaymentFail",
                    CancelUrl = $"{domain}/customer/cart/PaymentCancel",
                    IpnUrl = $"{domain}/customer/cart/PaymentIPN",
                    CustomerName = ShoppingCartVM.OrderHeader.Name,
                    CustomerEmail = applicationUser.Email,
                    CustomerAddress = ShoppingCartVM.OrderHeader.StreetAddress,
                    CustomerCity = ShoppingCartVM.OrderHeader.City,
                    CustomerCountry = "Bangladesh",
                    CustomerPhone = ShoppingCartVM.OrderHeader.PhoneNumber,
                    ProductName = "JUST Mart Order #" + ShoppingCartVM.OrderHeader.Id,
                    ProductCategory = "General"
                };

                var paymentResponse = await _sslCommerzService.InitiatePayment(sslRequest);
                
                if (paymentResponse.Status == "SUCCESS")
                {
                    // Store SessionKey for later validation
                    ShoppingCartVM.OrderHeader.SessionId = paymentResponse.SessionKey;
                    _unitOfWork.OrderHeader.Update(ShoppingCartVM.OrderHeader);
                    _unitOfWork.Save();
                    
                    // Redirect to SSLCommerz payment gateway
                    return Redirect(paymentResponse.GatewayPageURL);
                }
                else
                {
                    TempData["error"] = "Payment initiation failed: " + paymentResponse.Failedreason;
                    return RedirectToAction(nameof(Summary));
                }
			}

			return RedirectToAction(nameof(OrderConfirmation),new { id=ShoppingCartVM.OrderHeader.Id });
		}


        public IActionResult OrderConfirmation(int id) {

			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // Payment should be validated in PaymentSuccess callback
                // Only clear cart if payment was successful
                if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
                {
                    // Clear shopping cart session but keep user logged in
                    HttpContext.Session.SetInt32(SD.SessionCart, 0);

                    _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - JUST Mart",
                        $"<p>Your order has been placed successfully!</p><p>Order ID: {orderHeader.Id}</p><p>Total Amount: ৳{orderHeader.OrderTotal:F2}</p>");

                    List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                        .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

                    _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
                    _unitOfWork.Save();
                }
            }
            else
            {
                // Company user - delayed payment
                HttpContext.Session.SetInt32(SD.SessionCart, 0);
                _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - JUST Mart",
                    $"<p>New Order Created - {orderHeader.Id}</p>");

                List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                    .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

                _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
                _unitOfWork.Save();
            }

			return View(id);
		}

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentSuccess()
        {
            var valId = Request.Form["val_id"].ToString();
            var tranId = Request.Form["tran_id"].ToString();
            
            // Debug logging
            TempData["Debug"] = $"Received - valId: {valId}, tranId: {tranId}";

            if (string.IsNullOrEmpty(valId) || string.IsNullOrEmpty(tranId))
            {
                TempData["error"] = "Invalid payment response - Missing val_id or tran_id";
                return RedirectToAction("Index", "Home");
            }

            // Validate payment with SSLCommerz
            var validationResponse = await _sslCommerzService.ValidatePayment(valId);
            
            TempData["Debug2"] = $"Validation Status: {validationResponse.Status}, Bank Tran: {validationResponse.Bank_tran_id}, Card: {validationResponse.Card_type}";

            if (validationResponse.Status == "VALID" || validationResponse.Status == "VALIDATED")
            {
                // Extract order ID from transaction ID (format: JUST_OrderId)
                var orderIdStr = tranId.Replace("JUST_", "");
                if (int.TryParse(orderIdStr, out int orderId))
                {
                    var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
                    if (orderHeader != null)
                    {
                        // Update payment status and save transaction details
                        orderHeader.PaymentIntentId = validationResponse.Bank_tran_id ?? validationResponse.Tran_id;
                        orderHeader.PaymentDate = DateTime.Now;
                        
                        // Store additional payment info in SessionId field for reference
                        var paymentInfo = $"{validationResponse.Card_type ?? "Online"} - {validationResponse.Card_brand ?? "SSLCommerz"}";
                        if (string.IsNullOrEmpty(orderHeader.SessionId) || !orderHeader.SessionId.Contains("|"))
                        {
                            orderHeader.SessionId = $"{orderHeader.SessionId}|{paymentInfo}";
                        }
                        
                        TempData["Debug3"] = $"Saving - PaymentIntentId: {orderHeader.PaymentIntentId}, SessionId: {orderHeader.SessionId}";
                        
                        _unitOfWork.OrderHeader.Update(orderHeader);
                        _unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusApproved, SD.PaymentStatusApproved);
                        _unitOfWork.Save();

                        TempData["success"] = "Payment verified successfully!";
                        return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
                    }
                }
            }

            TempData["error"] = $"Payment validation failed - Status: {validationResponse.Status}";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult PaymentFail()
        {
            var tranId = Request.Form["tran_id"].ToString();
            var orderIdStr = tranId.Replace("JUST_", "");
            
            if (int.TryParse(orderIdStr, out int orderId))
            {
                var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
                if (orderHeader != null)
                {
                    _unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusCancelled, SD.StatusCancelled);
                    _unitOfWork.Save();
                }
            }

            TempData["error"] = "Payment failed. Please try again.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult PaymentCancel()
        {
            var tranId = Request.Form["tran_id"].ToString();
            var orderIdStr = tranId.Replace("JUST_", "");
            
            if (int.TryParse(orderIdStr, out int orderId))
            {
                var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
                if (orderHeader != null)
                {
                    _unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusCancelled, SD.StatusCancelled);
                    _unitOfWork.Save();
                }
            }

            TempData["error"] = "Payment was cancelled.";
            return RedirectToAction(nameof(Index));
        }

        // IPN (Instant Payment Notification) endpoint for SSLCommerz
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentIPN()
        {
            try
            {
                var valId = Request.Form["val_id"].ToString();
                var tranId = Request.Form["tran_id"].ToString();
                var status = Request.Form["status"].ToString();

                if (string.IsNullOrEmpty(valId) || string.IsNullOrEmpty(tranId))
                {
                    return BadRequest("Invalid IPN data");
                }

                // Validate payment with SSLCommerz
                var validationResponse = await _sslCommerzService.ValidatePayment(valId);

                if ((validationResponse.Status == "VALID" || validationResponse.Status == "VALIDATED") && status == "VALID")
                {
                    // Extract order ID from transaction ID
                    var orderIdStr = tranId.Replace("JUST_", "");
                    if (int.TryParse(orderIdStr, out int orderId))
                    {
                        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
                        if (orderHeader != null && orderHeader.PaymentStatus != SD.PaymentStatusApproved)
                        {
                            // Update payment status
                            orderHeader.PaymentIntentId = validationResponse.Bank_tran_id ?? validationResponse.Tran_id;
                            orderHeader.PaymentDate = DateTime.Now;
                            
                            // Store payment info
                            var paymentInfo = $"{validationResponse.Card_type ?? "Online"} - {validationResponse.Card_brand ?? "SSLCommerz"}";
                            if (string.IsNullOrEmpty(orderHeader.SessionId) || !orderHeader.SessionId.Contains("|"))
                            {
                                orderHeader.SessionId = $"{orderHeader.SessionId}|{paymentInfo}";
                            }
                            
                            _unitOfWork.OrderHeader.Update(orderHeader);
                            _unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusApproved, SD.PaymentStatusApproved);
                            _unitOfWork.Save();

                            return Ok("IPN processed successfully");
                        }
                    }
                }

                return Ok("IPN received");
            }
            catch (Exception ex)
            {
                // Log error but return OK to SSLCommerz
                return Ok($"IPN error: {ex.Message}");
            }
        }


		public IActionResult Plus(int cartId) {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId) {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if (cartFromDb.Count <= 1) {
                //remove that from cart
                
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                    .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
            }
            else {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId) {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
           
            _unitOfWork.ShoppingCart.Remove(cartFromDb);

            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
              .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }



        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart) {
            if (shoppingCart.Count <= 50) {
                return shoppingCart.Product.Price;
            }
            else {
                if (shoppingCart.Count <= 100) {
                    return shoppingCart.Product.Price50;
                }
                else {
                    return shoppingCart.Product.Price100;
                }
            }
        }
    }
}
