using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnChuyenNganh.ViewModels.Statistical;
using DoAnChuyenNganh.Models;
using System.Diagnostics;

namespace DoAnChuyenNganh.Controllers
{
    public class StatisticalController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;

        public StatisticalController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            Console.WriteLine("🔥 === BẮT ĐẦU STATISTICAL INDEX ===");

            // Khởi tạo model với giá trị mặc định ngay từ đầu
            var model = new StatisticalViewModel
            {
                MonthlyRevenue = new List<MonthlyRevenueData>(),
                MonthlySales = new List<MonthlySalesData>(),
                Payments = new List<Payment>(),
                Courses = new List<Course>(),
                Lessons = new List<Lesson>(),
                Users = new List<User>(),
                Subjects = new List<Subject>(),
                Grades = new List<Grade>()
            };

            try
            {
                // ===== LẤY DỮ LIỆU CƠ BẢN =====
                Console.WriteLine("📦 Bắt đầu load Courses...");
                model.Courses = await _context.Courses
                    .Include(c => c.Subject)
                    .Include(c => c.Grade)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
                Console.WriteLine($"✅ Loaded {model.Courses.Count()} courses");

                Console.WriteLine("📦 Bắt đầu load Lessons...");
                model.Lessons = await _context.Lessons
                    .Include(l => l.Course)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();
                Console.WriteLine($"✅ Loaded {model.Lessons.Count()} lessons");

                Console.WriteLine("📦 Bắt đầu load Users...");
                model.Users = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();
                Console.WriteLine($"✅ Loaded {model.Users.Count()} users");

                Console.WriteLine("📦 Bắt đầu load Subjects...");
                model.Subjects = await _context.Subjects
                    .OrderBy(s => s.SubjectName)
                    .ToListAsync();
                Console.WriteLine($"✅ Loaded {model.Subjects.Count()} subjects");

                Console.WriteLine("📦 Bắt đầu load Grades...");
                model.Grades = await _context.Grades
                    .OrderBy(g => g.GradeLevel)
                    .ToListAsync();
                Console.WriteLine($"✅ Loaded {model.Grades.Count()} grades");

                // Tổng số
                model.TotalCourses = model.Courses.Count();
                model.TotalLessons = model.Lessons.Count();
                model.TotalUsers = model.Users.Count();
                model.TotalSubjects = model.Subjects.Count();
                model.TotalGrades = model.Grades.Count();

                Console.WriteLine("✅ Đã load xong basic data");
                Console.WriteLine($"Courses: {model.TotalCourses}, Lessons: {model.TotalLessons}, Users: {model.TotalUsers}");

                // ===== THỐNG KÊ DOANH THU =====
                Console.WriteLine("========== BẮT ĐẦU LẤY DỮ LIỆU PAYMENTS ==========");
                Console.WriteLine("🔍 Kiểm tra _context.Payments...");

                // Kiểm tra xem có payment nào không
                Console.WriteLine("📊 Đang đếm tổng số Payments...");
                var totalPayments = await _context.Payments.CountAsync();
                Console.WriteLine($"✅ Tổng số Payments trong DB: {totalPayments}");

                // Lấy completed payments với navigation properties
                var completedPayments = await _context.Payments
                    .Include(p => p.User)
                    .Include(p => p.Course)
                    .Where(p => p.Status == "Completed")
                    .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
                    .ToListAsync();

                Console.WriteLine($"💰 Số Payments có Status = 'Completed': {completedPayments.Count}");

                // In ra chi tiết từng payment
                foreach (var payment in completedPayments.Take(5)) // Chỉ in 5 payment đầu
                {
                    var paidDate = payment.PaidAt ?? payment.CreatedAt;
                    Console.WriteLine($"  💳 Payment ID: {payment.PaymentId}, Amount: {payment.Amount:N0}, PaidAt: {paidDate:yyyy-MM-dd}");
                }

                // Gán payments vào model
                model.Payments = completedPayments;

                // Tính tổng doanh thu
                model.TotalRevenue = completedPayments.Sum(p => p.Amount);
                model.TotalCourseSales = completedPayments.Count();
                model.AverageOrderValue = completedPayments.Any()
                    ? completedPayments.Average(p => p.Amount)
                    : 0;

                Console.WriteLine($"💵 Tổng doanh thu: {model.TotalRevenue:N0} VNĐ");
                Console.WriteLine($"📦 Tổng số khóa học đã bán: {model.TotalCourseSales}");
                Console.WriteLine($"📊 Giá trị đơn hàng trung bình: {model.AverageOrderValue:N0} VNĐ");

                // ===== TẠO ĐẦY ĐỦ 12 THÁNG GẦN NHẤT =====
                var now = DateTime.Now;
                var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

                Console.WriteLine($"📅 Start Date (12 tháng trước): {startDate:yyyy-MM-dd}");
                Console.WriteLine($"📅 End Date (hiện tại): {now:yyyy-MM-dd}");

                // Tạo danh sách 12 tháng
                var allMonths = Enumerable.Range(0, 12)
                    .Select(i => startDate.AddMonths(i))
                    .Select(d => new { d.Year, d.Month })
                    .ToList();

                Console.WriteLine("📆 Danh sách 12 tháng:");
                foreach (var month in allMonths)
                {
                    Console.WriteLine($"  - Tháng {month.Month:D2}/{month.Year}");
                }

                // Lọc payments trong khoảng 12 tháng
                var paymentsInRange = completedPayments
                    .Where(p => (p.PaidAt ?? p.CreatedAt) >= startDate)
                    .ToList();

                Console.WriteLine($"✅ Số Payments trong 12 tháng gần nhất: {paymentsInRange.Count}");

                if (paymentsInRange.Any())
                {
                    Console.WriteLine("📈 Chi tiết payments theo tháng:");
                    var groupedPayments = paymentsInRange
                        .GroupBy(p => new
                        {
                            Year = (p.PaidAt ?? p.CreatedAt).Year,
                            Month = (p.PaidAt ?? p.CreatedAt).Month
                        })
                        .OrderBy(g => g.Key.Year)
                        .ThenBy(g => g.Key.Month);

                    foreach (var group in groupedPayments)
                    {
                        var monthKey = $"{group.Key.Month:D2}/{group.Key.Year}";
                        var revenue = group.Sum(p => p.Amount);
                        var count = group.Count();
                        Console.WriteLine($"  - {monthKey}: Revenue = {revenue:N0} VNĐ, Sales = {count}");
                    }
                }

                // Tính doanh thu thực tế theo tháng
                var actualRevenue = paymentsInRange
                    .GroupBy(p => new
                    {
                        Year = (p.PaidAt ?? p.CreatedAt).Year,
                        Month = (p.PaidAt ?? p.CreatedAt).Month
                    })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Revenue = g.Sum(p => p.Amount),
                        SalesCount = g.Count()
                    })
                    .ToDictionary(
                        x => $"{x.Month:D2}/{x.Year}",
                        x => (Revenue: x.Revenue, SalesCount: x.SalesCount)
                    );

                // Tạo dữ liệu đầy đủ 12 tháng (bao gồm cả tháng không có doanh thu)
                model.MonthlyRevenue = allMonths.Select(m =>
                {
                    var key = $"{m.Month:D2}/{m.Year}";
                    var hasData = actualRevenue.TryGetValue(key, out var data);

                    return new MonthlyRevenueData
                    {
                        Month = key,
                        Revenue = hasData ? data.Revenue : 0m
                    };
                }).ToList();

                model.MonthlySales = allMonths.Select(m =>
                {
                    var key = $"{m.Month:D2}/{m.Year}";
                    var hasData = actualRevenue.TryGetValue(key, out var data);

                    return new MonthlySalesData
                    {
                        Month = key,
                        SalesCount = hasData ? data.SalesCount : 0
                    };
                }).ToList();

                Console.WriteLine($"📊 MonthlyRevenue Count: {model.MonthlyRevenue.Count}");
                Console.WriteLine($"📊 MonthlySales Count: {model.MonthlySales.Count}");

                // In ra dữ liệu cuối cùng
                Console.WriteLine("📋 Dữ liệu MonthlyRevenue:");
                foreach (var item in model.MonthlyRevenue)
                {
                    Console.WriteLine($"  - {item.Month}: {item.Revenue:N0} VNĐ");
                }

                Console.WriteLine("📋 Dữ liệu MonthlySales:");
                foreach (var item in model.MonthlySales)
                {
                    Console.WriteLine($"  - {item.Month}: {item.SalesCount} khóa học");
                }

                Console.WriteLine("========== KẾT THÚC LẤY DỮ LIỆU ==========");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ LỖI: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Model đã được khởi tạo với giá trị mặc định ở trên
                // Không cần khởi tạo lại
            }

            return View("_Statistical", model);
        }

        // API endpoint để lấy thống kê theo khoảng thời gian
        [HttpGet]
        public async Task<IActionResult> GetRevenueByDateRange(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                startDate ??= DateTime.Now.AddMonths(-12);
                endDate ??= DateTime.Now;

                var payments = await _context.Payments
                    .Where(p => p.Status == "Completed"
                        && p.PaidAt.HasValue
                        && p.PaidAt.Value >= startDate
                        && p.PaidAt.Value <= endDate)
                    .GroupBy(p => new
                    {
                        Year = p.PaidAt.Value.Year,
                        Month = p.PaidAt.Value.Month
                    })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Month:D2}/{g.Key.Year}",
                        Revenue = g.Sum(p => p.Amount),
                        SalesCount = g.Count()
                    })
                    .OrderBy(x => x.Month)
                    .ToListAsync();

                return Json(new { success = true, data = payments });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetRevenueByDateRange Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // API endpoint để lấy top khóa học bán chạy
        [HttpGet]
        public async Task<IActionResult> GetTopSellingCourses(int top = 10)
        {
            try
            {
                var topCourses = await _context.Payments
                    .Include(p => p.Course)
                    .Where(p => p.Status == "Completed")
                    .GroupBy(p => new
                    {
                        p.CourseId,
                        CourseTitle = p.Course.Title
                    })
                    .Select(g => new
                    {
                        CourseId = g.Key.CourseId,
                        CourseTitle = g.Key.CourseTitle,
                        TotalSales = g.Count(),
                        TotalRevenue = g.Sum(p => p.Amount)
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .Take(top)
                    .ToListAsync();

                return Json(new { success = true, data = topCourses });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetTopSellingCourses Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // API endpoint để lấy thống kê theo phương thức thanh toán
        [HttpGet]
        public async Task<IActionResult> GetPaymentMethodStats()
        {
            try
            {
                var stats = await _context.Payments
                    .Where(p => p.Status == "Completed")
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new
                    {
                        PaymentMethod = g.Key ?? "Unknown",
                        Count = g.Count(),
                        TotalAmount = g.Sum(p => p.Amount)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetPaymentMethodStats Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // API endpoint để lấy thống kê tổng quan
        [HttpGet]
        public async Task<IActionResult> GetOverviewStats()
        {
            try
            {
                var stats = new
                {
                    TotalRevenue = await _context.Payments
                        .Where(p => p.Status == "Completed")
                        .SumAsync(p => p.Amount),

                    TotalSales = await _context.Payments
                        .Where(p => p.Status == "Completed")
                        .CountAsync(),

                    TotalCourses = await _context.Courses.CountAsync(),

                    TotalUsers = await _context.Users.CountAsync(),

                    RevenueThisMonth = await _context.Payments
                        .Where(p => p.Status == "Completed"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value.Year == DateTime.Now.Year
                            && p.PaidAt.Value.Month == DateTime.Now.Month)
                        .SumAsync(p => p.Amount),

                    SalesThisMonth = await _context.Payments
                        .Where(p => p.Status == "Completed"
                            && p.PaidAt.HasValue
                            && p.PaidAt.Value.Year == DateTime.Now.Year
                            && p.PaidAt.Value.Month == DateTime.Now.Month)
                        .CountAsync()
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetOverviewStats Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> TestPaymentsSimple()
        {
            Console.WriteLine("=== TEST PAYMENTS SIMPLE ===");

            try
            {
                var count = await _context.Payments.CountAsync();
                Console.WriteLine($"✅ Total: {count}");

                var completed = await _context.Payments
                    .Where(p => p.Status == "Completed")
                    .CountAsync();
                Console.WriteLine($"✅ Completed: {completed}");

                return Content($"Total: {count}, Completed: {completed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Content($"ERROR: {ex.Message}");
            }
        }
    }
}