using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyenNganh.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly DoAnChuyenNganhContext _context;

        public VnPayService(IConfiguration config, DoAnChuyenNganhContext context)
        {
            _config = config;
            _context = context;
        }

        public string CreatePaymentUrl(Payment payment, HttpContext context)
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                var vnpayUrl = _config["VNPAY:Url"];
                var tmnCode = _config["VNPAY:TmnCode"];
                var hashSecret = _config["VNPAY:HashSecret"];
                var returnUrl = _config["VNPAY:ReturnUrl"];

                Console.WriteLine("========== VNPAY PAYMENT REQUEST ==========");
                Console.WriteLine($"TmnCode: {tmnCode}");
                Console.WriteLine($"HashSecret: {hashSecret}");
                Console.WriteLine($"VnpayUrl: {vnpayUrl}");
                Console.WriteLine($"ReturnUrl: {returnUrl}");

                if (string.IsNullOrEmpty(vnpayUrl) || string.IsNullOrEmpty(tmnCode) ||
                    string.IsNullOrEmpty(hashSecret) || string.IsNullOrEmpty(returnUrl))
                {
                    throw new Exception("VNPay configuration is missing");
                }

                // Tạo OrderId unique
                var orderId = $"VNP{payment.PaymentId}_{DateTime.Now:yyyyMMddHHmmss}";
                payment.VnPayOrderId = orderId;
                _context.Payments.Update(payment);
                _context.SaveChanges();

                // Amount calculation (VNPay yêu cầu nhân 100)
                decimal amountVND = payment.Amount;
                long amountToSend = (long)Math.Round(amountVND * 100);

                Console.WriteLine($"========== AMOUNT CALCULATION ==========");
                Console.WriteLine($"Amount in database (VNĐ): {amountVND:N0}");
                Console.WriteLine($"Amount to send to VNPay: {amountToSend}");
                Console.WriteLine($"OrderId: {orderId}");
                Console.WriteLine($"========================================");

                // ✅ SỬ DỤNG VNPAYLIBRARY TỪ ĐỒ ÁN CŨ
                var vnpay = new VnPayLibrary();

                // Thêm các tham số theo đúng thứ tự
                vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", tmnCode);
                vnpay.AddRequestData("vnp_Amount", amountToSend.ToString());
                vnpay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", GetIpAddress(context));
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan khoa hoc {payment.CourseId}");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
                vnpay.AddRequestData("vnp_TxnRef", orderId);

                // Expire date (15 phút)
                var expireDate = timeNow.AddMinutes(15);
                vnpay.AddRequestData("vnp_ExpireDate", expireDate.ToString("yyyyMMddHHmmss"));

                // ✅ Tạo URL với signature đúng
                var paymentUrl = vnpay.CreateRequestUrl(vnpayUrl, hashSecret);

                Console.WriteLine("========== PAYMENT URL ==========");
                Console.WriteLine(paymentUrl);
                Console.WriteLine("=================================");

                return paymentUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public VnPayReturnModel ProcessReturn(IQueryCollection query)
        {
            try
            {
                Console.WriteLine("========== VNPAY RETURN PROCESSING ==========");

                // ✅ SỬ DỤNG VNPAYLIBRARY TỪ ĐỒ ÁN CŨ
                var vnpay = new VnPayLibrary();

                // Thêm tất cả params vào VnPayLibrary
                foreach (var (key, value) in query)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(key, value.ToString());
                        Console.WriteLine($"{key}: {value}");
                    }
                }

                var orderId = vnpay.GetResponseData("vnp_TxnRef");
                var vnpayTranId = vnpay.GetResponseData("vnp_TransactionNo");
                var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
                var secureHash = query["vnp_SecureHash"].ToString();
                var hashSecret = _config["VNPAY:HashSecret"];

                Console.WriteLine($"========== VALIDATION ==========");
                Console.WriteLine($"OrderId: {orderId}");
                Console.WriteLine($"TransactionNo: {vnpayTranId}");
                Console.WriteLine($"ResponseCode: {responseCode}");
                Console.WriteLine($"SecureHash from VNPay: {secureHash}");

                // ✅ Validate signature bằng VnPayLibrary
                bool checkSignature = vnpay.ValidateSignature(secureHash, hashSecret);
                Console.WriteLine($"Signature Valid: {checkSignature}");

                if (!checkSignature)
                {
                    Console.WriteLine("❌ Invalid signature");
                    return new VnPayReturnModel
                    {
                        Success = false,
                        Message = "Chữ ký không hợp lệ"
                    };
                }

                // Tìm payment trong database
                var payment = _context.Payments
                    .Include(p => p.Course)
                    .FirstOrDefault(p => p.VnPayOrderId == orderId);

                if (payment == null)
                {
                    Console.WriteLine($"❌ Payment not found for OrderId: {orderId}");
                    return new VnPayReturnModel
                    {
                        Success = false,
                        Message = "Không tìm thấy giao dịch"
                    };
                }

                // Xử lý kết quả thanh toán
                if (responseCode == "00")
                {
                    if (payment.Status == "Pending")
                    {
                        payment.Status = "Completed";
                        payment.PaidAt = DateTime.Now;
                        payment.TransactionId = vnpayTranId;

                        if (payment.Course != null)
                        {
                            payment.Course.EnrollmentCount++;
                        }

                        _context.SaveChanges();
                        Console.WriteLine($"✅ Payment {payment.PaymentId} completed successfully");
                    }

                    return new VnPayReturnModel
                    {
                        Success = true,
                        Message = "Thanh toán thành công",
                        PaymentId = payment.PaymentId,
                        TransactionId = vnpayTranId,
                        Amount = payment.Amount,
                        ResponseCode = responseCode
                    };
                }
                else
                {
                    if (payment.Status == "Pending")
                    {
                        payment.Status = "Failed";
                        _context.SaveChanges();
                    }

                    Console.WriteLine($"❌ Payment failed with code: {responseCode}");

                    return new VnPayReturnModel
                    {
                        Success = false,
                        Message = GetResponseMessage(responseCode),
                        PaymentId = payment.PaymentId,
                        ResponseCode = responseCode
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ProcessReturn Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return new VnPayReturnModel
                {
                    Success = false,
                    Message = "Lỗi xử lý thanh toán: " + ex.Message
                };
            }
        }

        private string GetIpAddress(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            return ipAddress;
        }

        private string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường)",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa",
                "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP)",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày",
                "71" => "Giao dịch không thành công do: Số tiền thanh toán không hợp lệ",
                "75" => "Ngân hàng thanh toán đang bảo trì",
                "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định",
                "99" => "Giao dịch thất bại do lỗi hệ thống",
                _ => "Giao dịch thất bại"
            };
        }
    }
}