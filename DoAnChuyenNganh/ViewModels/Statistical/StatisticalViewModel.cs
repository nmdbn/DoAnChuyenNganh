using DoAnChuyenNganh.Models;
using System.Collections.Generic;

namespace DoAnChuyenNganh.ViewModels.Statistical
{
    public class StatisticalViewModel
    {
        // ----- TỔNG SỐ -----
        public int TotalCourses { get; set; }
        public int TotalLessons { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalGrades { get; set; }

        // ----- TỔNG HỢP DOANH THU / BÁN HÀNG -----
        public decimal TotalRevenue { get; set; }        // Tổng doanh thu
        public int TotalCourseSales { get; set; }        // Tổng số khóa học đã bán (số order Completed)
        public decimal AverageOrderValue { get; set; }   // Giá trị trung bình mỗi đơn hàng

        // ----- DANH SÁCH CHI TIẾT (BẢNG) -----
        public IEnumerable<DoAnChuyenNganh.Models.Course> Courses { get; set; }
        public IEnumerable<Lesson> Lessons { get; set; } = new List<Lesson>();
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();
        public IEnumerable<Grade> Grades { get; set; } = new List<Grade>();
        public IEnumerable<Payment> Payments { get; set; } = new List<Payment>(); // 🔥 thêm để xem lịch sử thanh toán

        // ----- DỮ LIỆU VẼ BIỂU ĐỒ -----
        public List<MonthlyRevenueData> MonthlyRevenue { get; set; } = new();  // Doanh thu theo tháng
        public List<MonthlySalesData> MonthlySales { get; set; } = new();      // Số khóa học bán ra theo tháng
    }

    public class MonthlyRevenueData
    {
        public string Month { get; set; } = string.Empty; // ví dụ "05/2025"
        public decimal Revenue { get; set; }
    }

    public class MonthlySalesData
    {
        public string Month { get; set; } = string.Empty;
        public int SalesCount { get; set; }
    }
}
