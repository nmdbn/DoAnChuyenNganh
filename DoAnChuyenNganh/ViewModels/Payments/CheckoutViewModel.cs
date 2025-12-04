namespace DoAnChuyenNganh.ViewModels.Payments
{
    public class CheckoutViewModel
    {
        // Thông tin khóa học
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public decimal Price { get; set; }

        // Info người mua
        public string FullName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Province { get; set; } = "";
        public string District { get; set; } = "";
        public string Ward { get; set; } = "";
        public string SpecificAddress { get; set; } = "";
        public string Note { get; set; } = "";

        // Phương thức thanh toán
        public string PaymentMethod { get; set; } = "COD"; // mặc định
    }
}
