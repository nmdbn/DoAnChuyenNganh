using DoAnChuyenNganh.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace DoAnChuyenNganh.Services
{
    public class MomoService : IMomoService
    {
        private readonly DoAnChuyenNganhContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public MomoService(
            DoAnChuyenNganhContext context,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        // ĐÃ ĐỔI THỨ TỰ: (payment, returnUrl, ipnUrl, fakePaymentUrl)
        public async Task<string> CreatePaymentUrl(
            Payment payment,
            string returnUrl,
            string ipnUrl,
            string fakePaymentUrl)
        {
            var useSandbox = _config.GetValue<bool>("MOMO:UseSandbox");

            // ====== MODE DEV: DÙNG TRANG FAKE MOMO ======
            if (useSandbox)
            {
                // Nếu có fakePaymentUrl thì redirect qua đó
                if (!string.IsNullOrEmpty(fakePaymentUrl))
                    return fakePaymentUrl;

                // fallback: quay lại trang Public nếu không có trang fake
                return "/Course/Public";
            }

            // ====== MODE PROD: GỌI MOMO THẬT ======
            var partnerCode = _config["MOMO:PartnerCode"];
            var accessKey = _config["MOMO:AccessKey"];
            var secretKey = _config["MOMO:SecretKey"];
            var endpoint = _config["MOMO:Endpoint"];

            if (string.IsNullOrEmpty(secretKey))
                throw new Exception("MOMO SecretKey chưa được cấu hình! Vui lòng kiểm tra appsettings.json");

            var requestId = Guid.NewGuid().ToString();
            var orderId = "MOMO" + DateTime.Now.Ticks;
            var amount = payment.Amount.ToString("0");
            var orderInfo = $"Thanh toan khoa hoc {payment.Course?.Title ?? "eLearning"}";
            var extraData = "";

            var rawHash =
                $"accessKey={accessKey}" +
                $"&amount={amount}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={ipnUrl}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&partnerCode={partnerCode}" +
                $"&redirectUrl={returnUrl}" +
                $"&requestId={requestId}" +
                $"&requestType=captureWallet";

            var signature = HmacSHA256(rawHash, secretKey);

            var requestBody = new
            {
                partnerCode,
                accessKey,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl,
                extraData,
                requestType = "captureWallet",
                signature
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                endpoint + "/v2/gateway/api/create",
                new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));

            var resultJson = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(resultJson);

            if (result?.resultCode == "0")
            {
                payment.MoMoOrderId = orderId;
                payment.MoMoRequestId = requestId;
                await _context.SaveChangesAsync();
                return result.payUrl;
            }

            throw new Exception("Tạo link MoMo thất bại: " + (string?)result?.message);
        }

        public async Task<PaymentResult> ProcessReturn(IQueryCollection query)
        {
            // Ép về string cho chắc
            var resultCode = query["resultCode"].ToString();
            var orderId = query["orderId"].ToString();
            var message = query["message"].ToString();
            var transId = query["transId"].ToString();

            // ====== CASE: THANH TOÁN ẢO (FAKE MOMO) ======
            if (query.ContainsKey("paymentId"))
            {
                if (int.TryParse(query["paymentId"].ToString(), out int paymentId))
                {
                    var payment = await _context.Payments
                        .Include(p => p.Course)
                        .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                    if (payment != null && payment.Status == "Pending")
                    {
                        payment.Status = "Completed";
                        payment.PaidAt = DateTime.Now;
                        payment.TransactionId = "FAKE_SUCCESS_" + DateTime.Now.Ticks;

                        if (payment.Course != null)
                            payment.Course.EnrollmentCount++;

                        await _context.SaveChangesAsync();
                    }

                    return new PaymentResult
                    {
                        Success = true,
                        CourseId = payment?.CourseId
                    };
                }
            }

            // ====== CASE: MOMO THẬT ======
            if (resultCode == "0")
            {
                var payment = await _context.Payments
                    .Include(p => p.Course) // THÊM Include cho đúng
                    .FirstOrDefaultAsync(p => p.MoMoOrderId == orderId);

                if (payment != null && payment.Status == "Pending")
                {
                    payment.Status = "Completed";
                    payment.PaidAt = DateTime.Now;
                    payment.TransactionId = transId;

                    if (payment.Course != null)
                        payment.Course.EnrollmentCount++;

                    await _context.SaveChangesAsync();
                }

                return new PaymentResult
                {
                    Success = true,
                    CourseId = payment?.CourseId
                };
            }

            return new PaymentResult
            {
                Success = false,
                Message = message ?? "Thanh toán thất bại hoặc bị hủy."
            };
        }

        private string HmacSHA256(string input, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "SecretKey không được để trống");

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
