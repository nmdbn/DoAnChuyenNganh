namespace DoAnChuyenNganh.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendCODApprovalEmailAsync(string toEmail, string studentName, string courseTitle, decimal amount);
    }
}
