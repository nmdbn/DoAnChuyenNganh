using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyenNganh.Areas.Admin.Controllers
{
    public class LessonController : AdminBaseController
    {
        private readonly DoAnChuyenNganhContext _context;

        public LessonController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        // GET: Lesson
        public async Task<IActionResult> Index()
        {
            var doAnChuyenNganhContext = _context.Lessons.Include(l => l.Course).Include(l => l.CreatedByNavigation).Include(l => l.UpdatedByNavigation);
            return View(await doAnChuyenNganhContext.ToListAsync());
        }

        // GET: Lesson/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.CreatedByNavigation)
                .Include(l => l.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.LessonId == id);
            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }

        // GET: Lesson/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId");
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId");
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Lesson/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LessonId,CourseId,Title,Content,LessonOrder,LessonType,VideoUrl,VideoDuration,IsPreviewable,IsPublished,CreatedAt,UpdatedAt,CreatedBy,UpdatedBy")] Lesson lesson)
        {
            if (ModelState.IsValid)
            {
                _context.Add(lesson);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", lesson.CourseId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", lesson.CreatedBy);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", lesson.UpdatedBy);
            return View(lesson);
        }

        // GET: Lesson/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", lesson.CourseId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", lesson.CreatedBy);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", lesson.UpdatedBy);
            return View(lesson);
        }

        // POST: Lesson/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LessonId,CourseId,Title,Content,LessonOrder,LessonType,VideoUrl,VideoDuration,IsPreviewable,IsPublished,CreatedAt,UpdatedAt,CreatedBy,UpdatedBy")] Lesson lesson)
        {
            if (id != lesson.LessonId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lesson);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LessonExists(lesson.LessonId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", lesson.CourseId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", lesson.CreatedBy);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", lesson.UpdatedBy);
            return View(lesson);
        }

        // GET: Lesson/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.CreatedByNavigation)
                .Include(l => l.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.LessonId == id);
            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }

        // POST: Lesson/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson != null)
            {
                _context.Lessons.Remove(lesson);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LessonExists(int id)
        {
            return _context.Lessons.Any(e => e.LessonId == id);
        }
    }
}
