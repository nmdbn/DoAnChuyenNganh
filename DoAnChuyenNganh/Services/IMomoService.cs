using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.Services
{
    public interface IMomoService
    {
        /// <summary>
        /// Tạo URL thanh toán MoMo
        /// </summary>
        /// <param name="payment">Thông tin thanh toán</param>
        /// <param name="returnUrl">URL callback (nullable - lấy từ config nếu null)</param>
        /// <param name="ipnUrl">URL IPN (nullable - lấy từ config nếu null)</param>
        /// <param name="fakePaymentUrl">URL fake payment cho sandbox mode</param>
        Task<string> CreatePaymentUrl(
            Payment payment,
            string? returnUrl,
            string? ipnUrl,
            string? fakePaymentUrl
        );

        /// <summary>
        /// Xử lý callback từ MoMo sau khi thanh toán
        /// </summary>
        Task<PaymentResult> ProcessReturn(IQueryCollection query);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public int? PaymentId { get; set; }
    }
}