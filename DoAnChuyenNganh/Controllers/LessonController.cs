using System;
using System.Linq;
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
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "Username");
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "Username");
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

            if (ModelState.IsValid)
            {
                try
                {
                    // Set thời gian ở server
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
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "Username", lesson.CreatedBy);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "Username", lesson.UpdatedBy);
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
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "Username", lesson.CreatedBy);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "Username", lesson.UpdatedBy);

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

            if (ModelState.IsValid)
            {
                try
                {
                    // Giữ CreatedAt cũ, chỉ update UpdatedAt
                    var existing = await _context.Lessons
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.LessonId == id);

                    if (existing == null)
                        return NotFound();

                    lesson.CreatedAt = existing.CreatedAt;
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
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "Username", lesson.CreatedBy);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "Username", lesson.UpdatedBy);

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