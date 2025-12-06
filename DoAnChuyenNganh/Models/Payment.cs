namespace DoAnChuyenNganh.Models
{
    public partial class Payment
    {
        public int PaymentId { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "MoMo";
        public string? TransactionId { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }
        public string? MoMoOrderId { get; set; }
        public string? MoMoRequestId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? SpecificAddress { get; set; }
        public string? Note { get; set; }
        public virtual User User { get; set; } = null!;
        public virtual Course Course { get; set; } = null!;
        public string? VnPayOrderId { get; set; }
    }
}