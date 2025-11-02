using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.Controllers
{
    public class CourseMaterialController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;

        public CourseMaterialController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        // GET: CourseMaterial
        public async Task<IActionResult> Index()
        {
            var doAnChuyenNganhContext = _context.CourseMaterials.Include(c => c.Course).Include(c => c.CreatedByNavigation).Include(c => c.Lesson);
            return View(await doAnChuyenNganhContext.ToListAsync());
        }

        // GET: CourseMaterial/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseMaterial = await _context.CourseMaterials
                .Include(c => c.Course)
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.Lesson)
                .FirstOrDefaultAsync(m => m.MaterialId == id);
            if (courseMaterial == null)
            {
                return NotFound();
            }

            return View(courseMaterial);
        }

        // GET: CourseMaterial/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId");
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId");
            ViewData["LessonId"] = new SelectList(_context.Lessons, "LessonId", "LessonId");
            return View();
        }

        // POST: CourseMaterial/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaterialId,CourseId,LessonId,MaterialName,MaterialType,FileUrl,FileSize,DownloadCount,IsPublic,CreatedAt,CreatedBy")] CourseMaterial courseMaterial)
        {
            if (ModelState.IsValid)
            {
                _context.Add(courseMaterial);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", courseMaterial.CourseId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", courseMaterial.CreatedBy);
            ViewData["LessonId"] = new SelectList(_context.Lessons, "LessonId", "LessonId", courseMaterial.LessonId);
            return View(courseMaterial);
        }

        // GET: CourseMaterial/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseMaterial = await _context.CourseMaterials.FindAsync(id);
            if (courseMaterial == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", courseMaterial.CourseId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", courseMaterial.CreatedBy);
            ViewData["LessonId"] = new SelectList(_context.Lessons, "LessonId", "LessonId", courseMaterial.LessonId);
            return View(courseMaterial);
        }

        // POST: CourseMaterial/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaterialId,CourseId,LessonId,MaterialName,MaterialType,FileUrl,FileSize,DownloadCount,IsPublic,CreatedAt,CreatedBy")] CourseMaterial courseMaterial)
        {
            if (id != courseMaterial.MaterialId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(courseMaterial);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseMaterialExists(courseMaterial.MaterialId))
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", courseMaterial.CourseId);
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", courseMaterial.CreatedBy);
            ViewData["LessonId"] = new SelectList(_context.Lessons, "LessonId", "LessonId", courseMaterial.LessonId);
            return View(courseMaterial);
        }

        // GET: CourseMaterial/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseMaterial = await _context.CourseMaterials
                .Include(c => c.Course)
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.Lesson)
                .FirstOrDefaultAsync(m => m.MaterialId == id);
            if (courseMaterial == null)
            {
                return NotFound();
            }

            return View(courseMaterial);
        }

        // POST: CourseMaterial/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var courseMaterial = await _context.CourseMaterials.FindAsync(id);
            if (courseMaterial != null)
            {
                _context.CourseMaterials.Remove(courseMaterial);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseMaterialExists(int id)
        {
            return _context.CourseMaterials.Any(e => e.MaterialId == id);
        }
    }
}
