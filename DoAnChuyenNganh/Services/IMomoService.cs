using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.Services
{
    public interface IMomoService
    {
        // Đổi thứ tự tham số: returnUrl, ipnUrl, fakePaymentUrl
        Task<string> CreatePaymentUrl(
            Payment payment,
            string returnUrl,
            string ipnUrl,
            string fakePaymentUrl
        );

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
