using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Statistical;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyenNganh.Controllers
{
    namespace DoAnChuyenNganh.Controllers
    {
        public class DashboardController : Controller
        {
            private readonly DoAnChuyenNganhContext _context;

            public DashboardController(DoAnChuyenNganhContext context)
            {
                _context = context;
            }

            public IActionResult _Statistical()
            {
                var vm = new StatisticalViewModel
                {
                    TotalCourses = _context.Courses.Count(),
                    TotalLessons = _context.Lessons.Count(),
                    TotalUsers = _context.Users.Count(),
                    TotalSubjects = _context.Subjects.Count(),
                    TotalGrades = _context.Grades.Count(),

                    // lấy 20 bản ghi gần nhất mỗi loại cho bảng
                    Courses = _context.Courses
                        .Include(c => c.Subject)
                        .Include(c => c.Grade)
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(20)
                        .ToList(),

                    Lessons = _context.Lessons
                        .Include(l => l.Course)
                        .OrderBy(l => l.CourseId)
                        .ThenBy(l => l.LessonOrder)
                        .Take(20)
                        .ToList(),

                    Users = _context.Users
                        .OrderByDescending(u => u.CreatedAt)
                        .Take(20)
                        .ToList(),

                    Subjects = _context.Subjects
                        .OrderBy(s => s.SubjectName)
                        .ToList(),

                    Grades = _context.Grades
                        .OrderBy(g => g.GradeLevel)
                        .ToList()
                };

                return View("_Statistical", vm); // Views/Admin/_Statistical.cshtml
            }
        }
    }
}
