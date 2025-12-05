using System.Threading.Tasks;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Mvc;

namespace DoAnChuyenNganh.Services
{
    public interface IPaypalService
    {
        Task<string> CreatePayPalOrder(Payment payment, IUrlHelper urlHelper, string requestScheme);
        Task<bool> CapturePayPalOrder(string token, int paymentId);
    }
}
