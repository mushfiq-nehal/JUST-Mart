using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BulkyBook.Utility
{
    public class SSLCommerzService
    {
        private readonly SSLCommerzSettings _settings;
        private readonly HttpClient _httpClient;

        public SSLCommerzService(SSLCommerzSettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();
        }

        public async Task<SSLCommerzResponse> InitiatePayment(SSLCommerzRequest request)
        {
            var baseUrl = _settings.IsSandbox 
                ? "https://sandbox.sslcommerz.com" 
                : "https://securepay.sslcommerz.com";

            var parameters = new Dictionary<string, string>
            {
                { "store_id", _settings.StoreId },
                { "store_passwd", _settings.StorePassword },
                { "total_amount", request.TotalAmount.ToString("F2") },
                { "currency", request.Currency },
                { "tran_id", request.TransactionId },
                { "success_url", request.SuccessUrl },
                { "fail_url", request.FailUrl },
                { "cancel_url", request.CancelUrl },
                { "cus_name", request.CustomerName },
                { "cus_email", request.CustomerEmail },
                { "cus_add1", request.CustomerAddress },
                { "cus_city", request.CustomerCity },
                { "cus_country", request.CustomerCountry },
                { "cus_phone", request.CustomerPhone },
                { "shipping_method", "NO" },
                { "product_name", request.ProductName },
                { "product_category", request.ProductCategory },
                { "product_profile", "general" }
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync($"{baseUrl}/gwprocess/v4/api.php", content);
            var responseString = await response.Content.ReadAsStringAsync();

            var sslResponse = JsonSerializer.Deserialize<SSLCommerzResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return sslResponse;
        }

        public async Task<bool> ValidatePayment(string valId)
        {
            var baseUrl = _settings.IsSandbox 
                ? "https://sandbox.sslcommerz.com" 
                : "https://securepay.sslcommerz.com";

            var parameters = new Dictionary<string, string>
            {
                { "val_id", valId },
                { "store_id", _settings.StoreId },
                { "store_passwd", _settings.StorePassword }
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync($"{baseUrl}/validator/api/validationserverAPI.php", content);
            var responseString = await response.Content.ReadAsStringAsync();

            // Simple validation - check if status is VALID or VALIDATED
            return responseString.Contains("VALID") || responseString.Contains("VALIDATED");
        }
    }

    public class SSLCommerzRequest
    {
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "BDT";
        public string TransactionId { get; set; }
        public string SuccessUrl { get; set; }
        public string FailUrl { get; set; }
        public string CancelUrl { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerCountry { get; set; } = "Bangladesh";
        public string CustomerPhone { get; set; }
        public string ProductName { get; set; }
        public string ProductCategory { get; set; } = "Books";
    }

    public class SSLCommerzResponse
    {
        public string Status { get; set; }
        public string GatewayPageURL { get; set; }
        public string Failedreason { get; set; }
        public string SessionKey { get; set; }
    }
}
