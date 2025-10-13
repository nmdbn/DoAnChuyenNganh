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
    public class UserController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;

        public UserController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var doAnChuyenNganhContext = _context.Users.Include(u => u.CreatedByNavigation).Include(u => u.Role).Include(u => u.UpdatedByNavigation);
            return View(await doAnChuyenNganhContext.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.CreatedByNavigation)
                .Include(u => u.Role)
                .Include(u => u.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId");
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId");
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Username,Email,PasswordHash,PasswordSalt,FirstName,LastName,DateOfBirth,PhoneNumber,AvatarUrl,Bio,RoleId,IsEmailVerified,IsActive,IsLocked,LastLoginAt,LoginAttempts,LockoutUntil,CreatedAt,UpdatedAt,CreatedBy,UpdatedBy")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.CreatedBy);
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId", user.RoleId);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.UpdatedBy);
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.CreatedBy);
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId", user.RoleId);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.UpdatedBy);
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,Username,Email,PasswordHash,PasswordSalt,FirstName,LastName,DateOfBirth,PhoneNumber,AvatarUrl,Bio,RoleId,IsEmailVerified,IsActive,IsLocked,LastLoginAt,LoginAttempts,LockoutUntil,CreatedAt,UpdatedAt,CreatedBy,UpdatedBy")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
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
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.CreatedBy);
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId", user.RoleId);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.UpdatedBy);
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.CreatedByNavigation)
                .Include(u => u.Role)
                .Include(u => u.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ToggleLock - AJAX endpoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Toggle IsLocked and IsActive (opposite values)
                user.IsLocked = !user.IsLocked;
                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.Now;

                // If locking, set lockout time
                if (user.IsLocked == true)
                {
                    user.LockoutUntil = DateTime.Now.AddYears(100); // Permanent lock
                }
                else
                {
                    user.LockoutUntil = null;
                    user.LoginAttempts = 0;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    isLocked = user.IsLocked,
                    isActive = user.IsActive,
                    message = user.IsLocked == true ? "User has been locked" : "User has been unlocked"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}