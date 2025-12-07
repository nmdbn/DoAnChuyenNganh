using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyenNganh.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly DoAnChuyenNganhContext _context;
        public DashboardController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                TotalUsers = _context.Users.Count(),
                NewRegistrations = _context.Users
                    .Count(u => u.CreatedAt >= DateTime.Now.AddDays(-7)),
                ActiveStudents = _context.Enrollments
                    .Where(e => e.IsActive == true)
                    .Select(e => e.UserId)
                    .Distinct()
                    .Count(),
                TotalCourses = _context.Courses.Count()
            };

            return View(model);
        }
        [HttpGet]
        public IActionResult GetStudentByMonth()
        {
            int currentYear = DateTime.Now.Year;

            var data = _context.Users
                .Where(u => u.CreatedAt != null &&
                            EF.Functions.DateDiffYear(u.CreatedAt, DateTime.Now) == 0)
                .GroupBy(u => EF.Functions.DateDiffMonth(
                    new DateTime(currentYear, 1, 1),
                    u.CreatedAt.Value
                ))
                .Select(g => new
                {
                    Month = g.Key + 1,
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            return Json(data);
        }
        [HttpGet]
        public IActionResult GetTopCourses()
        {
            var result = _context.Enrollments
                .GroupBy(e => new { e.CourseId, e.Course.Title })
                .Select(g => new
                {
                    courseName = g.Key.Title,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToList();

            return Json(result);
        }
        [HttpGet]
        public IActionResult GetEnrollmentDetails()
        {
            var data = _context.Enrollments
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new
                {
                    e.EnrollmentId,
                    e.EnrolledAt,
                    e.ProgressPercentage,
                    e.IsActive,
                    UserId = e.User.UserId,
                    FirstName = e.User.FirstName,
                    LastName = e.User.LastName,
                    Email = e.User.Email,
                    CourseId = e.Course.CourseId,
                    CourseTitle = e.Course.Title
                })
                .ToList();

            return Json(data);
        }
    }
}
