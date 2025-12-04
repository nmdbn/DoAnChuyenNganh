using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Http; // để dùng HttpContext.Session.GetString

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
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            var doAnChuyenNganhContext = _context.Users
                .Include(u => u.CreatedByNavigation)
                .Include(u => u.Role)
                .Include(u => u.UpdatedByNavigation);

            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "RoleId", "RoleName");
            return View(await doAnChuyenNganhContext.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
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
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId");
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Username,Email,PasswordHash,PasswordSalt,FirstName,LastName,DateOfBirth,PhoneNumber,AvatarUrl,Bio,RoleId,IsEmailVerified,IsActive,IsLocked,LastLoginAt,LoginAttempts,LockoutUntil,CreatedAt,UpdatedAt,CreatedBy,UpdatedBy")] User user)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "User created successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.CreatedBy);
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.UpdatedBy);
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
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

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.CreatedBy);
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.UpdatedBy);
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            if (id != user.UserId)
            {
                return NotFound();
            }

            // BƯỚC 1: Xóa validation cho các trường không cần thiết
            ModelState.Remove("PasswordHash");
            ModelState.Remove("PasswordSalt");
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("UpdatedByNavigation");
            ModelState.Remove("Role");
            ModelState.Remove("Articles");
            ModelState.Remove("Comments");
            ModelState.Remove("InverseCreatedByNavigation");
            ModelState.Remove("InverseUpdatedByNavigation");

            // BƯỚC 2: Debug - Xem các lỗi còn lại
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new
                    {
                        Field = x.Key,
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    })
                    .ToList();

                // Log errors ra console (có thể xem trong Output window)
                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine($"Field: {error.Field}");
                    foreach (var msg in error.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Error: {msg}");
                    }
                }

                // Trả về view với errors để hiển thị
                ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.CreatedBy);
                ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
                ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.UpdatedBy);

                // Thêm message để biết validation failed
                ViewData["ErrorMessage"] = "Validation failed. Check console for details.";
                return View(user);
            }

            // BƯỚC 3: Lấy thông tin user hiện tại từ DB
            try
            {
                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (existingUser == null)
                {
                    return NotFound();
                }

                // Giữ nguyên password từ DB
                user.PasswordHash = existingUser.PasswordHash;
                user.PasswordSalt = existingUser.PasswordSalt;

                // Cập nhật thời gian
                user.UpdatedAt = DateTime.Now;

                // Attach và update
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Redirect về Index với thông báo thành công
                TempData["SuccessMessage"] = "User updated successfully!";
                return RedirectToAction(nameof(Index));
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
            catch (Exception ex)
            {
                // Log exception
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                ViewData["ErrorMessage"] = $"Error: {ex.Message}";

                ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.CreatedBy);
                ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
                ViewData["UpdatedBy"] = new SelectList(_context.Users, "UserId", "UserId", user.UpdatedBy);
                return View(user);
            }
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
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
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "You do not have permission to access this area.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "User deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // CÁC HÀM KHÁC GIỮ NGUYÊN
        // =========================

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

        // POST: Users/ChangeRole - AJAX endpoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int userId, byte roleId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var role = await _context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    return Json(new { success = false, message = "Role not found" });
                }

                user.RoleId = roleId;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    roleName = role.RoleName,
                    message = $"User role changed to {role.RoleName}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Users/GetRoles - AJAX endpoint to fetch roles
        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .Select(r => new { roleId = r.RoleId, roleName = r.RoleName })
                    .ToListAsync();
                return Json(roles);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Users/Stats
        public async Task<IActionResult> Stats()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive == true),
                LockedUsers = await _context.Users.CountAsync(u => u.IsLocked == true),
                Administrators = await _context.Users.CountAsync(u => u.RoleId == 4)
            };
            return View(stats);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        // Helper method giống SubjectController
        private bool IsAdmin()
        {
            var roleName = HttpContext.Session.GetString("RoleName");

            return !string.IsNullOrEmpty(roleName) &&
                   roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
