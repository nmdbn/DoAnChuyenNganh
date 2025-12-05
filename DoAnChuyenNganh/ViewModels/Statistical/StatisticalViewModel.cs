using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModels.Statistical
{
    public class StatisticalViewModel
    {
        // Tổng số
        public int TotalCourses { get; set; }
        public int TotalLessons { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalGrades { get; set; }

        // Danh sách chi tiết (để hiển thị bảng)
        public IEnumerable<DoAnChuyenNganh.Models.Course> Courses { get; set; } = new List<DoAnChuyenNganh.Models.Course>();
        public IEnumerable<Lesson> Lessons { get; set; } = new List<Lesson>();
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();
        public IEnumerable<Grade> Grades { get; set; } = new List<Grade>();
    }
}

