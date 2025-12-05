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
                if (!string.IsNullOrEmpty(fakePaymentUrl))
                    return fakePaymentUrl;

                return "/Course/Public";
            }

            // ====== MODE PROD: GỌI MOMO THẬT ======
            var partnerCode = _config["MOMO:PartnerCode"];
            var accessKey = _config["MOMO:AccessKey"];
            var secretKey = _config["MOMO:SecretKey"];
            var endpoint = _config["MOMO:Endpoint"];

            // ✅ LẤY RETURN URL VÀ IPN URL TỪ APPSETTINGS
            var configReturnUrl = _config["MOMO:ReturnUrl"];
            var configIpnUrl = _config["MOMO:IpnUrl"];

            if (string.IsNullOrEmpty(secretKey))
                throw new Exception("MOMO SecretKey chưa được cấu hình!");

            if (string.IsNullOrEmpty(configReturnUrl))
                throw new Exception("MOMO ReturnUrl chưa được cấu hình!");

            if (string.IsNullOrEmpty(configIpnUrl))
                throw new Exception("MOMO IpnUrl chưa được cấu hình!");

            var requestId = Guid.NewGuid().ToString();
            var orderId = "MOMO" + DateTime.Now.Ticks;
            var amount = ((long)payment.Amount).ToString();
            var orderInfo = $"Thanh toan khoa hoc: {payment.Course?.Title ?? "eLearning"}";
            var extraData = payment.PaymentId.ToString();
            var requestType = "captureWallet";
            var autoCapture = true;
            var lang = "vi";

            // Tạo rawSignature theo đúng format MoMo yêu cầu
            var rawSignature =
                $"accessKey={accessKey}" +
                $"&amount={amount}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={configIpnUrl}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&partnerCode={partnerCode}" +
                $"&redirectUrl={configReturnUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={requestType}";

            var signature = HmacSHA256(rawSignature, secretKey);

            var requestBody = new
            {
                partnerCode,
                partnerName = "eLearning Platform",
                storeId = "eLearningStore",
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = configReturnUrl,
                ipnUrl = configIpnUrl,
                lang,
                requestType,
                autoCapture,
                extraData,
                orderGroupId = "",
                signature
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                var jsonContent = JsonConvert.SerializeObject(requestBody);

                System.Diagnostics.Debug.WriteLine("=== MoMo Request ===");
                System.Diagnostics.Debug.WriteLine($"URL: {endpoint}/v2/gateway/api/create");
                System.Diagnostics.Debug.WriteLine($"Body: {jsonContent}");
                System.Diagnostics.Debug.WriteLine($"RawSignature: {rawSignature}");
                System.Diagnostics.Debug.WriteLine($"Signature: {signature}");

                var response = await client.PostAsync(
                    $"{endpoint}/v2/gateway/api/create",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                var resultJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"MoMo Response: {resultJson}");

                dynamic result = JsonConvert.DeserializeObject(resultJson);

                // resultCode = 0 là thành công
                if (result?.resultCode == 0)
                {
                    payment.MoMoOrderId = orderId;
                    payment.MoMoRequestId = requestId;
                    await _context.SaveChangesAsync();
                    return result.payUrl;
                }

                throw new Exception($"Tạo link MoMo thất bại: [{result?.resultCode}] {result?.message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MoMo Error: {ex.Message}");
                throw new Exception($"Lỗi kết nối MoMo: {ex.Message}");
            }
        }

        public async Task<PaymentResult> ProcessReturn(IQueryCollection query)
        {
            var partnerCode = query["partnerCode"].ToString();
            var orderId = query["orderId"].ToString();
            var requestId = query["requestId"].ToString();
            var amount = query["amount"].ToString();
            var orderInfo = query["orderInfo"].ToString();
            var orderType = query["orderType"].ToString();
            var transId = query["transId"].ToString();
            var resultCode = query["resultCode"].ToString();
            var message = query["message"].ToString();
            var payType = query["payType"].ToString();
            var responseTime = query["responseTime"].ToString();
            var extraData = query["extraData"].ToString();
            var signature = query["signature"].ToString();

            // Log để debug
            System.Diagnostics.Debug.WriteLine("=== MoMo Return ===");
            System.Diagnostics.Debug.WriteLine($"ResultCode: {resultCode}");
            System.Diagnostics.Debug.WriteLine($"OrderId: {orderId}");
            System.Diagnostics.Debug.WriteLine($"TransId: {transId}");
            System.Diagnostics.Debug.WriteLine($"ExtraData: {extraData}");

            // Verify signature
            var secretKey = _config["MOMO:SecretKey"];
            var accessKey = _config["MOMO:AccessKey"];

            var rawSignature =
                $"accessKey={accessKey}" +
                $"&amount={amount}" +
                $"&extraData={extraData}" +
                $"&message={message}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&orderType={orderType}" +
                $"&partnerCode={partnerCode}" +
                $"&payType={payType}" +
                $"&requestId={requestId}" +
                $"&responseTime={responseTime}" +
                $"&resultCode={resultCode}" +
                $"&transId={transId}";

            var computedSignature = HmacSHA256(rawSignature, secretKey);

            System.Diagnostics.Debug.WriteLine($"RawSignature: {rawSignature}");
            System.Diagnostics.Debug.WriteLine($"Computed Signature: {computedSignature}");
            System.Diagnostics.Debug.WriteLine($"Received Signature: {signature}");

            // Kiểm tra chữ ký
            if (computedSignature != signature)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Signature không khớp!");
                return new PaymentResult
                {
                    Success = false,
                    Message = "Chữ ký không hợp lệ"
                };
            }

            // resultCode = 0 là thành công
            if (resultCode == "0")
            {
                var payment = await _context.Payments
                    .Include(p => p.Course)
                    .FirstOrDefaultAsync(p => p.MoMoOrderId == orderId);

                if (payment != null && payment.Status == "Pending")
                {
                    payment.Status = "Completed";
                    payment.PaidAt = DateTime.Now;
                    payment.TransactionId = transId;

                    if (payment.Course != null)
                        payment.Course.EnrollmentCount++;

                    await _context.SaveChangesAsync();

                    return new PaymentResult
                    {
                        Success = true,
                        CourseId = payment.CourseId,
                        PaymentId = payment.PaymentId,
                        Message = "Thanh toán thành công"
                    };
                }

                return new PaymentResult
                {
                    Success = false,
                    Message = "Không tìm thấy đơn hàng"
                };
            }

            // Các mã lỗi khác
            return new PaymentResult
            {
                Success = false,
                Message = message ?? "Thanh toán thất bại hoặc bị hủy"
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