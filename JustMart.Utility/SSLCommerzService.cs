using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JustMart.Utility
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
            try
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
                    { "ipn_url", request.IpnUrl }, // IPN endpoint for server-to-server notification
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

                if (sslResponse == null)
                {
                    return new SSLCommerzResponse 
                    { 
                        Status = "FAILED", 
                        Failedreason = "Failed to parse response from SSLCommerz" 
                    };
                }

                return sslResponse;
            }
            catch (Exception ex)
            {
                return new SSLCommerzResponse 
                { 
                    Status = "FAILED", 
                    Failedreason = $"Exception: {ex.Message}" 
                };
            }
        }

        public async Task<SSLCommerzValidationResponse> ValidatePayment(string valId)
        {
            try
            {
                var baseUrl = _settings.IsSandbox 
                    ? "https://sandbox.sslcommerz.com" 
                    : "https://securepay.sslcommerz.com";

                var parameters = new Dictionary<string, string>
                {
                    { "val_id", valId },
                    { "store_id", _settings.StoreId },
                    { "store_passwd", _settings.StorePassword },
                    { "format", "json" }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.GetAsync($"{baseUrl}/validator/api/validationserverAPI.php?{await content.ReadAsStringAsync()}");
                var responseString = await response.Content.ReadAsStringAsync();

                var validationResponse = JsonSerializer.Deserialize<SSLCommerzValidationResponse>(responseString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return validationResponse ?? new SSLCommerzValidationResponse { Status = "FAILED" };
            }
            catch (Exception ex)
            {
                return new SSLCommerzValidationResponse 
                { 
                    Status = "FAILED",
                    Failedreason = $"Exception: {ex.Message}"
                };
            }
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
        public string IpnUrl { get; set; } // IPN endpoint
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerCountry { get; set; } = "Bangladesh";
        public string CustomerPhone { get; set; }
        public string ProductName { get; set; }
        public string ProductCategory { get; set; } = "General";
    }

    public class SSLCommerzResponse
    {
        public string Status { get; set; }
        public string GatewayPageURL { get; set; }
        public string Failedreason { get; set; }
        public string SessionKey { get; set; }
    }

    public class SSLCommerzValidationResponse
    {
        public string Status { get; set; }
        public string Tran_date { get; set; }
        public string Tran_id { get; set; }
        public string Val_id { get; set; }
        public decimal Amount { get; set; }
        public string Store_amount { get; set; }
        public string Currency { get; set; }
        public string Bank_tran_id { get; set; }
        public string Card_type { get; set; }
        public string Card_no { get; set; }
        public string Card_issuer { get; set; }
        public string Card_brand { get; set; }
        public string Card_issuer_country { get; set; }
        public string Card_issuer_country_code { get; set; }
        public string Currency_type { get; set; }
        public string Currency_amount { get; set; }
        public string Currency_rate { get; set; }
        public string Base_fair { get; set; }
        public string Value_a { get; set; }
        public string Value_b { get; set; }
        public string Value_c { get; set; }
        public string Value_d { get; set; }
        public string Risk_level { get; set; }
        public string Risk_title { get; set; }
        public string Failedreason { get; set; }
    }
}
