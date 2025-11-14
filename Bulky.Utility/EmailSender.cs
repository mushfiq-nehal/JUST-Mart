using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BulkyBook.Utility {
    public class EmailSender : IEmailSender {
        public string BrevoApiKey { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }

        public EmailSender(IConfiguration _config) {
            BrevoApiKey = _config.GetValue<string>("Brevo:ApiKey");
            SenderEmail = _config.GetValue<string>("Brevo:SenderEmail");
            SenderName = _config.GetValue<string>("Brevo:SenderName");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage) {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", BrevoApiKey);
            client.DefaultRequestHeaders.Add("accept", "application/json");

            var emailData = new {
                sender = new { name = SenderName, email = SenderEmail },
                to = new[] { new { email = email } },
                subject = subject,
                htmlContent = htmlMessage
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
            response.EnsureSuccessStatusCode();
        }
    }
}

