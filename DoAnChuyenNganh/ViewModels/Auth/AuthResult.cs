using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModels.Auth
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}
