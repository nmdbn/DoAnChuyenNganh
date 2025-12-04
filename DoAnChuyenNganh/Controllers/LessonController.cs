using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyenNganh.Controllers
{
    public class LessonController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;

        public LessonController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        // Hàm helper lấy UserId từ user đang đăng nhập
        private int GetCurrentUserId()
        {
            // Cách 1: Nếu bạn lưu UserId trong Claims
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            // Cách 2: Nếu bạn lưu UserId trong Session
            // var userId = HttpContext.Session.GetInt32("UserId");
            // if (userId.HasValue) return userId.Value;

            // Mặc định trả về 1 nếu không tìm thấy (hoặc throw exception)
            return 1; // TODO: Thay đổi logic này theo cách bạn quản lý session
        }

        // GET: Lesson
        public async Task<IActionResult> Index()
        {
            var lessons = _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.CreatedByNavigation)
                .Include(l => l.UpdatedByNavigation)
                .OrderBy(l => l.CourseId)
                .ThenBy(l => l.LessonOrder);

            return View(await lessons.ToListAsync());
        }

        // GET: Lesson/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.CreatedByNavigation)
                .Include(l => l.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.LessonId == id);

            if (lesson == null)
                return NotFound();

            return View(lesson);
        }

        // GET: Lesson/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title");
            // Không cần ViewData cho CreatedBy và UpdatedBy nữa
            return View();
        }

        // POST: Lesson/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lesson lesson)
        {
            // Loại bỏ validation cho các field navigation properties
            ModelState.Remove("Course");
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("UpdatedByNavigation");
            ModelState.Remove("CourseMaterials");
            ModelState.Remove("LessonProgresses");
            ModelState.Remove("CreatedBy");  // Thêm dòng này
            ModelState.Remove("UpdatedBy");  // Thêm dòng này

            if (ModelState.IsValid)
            {
                try
                {
                    // Tự động set user hiện tại
                    int currentUserId = GetCurrentUserId();

                    lesson.CreatedBy = currentUserId;
                    lesson.UpdatedBy = currentUserId;
                    lesson.CreatedAt = DateTime.Now;
                    lesson.UpdatedAt = DateTime.Now;

                    _context.Add(lesson);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Lesson created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating lesson: {ex.Message}";
                }
            }

            // Nếu có lỗi, load lại dropdown
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", lesson.CourseId);
            return View(lesson);
        }

        // GET: Lesson/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
                return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", lesson.CourseId);
            return View(lesson);
        }

        // POST: Lesson/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Lesson lesson)
        {
            if (id != lesson.LessonId)
                return NotFound();

            // Loại bỏ validation cho navigation properties
            ModelState.Remove("Course");
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("UpdatedByNavigation");
            ModelState.Remove("CourseMaterials");
            ModelState.Remove("LessonProgresses");
            ModelState.Remove("UpdatedBy");  // Thêm dòng này

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy bản ghi cũ
                    var existing = await _context.Lessons
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.LessonId == id);

                    if (existing == null)
                        return NotFound();

                    // Giữ nguyên CreatedAt và CreatedBy
                    lesson.CreatedAt = existing.CreatedAt;
                    lesson.CreatedBy = existing.CreatedBy;

                    // Tự động set user hiện tại cho UpdatedBy
                    lesson.UpdatedBy = GetCurrentUserId();
                    lesson.UpdatedAt = DateTime.Now;

                    _context.Update(lesson);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Lesson updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LessonExists(lesson.LessonId))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error updating lesson: {ex.Message}";
                }
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", lesson.CourseId);
            return View(lesson);
        }

        // GET: Lesson/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.CreatedByNavigation)
                .Include(l => l.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.LessonId == id);

            if (lesson == null)
                return NotFound();

            return View(lesson);
        }

        // POST: Lesson/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var lesson = await _context.Lessons.FindAsync(id);
                if (lesson != null)
                {
                    _context.Lessons.Remove(lesson);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Lesson deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting lesson: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LessonExists(int id)
        {
            return _context.Lessons.Any(e => e.LessonId == id);
        }
    }
}