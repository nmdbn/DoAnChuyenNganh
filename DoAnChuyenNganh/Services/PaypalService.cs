using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DoAnChuyenNganh.Services
{
    public class PaypalService : IPaypalService
    {
        private readonly DoAnChuyenNganhContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaypalService(
            DoAnChuyenNganhContext context,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> CreatePayPalOrder(
            Payment payment,
            IUrlHelper urlHelper,
            string requestScheme)
        {
            try
            {
                // 1) Đọc config và validate
                var clientId = _configuration["PayPal:ClientId"]?.Trim();
                var clientSecret = _configuration["PayPal:ClientSecret"]?.Trim();
                var mode = _configuration["PayPal:Mode"]?.Trim() ?? "sandbox";

                Console.WriteLine($"[PayPal Config]");
                Console.WriteLine($"ClientId length: {clientId?.Length ?? 0}");
                Console.WriteLine($"ClientId first 20 chars: {clientId?.Substring(0, Math.Min(20, clientId?.Length ?? 0))}");
                Console.WriteLine($"Secret length: {clientSecret?.Length ?? 0}");
                Console.WriteLine($"Mode: {mode}");

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    throw new Exception("PayPal ClientId is missing in appsettings.json");
                }

                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    throw new Exception("PayPal ClientSecret is missing in appsettings.json");
                }

                // 2) Xác định base URL
                var baseUrl = string.Equals(mode, "live", StringComparison.OrdinalIgnoreCase)
                    ? "https://api-m.paypal.com"
                    : "https://api-m.sandbox.paypal.com";

                Console.WriteLine($"Base URL: {baseUrl}");

                // 3) Tạo HTTP client
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // 4) Lấy access token
                var accessToken = await GetAccessToken(httpClient, baseUrl, clientId, clientSecret);

                // 5) Tính số tiền USD
                decimal exchangeRate = 23000m; // VND to USD (có thể lấy từ API thực tế)
                decimal amountVnd = payment.Amount;
                decimal amountUsd = Math.Round(amountVnd / exchangeRate, 2);

                // Đảm bảo amount >= 0.01 USD (yêu cầu của PayPal)
                if (amountUsd < 0.01m)
                {
                    amountUsd = 0.01m;
                }

                Console.WriteLine($"Amount: {amountVnd} VND = {amountUsd} USD");

                // 6) Tạo return/cancel URLs
                var returnUrl = urlHelper.Action(
                    "PayPalReturn",
                    "Course",
                    new { paymentId = payment.PaymentId },
                    requestScheme
                );

                var cancelUrl = urlHelper.Action(
                    "PayPalCancel",
                    "Course",
                    new { paymentId = payment.PaymentId },
                    requestScheme
                );

                Console.WriteLine($"Return URL: {returnUrl}");
                Console.WriteLine($"Cancel URL: {cancelUrl}");

                // 7) Tạo PayPal order
                var orderRequest = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            reference_id = payment.PaymentId.ToString(),
                            description = $"Course Payment - ID: {payment.CourseId}",
                            amount = new
                            {
                                currency_code = "USD",
                                value = amountUsd.ToString("F2", CultureInfo.InvariantCulture)
                            }
                        }
                    },
                    application_context = new
                    {
                        brand_name = "DoAnChuyenNganh",
                        landing_page = "BILLING",
                        shipping_preference = "NO_SHIPPING",
                        user_action = "PAY_NOW",
                        return_url = returnUrl,
                        cancel_url = cancelUrl
                    }
                };

                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(orderRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                // Set authorization header
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var orderResponse = await httpClient.PostAsync(
                    $"{baseUrl}/v2/checkout/orders",
                    jsonContent
                );

                var orderContent = await orderResponse.Content.ReadAsStringAsync();

                Console.WriteLine($"[PayPal Order Response] Status: {orderResponse.StatusCode}");
                Console.WriteLine($"[PayPal Order Response] Body: {orderContent}");

                if (!orderResponse.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Failed to create PayPal order: {orderResponse.StatusCode}\n" +
                        $"Response: {orderContent}"
                    );
                }

                // 8) Parse response và lấy approval URL
                var orderData = JsonConvert.DeserializeObject<Dictionary<string, object>>(orderContent);

                if (orderData == null || !orderData.ContainsKey("links"))
                {
                    throw new Exception("PayPal response missing 'links' field");
                }

                var links = orderData["links"] as Newtonsoft.Json.Linq.JArray;

                if (links == null || links.Count == 0)
                {
                    throw new Exception("PayPal response has empty 'links' array");
                }

                foreach (var link in links)
                {
                    var rel = link["rel"]?.ToString();
                    if (rel == "approve")
                    {
                        var approvalUrl = link["href"]?.ToString();

                        if (string.IsNullOrEmpty(approvalUrl))
                        {
                            throw new Exception("PayPal approval URL is empty");
                        }

                        Console.WriteLine($"[PayPal Success] Approval URL: {approvalUrl}");
                        return approvalUrl;
                    }
                }

                throw new Exception("No 'approve' link found in PayPal response");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayPal Error] {ex.Message}");
                Console.WriteLine($"[PayPal Error] Stack: {ex.StackTrace}");
                throw new Exception($"PayPal CreateOrder failed: {ex.Message}", ex);
            }
        }

        public async Task<bool> CapturePayPalOrder(string token, int paymentId)
        {
            try
            {
                // 1) Đọc config
                var clientId = _configuration["PayPal:ClientId"]?.Trim();
                var clientSecret = _configuration["PayPal:ClientSecret"]?.Trim();
                var mode = _configuration["PayPal:Mode"]?.Trim() ?? "sandbox";

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    Console.WriteLine("[PayPal Capture Error] Missing credentials");
                    return false;
                }

                var baseUrl = mode.Equals("live", StringComparison.OrdinalIgnoreCase)
                    ? "https://api-m.paypal.com"
                    : "https://api-m.sandbox.paypal.com";

                Console.WriteLine($"[PayPal Capture] Token: {token}, PaymentId: {paymentId}");

                // 2) Tạo HTTP client
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // 3) Lấy access token
                var accessToken = await GetAccessToken(httpClient, baseUrl, clientId, clientSecret);

                // 4) Capture order
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var captureRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{baseUrl}/v2/checkout/orders/{token}/capture"
                )
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };

                var captureResponse = await httpClient.SendAsync(captureRequest);
                var captureContent = await captureResponse.Content.ReadAsStringAsync();

                Console.WriteLine($"[PayPal Capture Response] Status: {captureResponse.StatusCode}");
                Console.WriteLine($"[PayPal Capture Response] Body: {captureContent}");

                if (!captureResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[PayPal Capture Error] Failed with status {captureResponse.StatusCode}");
                    return false;
                }

                // 5) Parse response
                dynamic captureData = JsonConvert.DeserializeObject<dynamic>(captureContent);

                if (captureData == null)
                {
                    Console.WriteLine("[PayPal Capture Error] Failed to parse response");
                    return false;
                }

                var status = captureData.status?.ToString();
                Console.WriteLine($"[PayPal Capture] Order status: {status}");

                if (status != "COMPLETED")
                {
                    Console.WriteLine($"[PayPal Capture Error] Order not completed, status: {status}");
                    return false;
                }

                // 6) Update database
                var payment = await _context.Payments
                    .Include(p => p.Course)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null)
                {
                    Console.WriteLine($"[PayPal Capture Error] Payment {paymentId} not found in database");
                    return false;
                }

                // Extract transaction ID from capture response
                string transactionId = token;
                try
                {
                    var purchaseUnits = captureData.purchase_units;
                    if (purchaseUnits != null && purchaseUnits.Count > 0)
                    {
                        var captures = purchaseUnits[0].payments?.captures;
                        if (captures != null && captures.Count > 0)
                        {
                            transactionId = captures[0].id?.ToString() ?? token;
                        }
                    }
                }
                catch
                {
                    // Fallback to token if parsing fails
                }

                payment.Status = "Completed";
                payment.PaidAt = DateTime.Now;
                payment.TransactionId = transactionId;

                if (payment.Course != null)
                {
                    payment.Course.EnrollmentCount++;
                }

                await _context.SaveChangesAsync();

                Console.WriteLine($"[PayPal Capture Success] Payment {paymentId} completed");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayPal Capture Error] {ex.Message}");
                Console.WriteLine($"[PayPal Capture Error] Stack: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Helper method để lấy PayPal access token
        /// </summary>
        private async Task<string> GetAccessToken(
            HttpClient httpClient,
            string baseUrl,
            string clientId,
            string clientSecret)
        {
            try
            {
                // 1) Tạo Basic Auth token
                var authString = $"{clientId}:{clientSecret}";
                var authBytes = Encoding.UTF8.GetBytes(authString);
                var authToken = Convert.ToBase64String(authBytes);

                Console.WriteLine($"[PayPal Auth] Requesting access token...");
                Console.WriteLine($"[PayPal Auth] Auth string length: {authString.Length}");

                // 2) Clear headers và set Authorization
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

                // 3) Tạo request
                var tokenRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{baseUrl}/v1/oauth2/token"
                )
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "client_credentials" }
                    })
                };

                // 4) Gửi request
                var tokenResponse = await httpClient.SendAsync(tokenRequest);
                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();

                Console.WriteLine($"[PayPal Auth] Response status: {tokenResponse.StatusCode}");

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[PayPal Auth Error] Response: {tokenContent}");
                    throw new Exception(
                        $"Failed to get PayPal access token: {tokenResponse.StatusCode}\n" +
                        $"Response: {tokenContent}\n" +
                        $"ClientId length: {clientId.Length}\n" +
                        $"Secret length: {clientSecret.Length}\n" +
                        $"BaseUrl: {baseUrl}"
                    );
                }

                // 5) Parse response
                var tokenData = JsonConvert.DeserializeObject<Dictionary<string, object>>(tokenContent);

                if (tokenData == null || !tokenData.ContainsKey("access_token"))
                {
                    throw new Exception("PayPal token response missing 'access_token' field");
                }

                var accessToken = tokenData["access_token"].ToString();

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new Exception("PayPal access token is empty");
                }

                Console.WriteLine($"[PayPal Auth] Access token obtained successfully");
                return accessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayPal Auth Error] {ex.Message}");
                throw;
            }
        }
    }
}