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
    public class SubjectController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;

        public SubjectController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        // GET: Subject
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            return View(await _context.Subjects.ToListAsync());
        }

        // GET: Subject/Details/5
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

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(m => m.SubjectId == id);
            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        // GET: Subject/Create
        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Subject/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SubjectId,SubjectName,SubjectCode,Description,IconUrl,IsActive,CreatedAt,UpdatedAt")] Subject subject)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                subject.CreatedAt = DateTime.Now;
                subject.UpdatedAt = DateTime.Now;
                _context.Add(subject);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Subject created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(subject);
        }

        // GET: Subject/Edit/5
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

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return NotFound();
            }
            return View(subject);
        }

        // POST: Subject/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(short id, [Bind("SubjectId,SubjectName,SubjectCode,Description,IconUrl,IsActive,CreatedAt,UpdatedAt")] Subject subject)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            if (id != subject.SubjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    subject.UpdatedAt = DateTime.Now;
                    _context.Update(subject);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Subject updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubjectExists(subject.SubjectId))
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
            return View(subject);
        }

        // GET: Subject/Delete/5
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

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(m => m.SubjectId == id);
            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        // POST: Subject/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            var subject = await _context.Subjects.FindAsync(id);
            if (subject != null)
            {
                _context.Subjects.Remove(subject);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Subject deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool SubjectExists(short id)
        {
            return _context.Subjects.Any(e => e.SubjectId == id);
        }

        // Helper method để kiểm tra quyền Admin
        private bool IsAdmin()
        {
            // Kiểm tra RoleName từ Session
            var roleName = HttpContext.Session.GetString("RoleName");

            // So sánh RoleName với "Admin" (không phân biệt hoa thường)
            return !string.IsNullOrEmpty(roleName) &&
                   roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}