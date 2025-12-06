using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Http;

namespace DoAnChuyenNganh.Services
{
    public interface IVnPayService
    {
        /// <summary>
        /// Tạo URL thanh toán VNPay
        /// </summary>
        string CreatePaymentUrl(Payment payment, HttpContext context);

        /// <summary>
        /// Xử lý response từ VNPay
        /// </summary>
        VnPayReturnModel ProcessReturn(IQueryCollection query);
    }

    public class VnPayReturnModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? PaymentId { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string ResponseCode { get; set; }
    }
}