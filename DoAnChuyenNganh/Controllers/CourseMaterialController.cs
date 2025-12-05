using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace DoAnChuyenNganh.Controllers
{
    public class CourseMaterialController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CourseMaterialController(DoAnChuyenNganhContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Helper: chỉ Instructor + Admin mới được vào
        private bool HasInstructorOrAdminPermission()
        {
            // Ưu tiên check theo RoleName trong Session
            var roleName = HttpContext.Session.GetString("RoleName");
            if (!string.IsNullOrEmpty(roleName))
            {
                if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    roleName.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Nếu có PermissionLevel thì dùng: 3 = Instructor, 4 = Admin
            var permissionLevel = HttpContext.Session.GetInt32("PermissionLevel");
            if (permissionLevel.HasValue && permissionLevel.Value >= 3)
            {
                return true;
            }

            return false;
        }

        private IActionResult ForbidToHome()
        {
            TempData["ErrorMessage"] = "You do not have permission to access this area.";
            return RedirectToAction("Index", "Home");
        }

        // GET: CourseMaterial
        public async Task<IActionResult> Index()
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            var doAnChuyenNganhContext = _context.CourseMaterials
                .Include(c => c.Course)
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.Lesson);
            return View(await doAnChuyenNganhContext.ToListAsync());
        }

        // GET: CourseMaterial/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

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
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            // Hiển thị tên Course thay vì ID
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title");

            // Không cần load ViewData["LessonId"] vì sẽ load qua AJAX

            return View();
        }

        // POST: CourseMaterial/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaterialId,CourseId,LessonId,MaterialName")] CourseMaterial courseMaterial, IFormFile uploadedFile, bool IsPublic = false)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            try
            {
                // Validate: Phải chọn ít nhất Course hoặc Lesson
                if (!courseMaterial.CourseId.HasValue && !courseMaterial.LessonId.HasValue)
                {
                    ModelState.AddModelError("", "Vui lòng chọn ít nhất Course hoặc Lesson!");
                    ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title");
                    ViewData["LessonId"] = new SelectList(_context.Lessons.Where(l => l.IsPublished == true), "LessonId", "Title");
                    return View(courseMaterial);
                }

                // Lấy UserId từ session hoặc User đang đăng nhập
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    // Nếu không có trong claims, thử lấy từ session
                    var userId = HttpContext.Session.GetInt32("UserId");
                    if (userId.HasValue)
                    {
                        courseMaterial.CreatedBy = userId.Value;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Bạn cần đăng nhập để thực hiện chức năng này.");
                        ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", courseMaterial.CourseId);
                        ViewData["LessonId"] = new SelectList(_context.Lessons.Where(l => l.IsPublished == true), "LessonId", "Title", courseMaterial.LessonId);
                        return View(courseMaterial);
                    }
                }
                else
                {
                    courseMaterial.CreatedBy = int.Parse(userIdClaim);
                }

                // Xử lý upload file
                if (uploadedFile != null && uploadedFile.Length > 0)
                {
                    // Nếu chỉ chọn Lesson, tự động lấy CourseId từ Lesson
                    if (!courseMaterial.CourseId.HasValue && courseMaterial.LessonId.HasValue)
                    {
                        var lesson = await _context.Lessons.FindAsync(courseMaterial.LessonId.Value);
                        if (lesson != null)
                        {
                            courseMaterial.CourseId = lesson.CourseId;
                        }
                    }

                    // Nếu chọn cả Course và Lesson, kiểm tra lesson có thuộc course không
                    if (courseMaterial.CourseId.HasValue && courseMaterial.LessonId.HasValue)
                    {
                        var lesson = await _context.Lessons.FindAsync(courseMaterial.LessonId.Value);
                        if (lesson == null || lesson.CourseId != courseMaterial.CourseId.Value)
                        {
                            ModelState.AddModelError("LessonId", "Bài học không thuộc khóa học đã chọn.");
                            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", courseMaterial.CourseId);
                            ViewData["LessonId"] = new SelectList(_context.Lessons.Where(l => l.IsPublished == true), "LessonId", "Title", courseMaterial.LessonId);
                            return View(courseMaterial);
                        }
                    }

                    // Tạo thư mục lưu file nếu chưa có
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "materials");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Tạo tên file unique
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Lưu file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream);
                    }

                    // Lưu thông tin file vào model
                    courseMaterial.FileUrl = "/uploads/materials/" + uniqueFileName;
                    courseMaterial.FileSize = uploadedFile.Length;

                    // Tự động nhận dạng MaterialType từ extension
                    string fileExtension = Path.GetExtension(uploadedFile.FileName).ToLower();
                    courseMaterial.MaterialType = GetMaterialType(fileExtension);

                    // Nếu MaterialName trống, dùng tên file
                    if (string.IsNullOrEmpty(courseMaterial.MaterialName))
                    {
                        courseMaterial.MaterialName = Path.GetFileNameWithoutExtension(uploadedFile.FileName);
                    }
                }
                else
                {
                    ModelState.AddModelError("uploadedFile", "Vui lòng chọn file để upload.");
                    ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", courseMaterial.CourseId);
                    ViewData["LessonId"] = new SelectList(_context.Lessons, "LessonId", "Title", courseMaterial.LessonId);
                    return View(courseMaterial);
                }

                // Set các giá trị mặc định
                courseMaterial.CreatedAt = DateTime.Now;
                courseMaterial.DownloadCount = 0;
                courseMaterial.IsPublic = IsPublic;

                // Xóa validation cho các field không cần thiết
                ModelState.Remove("MaterialType");
                ModelState.Remove("FileUrl");
                ModelState.Remove("FileSize");
                ModelState.Remove("CreatedBy");
                ModelState.Remove("Course");
                ModelState.Remove("Lesson");
                ModelState.Remove("CreatedByNavigation");

                if (ModelState.IsValid)
                {
                    _context.Add(courseMaterial);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tài liệu đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Debug: Hiển thị lỗi validation
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", courseMaterial.CourseId);
            ViewData["LessonId"] = new SelectList(_context.Lessons.Where(l => l.IsPublished == true), "LessonId", "Title", courseMaterial.LessonId);
            return View(courseMaterial);
        }

        // Hàm xác định MaterialType từ file extension
        private string GetMaterialType(string fileExtension)
        {
            switch (fileExtension)
            {
                case ".pdf":
                case ".doc":
                case ".docx":
                case ".txt":
                case ".xls":
                case ".xlsx":
                case ".ppt":
                case ".pptx":
                    return "document";

                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                case ".svg":
                    return "image";

                case ".mp4":
                case ".avi":
                case ".mov":
                case ".wmv":
                case ".flv":
                case ".mkv":
                    return "video";

                case ".mp3":
                case ".wav":
                case ".flac":
                case ".aac":
                case ".ogg":
                    return "audio";

                default:
                    return "document";
            }
        }

        // GET: CourseMaterial/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            if (id == null)
            {
                return NotFound();
            }

            var courseMaterial = await _context.CourseMaterials
                .Include(c => c.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.MaterialId == id);

            if (courseMaterial == null)
            {
                return NotFound();
            }

            // Hiển thị CourseName & Lesson Title
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", courseMaterial.CourseId);
            ViewData["LessonId"] = new SelectList(_context.Lessons.Where(l => l.CourseId == courseMaterial.CourseId),
                                                  "LessonId", "Title", courseMaterial.LessonId);

            return View(courseMaterial);
        }

        // POST: CourseMaterial/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CourseMaterial formModel, IFormFile? uploadedFile, string? IsPublic)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            if (id != formModel.MaterialId)
            {
                return NotFound();
            }

            // Lấy record gốc từ DB
            var existing = await _context.CourseMaterials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialId == id);

            if (existing == null)
            {
                return NotFound();
            }

            try
            {
                // Validate: phải chọn ít nhất Course hoặc Lesson
                if (!formModel.CourseId.HasValue && !formModel.LessonId.HasValue)
                {
                    ModelState.AddModelError("", "Vui lòng chọn ít nhất Course hoặc Lesson!");
                    ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", formModel.CourseId);
                    ViewData["LessonId"] = new SelectList(_context.Lessons.Where(l => l.CourseId == formModel.CourseId),
                                                          "LessonId", "Title", formModel.LessonId);
                    return View(formModel);
                }

                // Nếu chỉ chọn Lesson → tự động lấy CourseId từ Lesson
                if (!formModel.CourseId.HasValue && formModel.LessonId.HasValue)
                {
                    var lesson = await _context.Lessons.FindAsync(formModel.LessonId.Value);
                    if (lesson != null)
                    {
                        formModel.CourseId = lesson.CourseId;
                    }
                }

                // Kiểm tra nếu chọn cả Course và Lesson, lesson phải thuộc course
                if (formModel.CourseId.HasValue && formModel.LessonId.HasValue)
                {
                    var lesson = await _context.Lessons.FindAsync(formModel.LessonId.Value);
                    if (lesson == null || lesson.CourseId != formModel.CourseId.Value)
                    {
                        ModelState.AddModelError("LessonId", "Bài học không thuộc khóa học đã chọn.");
                        ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", formModel.CourseId);
                        ViewData["LessonId"] = new SelectList(_context.Lessons.Where(l => l.CourseId == formModel.CourseId),
                                                              "LessonId", "Title", formModel.LessonId);
                        return View(formModel);
                    }
                }

                // Xử lý IsPublic từ checkbox
                formModel.IsPublic = IsPublic == "true";

                // Giữ lại các giá trị cũ không được phép thay đổi
                formModel.CreatedAt = existing.CreatedAt;
                formModel.CreatedBy = existing.CreatedBy;
                formModel.DownloadCount = existing.DownloadCount;
                formModel.FileUrl = existing.FileUrl;
                formModel.FileSize = existing.FileSize;
                formModel.MaterialType = existing.MaterialType;

                // Xử lý upload file mới (nếu có)
                if (uploadedFile != null && uploadedFile.Length > 0)
                {
                    // Xóa file cũ nếu có
                    if (!string.IsNullOrEmpty(existing.FileUrl))
                    {
                        string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existing.FileUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Upload file mới
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "materials");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream);
                    }

                    formModel.FileUrl = "/uploads/materials/" + uniqueFileName;
                    formModel.FileSize = uploadedFile.Length;
                    formModel.MaterialType = GetMaterialType(Path.GetExtension(uploadedFile.FileName).ToLower());
                }

                // Xóa validation cho navigation properties
                ModelState.Remove("Course");
                ModelState.Remove("Lesson");
                ModelState.Remove("CreatedByNavigation");

                if (ModelState.IsValid)
                {
                    _context.Update(formModel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Course material updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseMaterialExists(id))
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
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", formModel.CourseId);
            ViewData["LessonId"] = new SelectList(_context.Lessons.Where(l => l.CourseId == formModel.CourseId),
                                                  "LessonId", "Title", formModel.LessonId);

            return View(formModel);
        }

        // GET: CourseMaterial/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

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
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            var courseMaterial = await _context.CourseMaterials.FindAsync(id);
            if (courseMaterial != null)
            {
                // Xóa file vật lý nếu có
                if (!string.IsNullOrEmpty(courseMaterial.FileUrl))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, courseMaterial.FileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.CourseMaterials.Remove(courseMaterial);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseMaterialExists(int id)
        {
            return _context.CourseMaterials.Any(e => e.MaterialId == id);
        }

        // API để load Lessons theo CourseId (dùng cho AJAX)
        [HttpGet]
        public IActionResult GetLessonsByCourse(int? courseId)
        {
            if (!HasInstructorOrAdminPermission())
                return Unauthorized(new { message = "You do not have permission to access this resource." });

            try
            {
                if (courseId.HasValue && courseId.Value > 0)
                {
                    // Load lessons của course cụ thể
                    var lessons = _context.Lessons
                        .Where(l => l.CourseId == courseId.Value)
                        .Select(l => new { lessonId = l.LessonId, title = l.Title })
                        .ToList();

                    return Json(lessons);
                }
                else
                {
                    // Load tất cả lessons
                    var lessons = _context.Lessons
                        .Select(l => new { lessonId = l.LessonId, title = l.Title })
                        .OrderBy(l => l.title)
                        .ToList();

                    return Json(lessons);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
