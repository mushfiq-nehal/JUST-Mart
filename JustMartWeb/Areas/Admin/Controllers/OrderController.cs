using JustMart.DataAccess.Repository.IRepository;
using JustMart.Models;
using JustMart.Models.ViewModels;
using JustMart.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace JustMartWeb.Areas.Admin.Controllers {
	[Area("admin")]
    [Authorize]
	public class OrderController : Controller {


		private readonly IUnitOfWork _unitOfWork;
        private readonly SSLCommerzService _sslCommerzService;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork, SSLCommerzService sslCommerzService)
        {
            _unitOfWork = unitOfWork;
            _sslCommerzService = sslCommerzService;
        }

        public IActionResult Index() {
            return View();
        }

        public IActionResult Details(int orderId) {
            OrderVM = new() {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };

            return View(OrderVM);
        }
        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail() {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier)) {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber)) {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";


            return RedirectToAction(nameof(Details), new {orderId= orderHeaderFromDb.Id});
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing() {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder() {

            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment) {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder() {

            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            // TODO: Implement payment gateway refund when payment is integrated
            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved) {
                // Refund logic will be added with payment gateway
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _unitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }



        [ActionName("Details")]
        [HttpPost]
        public async Task<IActionResult> Details_PAY_NOW() 
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeader
                .Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetail = _unitOfWork.OrderDetail
                .GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

            // Initiate SSLCommerz payment for company users with delayed payment
            var domain = $"{Request.Scheme}://{Request.Host}";
            
            var sslRequest = new SSLCommerzRequest
            {
                TotalAmount = (decimal)OrderVM.OrderHeader.OrderTotal,
                Currency = "BDT",
                TransactionId = "JUST_" + OrderVM.OrderHeader.Id.ToString(),
                SuccessUrl = $"{domain}/admin/order/PaymentSuccess",
                FailUrl = $"{domain}/admin/order/PaymentFail",
                CancelUrl = $"{domain}/admin/order/PaymentCancel",
                CustomerName = OrderVM.OrderHeader.Name,
                CustomerEmail = OrderVM.OrderHeader.ApplicationUser.Email,
                CustomerAddress = OrderVM.OrderHeader.StreetAddress,
                CustomerCity = OrderVM.OrderHeader.City,
                CustomerCountry = "Bangladesh",
                CustomerPhone = OrderVM.OrderHeader.PhoneNumber,
                ProductName = "JUST Mart Order #" + OrderVM.OrderHeader.Id,
                ProductCategory = "General"
            };

            var paymentResponse = await _sslCommerzService.InitiatePayment(sslRequest);
            
            if (paymentResponse.Status == "SUCCESS")
            {
                // Store SessionKey for later validation
                OrderVM.OrderHeader.SessionId = paymentResponse.SessionKey;
                _unitOfWork.OrderHeader.Update(OrderVM.OrderHeader);
                _unitOfWork.Save();
                
                // Redirect to SSLCommerz payment gateway
                return Redirect(paymentResponse.GatewayPageURL);
            }
            else
            {
                TempData["error"] = "Payment initiation failed: " + paymentResponse.Failedreason;
                return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentSuccess()
        {
            var valId = Request.Form["val_id"].ToString();
            var tranId = Request.Form["tran_id"].ToString();

            if (string.IsNullOrEmpty(valId) || string.IsNullOrEmpty(tranId))
            {
                TempData["error"] = "Invalid payment response";
                return RedirectToAction(nameof(Index));
            }

            // Validate payment with SSLCommerz
            var validationResponse = await _sslCommerzService.ValidatePayment(valId);

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
                        
                        // Store additional payment info
                        var paymentInfo = $"{validationResponse.Card_type ?? "Online"} - {validationResponse.Card_brand ?? "SSLCommerz"}";
                        if (string.IsNullOrEmpty(orderHeader.SessionId) || !orderHeader.SessionId.Contains("|"))
                        {
                            orderHeader.SessionId = $"{orderHeader.SessionId}|{paymentInfo}";
                        }
                        
                        _unitOfWork.OrderHeader.Update(orderHeader);
                        _unitOfWork.OrderHeader.UpdateStatus(orderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                        _unitOfWork.Save();

                        TempData["Success"] = "Payment received successfully!";
                        return RedirectToAction(nameof(Details), new { orderId = orderId });
                    }
                }
            }

            TempData["error"] = "Payment validation failed";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult PaymentFail()
        {
            TempData["error"] = "Payment failed. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult PaymentCancel()
        {
            TempData["error"] = "Payment was cancelled.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult PaymentConfirmation(int orderHeaderId) {

            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            // Payment is already validated in callback methods
            return View(orderHeaderId);
        }



        #region API CALLS

        [HttpGet]
		public IActionResult GetAll(string status) {
            IEnumerable<OrderHeader> objOrderHeaders;


            if(User.IsInRole(SD.Role_Admin)|| User.IsInRole(SD.Role_Employee)) {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else {

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader
                    .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }


            switch (status) {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;

            }


            return Json(new { data = objOrderHeaders });
		}


		#endregion
	}
}
