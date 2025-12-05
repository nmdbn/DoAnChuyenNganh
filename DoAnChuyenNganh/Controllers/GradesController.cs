using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // để dùng HttpContext.Session
using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.Controllers
{
    public class GradesController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;

        public GradesController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        // GET: Grades
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            return View(await _context.Grades.ToListAsync());
        }

        // GET: Grades/Details/5
        public async Task<IActionResult> Details(short? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .FirstOrDefaultAsync(m => m.GradeId == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // GET: Grades/Create
        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Grades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GradeName,GradeLevel,Description,IsActive")] Grade grade)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                grade.CreatedAt = DateTime.Now;
                grade.UpdatedAt = DateTime.Now;
                _context.Add(grade);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Grade created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(grade);
        }

        // GET: Grades/Edit/5
        public async Task<IActionResult> Edit(short? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades.FindAsync(id);
            if (grade == null)
            {
                return NotFound();
            }
            return View(grade);
        }

        // POST: Grades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(short id, [Bind("GradeId,GradeName,GradeLevel,Description,IsActive")] Grade grade)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            if (id != grade.GradeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    grade.UpdatedAt = DateTime.Now;
                    _context.Update(grade);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Grade updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GradeExists(grade.GradeId))
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
            return View(grade);
        }

        // GET: Grades/Delete/5
        public async Task<IActionResult> Delete(short? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .FirstOrDefaultAsync(m => m.GradeId == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // POST: Grades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            var grade = await _context.Grades.FindAsync(id);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Grade deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool GradeExists(short id)
        {
            return _context.Grades.Any(e => e.GradeId == id);
        }

        // Chỉ cho user có RoleName = "Admin" (lưu trong Session) được vào
        private bool IsAdmin()
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            return !string.IsNullOrEmpty(roleName) &&
                   roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}