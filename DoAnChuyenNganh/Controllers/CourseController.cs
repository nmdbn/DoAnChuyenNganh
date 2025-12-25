using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.Services;
using DoAnChuyenNganh.ViewModels.Payments;
using DoAnChuyenNganh.ViewModels.CoursesViewModels;
using System.Security.Claims;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using CourseQuizVM = DoAnChuyenNganh.ViewModels.Course;
using DoAnChuyenNganh.ViewModels.Course; // ✅ CRITICAL

namespace DoAnChuyenNganh.Controllers
{
    public class CourseController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IPaypalService _paypalService;
        private readonly DoAnChuyenNganhContext _context;
        private readonly IMomoService _momoService;
        private readonly IVnPayService _vnPayService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailService _emailService;

        public CourseController(DoAnChuyenNganhContext context, IMomoService momoService, IPaypalService paypalService, IConfiguration config, IVnPayService vnPayService, IWebHostEnvironment webHostEnvironment, IEmailService emailService)
        {
            _context = context;
            _momoService = momoService;
            _paypalService = paypalService;
            _config = config;
            _vnPayService = vnPayService;
            _webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
        }

        // =============================================
        // MANAGEMENT ACTIONS (Admin & Instructor Only)
        // =============================================

        // GET: Course - Course list (Admin & Instructor only)
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to access this page.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdminOrInstructor())
            {
                TempData["ErrorMessage"] = "Access denied. Only Instructors and Administrators can access course management.";
                return RedirectToAction("Public", "Course");
            }

            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            IQueryable<Course> courses = _context.Courses
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.Grade)
                .Include(c => c.Instructor)
                .Include(c => c.Subject)
                .Include(c => c.UpdatedByNavigation);

            // ✅ Instructor chỉ thấy course của họ
            if (!isAdmin)
            {
                courses = courses.Where(c => c.InstructorId == userId);
            }

            courses = courses.OrderByDescending(c => c.CreatedAt);

            // (optional) dùng cho view để ẩn/hiện nút
            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentUserId = userId;

            return View(await courses.ToListAsync());
        }

        // GET: Course/Details/5 (Admin & Instructor only)
        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to access this page.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.Grade)
                .Include(c => c.Instructor)
                .Include(c => c.Subject)
                .Include(c => c.UpdatedByNavigation)
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null) return NotFound();

            if (!IsAdminOrInstructor())
            {
                TempData["ErrorMessage"] = "You do not have permission to view course management details.";
                return RedirectToAction("Public", "Course");
            }

            return View(course);
        }

        // GET: Course/Create (Admin & Instructor only)
        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to create courses.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdminOrInstructor())
            {
                TempData["ErrorMessage"] = "Only Instructors and Administrators can create courses.";
                return RedirectToAction("Public", "Course");
            }

            var currentUserId = GetCurrentUserId();
            var currentUser = _context.Users.Find(currentUserId);

            ViewData["CurrentUserFullName"] = currentUser != null
                ? $"{currentUser.FirstName} {currentUser.LastName}".Trim()
                : "You";

            ViewData["CurrentUserDisplay"] = currentUser != null
                ? $"{currentUser.FirstName} {currentUser.LastName} (@{currentUser.Username})"
                : "You";

            PopulateDropDownLists();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Slug,Description,ShortDescription,SubjectId,GradeId,ThumbnailUrl,DifficultyLevel,EstimatedHours,Price,IsPublished")]
            Course course,
            IFormFile ThumbnailFile) // ✅ Thêm parameter
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to create courses.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdminOrInstructor())
            {
                TempData["ErrorMessage"] = "You do not have permission to create courses.";
                return RedirectToAction("Public", "Course");
            }

            // Remove validations
            ModelState.Remove("InstructorId");
            ModelState.Remove("Instructor");
            ModelState.Remove("Subject");
            ModelState.Remove("Grade");
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("UpdatedByNavigation");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("UpdatedBy");
            ModelState.Remove("IsPublished");
            ModelState.Remove("IsFeatured");
            ModelState.Remove("ViewCount");
            ModelState.Remove("EnrollmentCount");
            ModelState.Remove("AverageRating");
            ModelState.Remove("RatingCount");
            ModelState.Remove("PublishedAt");

            if (ModelState.IsValid)
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                {
                    TempData["ErrorMessage"] = "Unable to identify current user.";
                    return RedirectToAction("Login", "Account");
                }

                // ✅ XỬ LÝ UPLOAD FILE
                string uploadedFileUrl = null;
                long fileSize = 0;

                if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                {
                    var uploadResult = await UploadThumbnailFile(ThumbnailFile);
                    if (uploadResult.success)
                    {
                        uploadedFileUrl = uploadResult.url;
                        fileSize = ThumbnailFile.Length;
                        course.ThumbnailUrl = uploadedFileUrl; // Gán URL vào course
                    }
                    else
                    {
                        TempData["ErrorMessage"] = uploadResult.error;
                        PopulateDropDownLists(course);
                        return View(course);
                    }
                }

                course.InstructorId = currentUserId;
                course.CreatedBy = currentUserId;
                course.CreatedAt = DateTime.Now;
                course.UpdatedAt = DateTime.Now;

                var isPublished = course.IsPublished;
                course.IsPublished = isPublished;
                course.IsFeatured = false;

                if (isPublished)
                {
                    course.PublishedAt = DateTime.Now;
                }
                else
                {
                    course.PublishedAt = null;
                }

                course.ViewCount = 0;
                course.EnrollmentCount = 0;
                course.AverageRating = 0;
                course.RatingCount = 0;
                course.Price ??= 0;

                _context.Add(course);
                await _context.SaveChangesAsync();

                // ✅ LƯU VÀO BẢNG COURSEMATERIALS
                if (!string.IsNullOrEmpty(uploadedFileUrl))
                {
                    var material = new CourseMaterial
                    {
                        CourseId = course.CourseId,
                        LessonId = null,
                        MaterialName = "Course Thumbnail",
                        MaterialType = "image",
                        FileUrl = uploadedFileUrl,
                        FileSize = fileSize,
                        IsPublic = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = currentUserId
                    };

                    _context.CourseMaterials.Add(material);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Course \"{course.Title}\" created successfully!";
                return RedirectToAction(nameof(Index));
            }

            PopulateDropDownLists(course);
            return View(course);
        }


        // GET: Course/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to edit courses.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null) return NotFound();

            if (!CanEditCourse(course))
            {
                TempData["ErrorMessage"] = "You can only edit courses you created.";
                return RedirectToAction(nameof(Index));
            }

            string instructorName = "Unknown";
            if (course.Instructor != null)
            {
                instructorName = $"{course.Instructor.FirstName} {course.Instructor.LastName} (@{course.Instructor.Username})";
            }
            ViewData["CurrentInstructorName"] = instructorName;

            PopulateDropDownLists(course);
            return View(course);
        }

        // POST: Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("CourseId,Title,Slug,Description,ShortDescription,SubjectId,GradeId,ThumbnailUrl,DifficultyLevel,EstimatedHours,Price,IsPublished,IsFeatured")]
    Course course,
            IFormFile ThumbnailFile)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to edit courses.";
                return RedirectToAction("Login", "Account");
            }

            if (id != course.CourseId) return NotFound();

            // ✅ Lấy course hiện tại KHÔNG dùng AsNoTracking (để có thể update)
            var existingCourse = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (existingCourse == null) return NotFound();

            if (!CanEditCourse(existingCourse))
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this course.";
                return RedirectToAction(nameof(Index));
            }

            // Remove validations
            ModelState.Remove("InstructorId");
            ModelState.Remove("Instructor");
            ModelState.Remove("Subject");
            ModelState.Remove("Grade");
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("UpdatedByNavigation");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedBy");
            ModelState.Remove("UpdatedAt");
            ModelState.Remove("PublishedAt");
            ModelState.Remove("ThumbnailFile"); // ✅ Thêm dòng này

            // ✅ Debug ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new {
                        Field = x.Key,
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    })
                    .ToList();

                foreach (var error in errors)
                {
                    Console.WriteLine($"❌ Field: {error.Field}");
                    foreach (var msg in error.Errors)
                    {
                        Console.WriteLine($"   - {msg}");
                    }
                }

                TempData["ErrorMessage"] = "Please fix the errors in the form.";
                PopulateDropDownLists(course);

                var instructor = await _context.Users.FindAsync(existingCourse.InstructorId);
                ViewData["CurrentInstructorName"] = instructor != null
                    ? $"{instructor.FirstName} {instructor.LastName} (@{instructor.Username})"
                    : "Unknown";

                return View(course);
            }

            try
            {
                var currentUserId = GetCurrentUserId();

                // ✅ XỬ LÝ UPLOAD FILE MỚI
                if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                {
                    var uploadResult = await UploadThumbnailFile(ThumbnailFile);
                    if (uploadResult.success)
                    {
                        existingCourse.ThumbnailUrl = uploadResult.url;

                        // Lưu vào CourseMaterials
                        var material = new CourseMaterial
                        {
                            CourseId = existingCourse.CourseId,
                            LessonId = null,
                            MaterialName = "Course Thumbnail - Updated",
                            MaterialType = "image",
                            FileUrl = uploadResult.url,
                            FileSize = ThumbnailFile.Length,
                            IsPublic = true,
                            CreatedAt = DateTime.Now,
                            CreatedBy = currentUserId
                        };

                        _context.CourseMaterials.Add(material);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = uploadResult.error;
                        PopulateDropDownLists(course);

                        var instructor = await _context.Users.FindAsync(existingCourse.InstructorId);
                        ViewData["CurrentInstructorName"] = instructor != null
                            ? $"{instructor.FirstName} {instructor.LastName} (@{instructor.Username})"
                            : "Unknown";

                        return View(course);
                    }
                }
                else if (!string.IsNullOrEmpty(course.ThumbnailUrl))
                {
                    // ✅ Nếu user thay đổi URL (không upload file)
                    existingCourse.ThumbnailUrl = course.ThumbnailUrl;
                }

                // ✅ CẬP NHẬT CÁC TRƯỜNG CƠ BẢN
                existingCourse.Title = course.Title;
                existingCourse.Slug = course.Slug;
                existingCourse.Description = course.Description;
                existingCourse.ShortDescription = course.ShortDescription;
                existingCourse.SubjectId = course.SubjectId;
                existingCourse.GradeId = course.GradeId;
                existingCourse.DifficultyLevel = course.DifficultyLevel;
                existingCourse.EstimatedHours = course.EstimatedHours;
                existingCourse.Price = course.Price;
                existingCourse.IsPublished = course.IsPublished;
                existingCourse.IsFeatured = course.IsFeatured;

                // Cập nhật metadata
                existingCourse.UpdatedAt = DateTime.Now;
                existingCourse.UpdatedBy = currentUserId;

                // ✅ Xử lý PublishedAt
                if (course.IsPublished && !existingCourse.IsPublished)
                {
                    existingCourse.PublishedAt = DateTime.Now;
                }
                else if (!course.IsPublished)
                {
                    existingCourse.PublishedAt = null;
                }
                // Nếu đã published trước đó và vẫn published → giữ nguyên PublishedAt

                // ✅ KHÔNG CẦN gọi _context.Update(existingCourse) 
                // vì existingCourse đã được tracked

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "✅ Course updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CourseExists(course.CourseId))
                {
                    return NotFound();
                }
                else
                {
                    Console.WriteLine($"❌ Concurrency Error: {ex.Message}");
                    ModelState.AddModelError("", "The course was modified by another user. Please refresh and try again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", "Error saving: " + ex.Message);
            }

            var instructorForView = await _context.Users.FindAsync(existingCourse.InstructorId);
            ViewData["CurrentInstructorName"] = instructorForView != null
                ? $"{instructorForView.FirstName} {instructorForView.LastName} (@{instructorForView.Username})"
                : "Unknown";

            PopulateDropDownLists(course);
            return View(course);
        }

        // ✅ HELPER METHOD - UPLOAD FILE
        private async Task<(bool success, string url, string error)> UploadThumbnailFile(IFormFile file)
        {
            try
            {
                // Kiểm tra file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return (false, null, "Only image files are allowed (jpg, jpeg, png, gif, webp)");
                }

                // Kiểm tra file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return (false, null, "File size must be less than 5MB");
                }

                // Tạo tên file unique
                var fileName = $"course_{Guid.NewGuid()}{extension}";

                // Đường dẫn thư mục lưu file
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "courses");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Đường dẫn file đầy đủ
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Trả về URL tương đối
                var fileUrl = $"/uploads/courses/{fileName}";

                return (true, fileUrl, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Error uploading file: {ex.Message}");
            }
        }

        // GET: Course/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to delete courses.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Grade)
                .Include(c => c.Subject)
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null) return NotFound();

            if (!CanDeleteCourse(course))
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this course.";
                return RedirectToAction(nameof(Index));
            }

            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to delete courses.";
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                if (!CanDeleteCourse(course))
                {
                    TempData["ErrorMessage"] = "You do not have permission to delete this course.";
                    return RedirectToAction(nameof(Index));
                }

                var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.CourseId == id);
                if (hasEnrollments)
                {
                    TempData["ErrorMessage"] = "Cannot delete course with enrolled students.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Course deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Course/Public - Public course list (Requires login: User, Instructor, Admin)
        public async Task<IActionResult> Public(string searchString, int? subjectId, int? gradeId, bool myCourses = false, int pageNumber = 1)
        {
            // REQUIRE LOGIN
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to view courses.";
                return RedirectToAction("Login", "Account");
            }

            const int pageSize = 9; // Số khóa học mỗi trang
            var userId = GetCurrentUserId();

            // Base query - CHỈ HIỂN THỊ COURSE ĐÃ PUBLISHED
            IQueryable<Course> courses = _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Grade)
                .Include(c => c.Instructor)
                .Where(c => c.IsPublished == true);

            // ✅ FILTER MY COURSES - User đã thanh toán (Completed hoặc CODPending)
            if (myCourses)
            {
                var myPaidCourseIds = await _context.Payments
                    .Where(p => p.UserId == userId &&
                               (p.Status == "Completed" || p.Status == "CODPending"))
                    .Select(p => p.CourseId)
                    .Distinct()
                    .ToListAsync();

                courses = courses.Where(c => myPaidCourseIds.Contains(c.CourseId));
            }

            // Search by title / short description
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                courses = courses.Where(c =>
                    c.Title.Contains(searchString) ||
                    (c.ShortDescription != null && c.ShortDescription.Contains(searchString)));

                ViewData["CurrentFilter"] = searchString;
            }

            // Filter by subject
            if (subjectId.HasValue)
            {
                courses = courses.Where(c => c.SubjectId == subjectId);
                ViewData["SelectedSubject"] = subjectId;
            }

            // Filter by grade
            if (gradeId.HasValue)
            {
                courses = courses.Where(c => c.GradeId == gradeId);
                ViewData["SelectedGrade"] = gradeId;
            }

            // Tính tổng số khóa học trước khi phân trang
            var totalCourses = await courses.CountAsync();

            // Sort: ưu tiên PublishedAt, fallback CreatedAt
            courses = courses.OrderByDescending(c => c.PublishedAt ?? c.CreatedAt);

            // Áp dụng phân trang
            courses = courses.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            // Dropdowns
            ViewData["Subjects"] = new SelectList(
                _context.Subjects
                    .Where(s => s.IsActive == true)
                    .OrderBy(s => s.SubjectName),
                "SubjectId",
                "SubjectName"
            );

            ViewData["Grades"] = new SelectList(
                _context.Grades
                    .Where(g => g.IsActive == true)
                    .OrderBy(g => g.GradeLevel),
                "GradeId",
                "GradeName"
            );

            // ✅ Truyền trạng thái My Courses
            ViewData["MyCourses"] = myCourses;

            var model = await courses.ToListAsync();

            // Truyền thông tin phân trang qua ViewBag
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCourses / (double)pageSize);
            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = pageNumber < ViewBag.TotalPages;
            ViewBag.TotalCourses = totalCourses;

            return View(model);
        }

        // GET: Course/PublicDetails/5
        public async Task<IActionResult> PublicDetails(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Grade)
                .Include(c => c.Instructor)
                .Include(c => c.Lessons.Where(l => l.IsPublished == true))
                .FirstOrDefaultAsync(c => c.CourseId == id && c.IsPublished == true);

            if (course == null) return NotFound();

            // Tăng view count
            course.ViewCount++;
            _context.Update(course);
            await _context.SaveChangesAsync();

            var userId = GetCurrentUserId();
            var price = course.Price.GetValueOrDefault(0m);

            // ✅ CASE 1: KHÓA HỌC MIỄN PHÍ
            if (price <= 0)
            {
                if (userId > 0)
                {
                    // Tạo payment miễn phí nếu chưa có
                    var existingPayment = await _context.Payments
                        .FirstOrDefaultAsync(p =>
                            p.UserId == userId &&
                            p.CourseId == course.CourseId &&
                            p.Status == "Completed");

                    if (existingPayment == null)
                    {
                        var freePayment = new Payment
                        {
                            UserId = userId,
                            CourseId = course.CourseId,
                            Amount = 0,
                            PaymentMethod = "Free",
                            Status = "Completed",
                            CreatedAt = DateTime.Now,
                            PaidAt = DateTime.Now
                        };

                        _context.Payments.Add(freePayment);
                        course.EnrollmentCount++;
                        await _context.SaveChangesAsync();
                    }

                    // Free course → vào học luôn
                    return RedirectToAction("Learning", new { id = course.CourseId });
                }

                // Chưa login → yêu cầu đăng nhập
                TempData["ErrorMessage"] = "Please login to access this free course.";
                return RedirectToAction("Login", "Account");
            }

            // ✅ CASE 2: KHÓA HỌC CÓ PHÍ
            if (userId > 0)
            {
                // Kiểm tra user này đã thanh toán chưa
                bool hasPaid = await _context.Payments
                    .AnyAsync(p =>
                        p.UserId == userId &&
                        p.CourseId == id &&
                        p.Status == "Completed");

                if (hasPaid)
                {
                    // Đã thanh toán → vào học
                    return RedirectToAction("Learning", new { id = course.CourseId });
                }
            }

            // Chưa thanh toán hoặc chưa login → hiển thị trang chi tiết
            return View("PublicDetails", course);
        }


        // POST: Đăng ký miễn phí
        [HttpPost]
        public async Task<IActionResult> EnrollFree(int courseId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null || !course.IsPublished || course.Price > 0) return NotFound();

            // Tạo bản ghi thanh toán miễn phí
            var payment = new Payment
            {
                UserId = userId,
                CourseId = courseId,
                Amount = 0,
                PaymentMethod = "Free",
                Status = "Completed",
                PaidAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            course.EnrollmentCount++;
            await _context.SaveChangesAsync();

            return RedirectToAction("PublicDetails", new { id = courseId });
        }

        
        // =============================================
        // HELPER METHODS
        // =============================================

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }

        private void PopulateDropDownLists(Course course = null)
        {
            ViewData["SubjectId"] = new SelectList(
                _context.Subjects.Where(s => s.IsActive == true).OrderBy(s => s.SubjectName),
                "SubjectId", "SubjectName", course?.SubjectId);

            ViewData["GradeId"] = new SelectList(
                _context.Grades.Where(g => g.IsActive == true).OrderBy(g => g.GradeLevel),
                "GradeId", "GradeName", course?.GradeId);
        }

        // =============================================
        // AUTHORIZATION HELPERS
        // =============================================

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out int id))
            {
                return id;
            }

            // ✅ Trả về 0 nếu không tìm thấy (thay vì 1)
            return 0;
        }

        private bool IsLoggedIn()
        {
            return User.Identity.IsAuthenticated && GetCurrentUserId() > 0;
        }

        private bool IsAdminOrInstructor()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return false;

            var roleName = HttpContext.Session.GetString("RoleName");
            if (!string.IsNullOrEmpty(roleName))
            {
                return roleName == "Admin" || roleName == "Instructor";
            }

            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserId == userId && u.IsActive == true);

            if (user?.Role?.RoleName != null)
            {
                HttpContext.Session.SetString("RoleName", user.Role.RoleName);
                return user.Role.RoleName == "Admin" || user.Role.RoleName == "Instructor";
            }

            return false;
        }

        private bool IsAdmin()
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            if (!string.IsNullOrEmpty(roleName))
            {
                return roleName == "Admin";
            }

            var userId = GetCurrentUserId();
            if (userId == 0) return false;

            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserId == userId && u.IsActive == true);

            if (user?.Role?.RoleName != null)
            {
                HttpContext.Session.SetString("RoleName", user.Role.RoleName);
                return user.Role.RoleName == "Admin";
            }

            return false;
        }

        private bool CanEditCourse(Course course)
        {
            if (course == null) return false;

            var userId = GetCurrentUserId();
            if (userId == 0) return false;

            if (IsAdmin()) return true;

            return course.InstructorId == userId;
        }

        private bool CanDeleteCourse(Course course)
        {
            return CanEditCourse(course);
        }
        [HttpPost]
        public async Task<IActionResult> PayWithMoMo(int courseId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null || !course.IsPublished || (course.Price ?? 0) <= 0)
                return NotFound();

            // Tạo bản ghi Payment ở trạng thái Pending
            var payment = new Payment
            {
                UserId = userId,
                CourseId = courseId,
                Amount = course.Price ?? 0,
                Status = "Pending",
                PaymentMethod = "MoMo",
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // URL trả về & notify (dùng cho MoMo thật)
            var returnUrl = Url.Action("MoMoReturn", "Course", null, Request.Scheme) ?? "";
            var notifyUrl = Url.Action("MoMoNotify", "Course", null, Request.Scheme) ?? "";

            // URL trang fake MoMo (cho UseSandbox = true)
            var fakePaymentUrl = Url.Action(
                "FakeMoMoPayment",
                "Course",
                new { paymentId = payment.PaymentId },
                Request.Scheme
            ) ?? "";

            // GỌI ĐÚNG THỨ TỰ: payment, returnUrl, notifyUrl, fakePaymentUrl
            var url = await _momoService.CreatePaymentUrl(
                payment,
                returnUrl,
                notifyUrl,
                fakePaymentUrl
            );

            return Redirect(url);
        }
        public async Task<IActionResult> MoMoReturn()
        {
            try
            {
                var result = await _momoService.ProcessReturn(Request.Query);

                if (result.Success && result.PaymentId.HasValue)
                {
                    TempData["SuccessMessage"] = "Thanh toán MoMo thành công! Chào mừng bạn đến với khóa học!";
                    return RedirectToAction("PaymentSuccess", new { paymentId = result.PaymentId });
                }

                TempData["ErrorMessage"] = result.Message ?? "Thanh toán thất bại";
                return RedirectToAction("Public");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MoMoReturn Error: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý thanh toán";
                return RedirectToAction("Public");
            }
        }
        [HttpGet]
        public async Task<IActionResult> FakeMoMoPayment(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Course)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                return NotFound();

            return View(payment);
        }
        private string HmacSHA256(string input, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmFakePayment(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment != null && payment.Status == "Pending")
            {
                payment.Status = "Completed";
                payment.PaidAt = DateTime.Now;
                payment.TransactionId = "FAKE_SUCCESS_" + DateTime.Now.Ticks;

                if (payment.Course != null)
                    payment.Course.EnrollmentCount++;

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Thanh toán thành công! Chào mừng bạn đến với khóa học!";
            return RedirectToAction("PaymentSuccess", new { paymentId = payment.PaymentId });
        }


        // MoMo callback server-to-server (IPN) – optional
        [HttpPost]
        public async Task<IActionResult> MoMoNotify()
        {
            // Ở giai đoạn dev fake, có thể chưa cần xử lý gì
            // Nếu dùng MoMo thật: parse body JSON, verify signature tương tự ProcessReturn
            await Task.CompletedTask;
            return Ok();
        }
        // GET: Course/Checkout/5
        public async Task<IActionResult> Checkout(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.CourseId == id && c.IsPublished == true);

            if (course == null) return NotFound();

            var price = course.Price ?? 0m;

            bool isFree = price <= 0m;

            if (isFree)
            {
                return RedirectToAction("Learning", new { id = course.CourseId });
            }

            // Check existing payments
            var latestPayment = await _context.Payments
                .Where(p => p.UserId == userId && p.CourseId == id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestPayment != null)
            {
                switch (latestPayment.Status)
                {
                    case "Completed":
                        return RedirectToAction("Learning", new { id = course.CourseId });

                    case "CODPending":
                        return RedirectToAction("CODPending", new { paymentId = latestPayment.PaymentId });

                    case "Failed":
                    case "Pending":
                        // Show checkout, perhaps with message
                        TempData["InfoMessage"] = "Your previous payment is " + latestPayment.Status + ". You can try again.";
                        break;

                    default:
                        break;
                }
            }

            // If no payment or Failed/Pending, show checkout
            var user = await _context.Users.FindAsync(userId);

            var vm = new CheckoutViewModel
            {
                CourseId = course.CourseId,
                CourseTitle = course.Title,
                ThumbnailUrl = course.ThumbnailUrl,
                Price = price,
                FullName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "",
                PhoneNumber = user?.PhoneNumber ?? ""
            };

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> MoMoIPN()
        {
            try
            {
                // Đọc body từ request
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                System.Diagnostics.Debug.WriteLine($"=== MoMo IPN Received ===");
                System.Diagnostics.Debug.WriteLine(body);

                // Parse JSON với strong-typed model
                var data = JsonConvert.DeserializeObject<MoMoIPNRequest>(body);

                if (data == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot parse MoMo IPN body");
                    return BadRequest();
                }

                System.Diagnostics.Debug.WriteLine($"ResultCode: {data.resultCode}");
                System.Diagnostics.Debug.WriteLine($"OrderId: {data.orderId}");
                System.Diagnostics.Debug.WriteLine($"TransId: {data.transId}");

                // Verify signature (optional nhưng nên có)
                var secretKey = _config["MOMO:SecretKey"];
                var accessKey = _config["MOMO:AccessKey"];

                var rawSignature =
                    $"accessKey={accessKey}" +
                    $"&amount={data.amount}" +
                    $"&extraData={data.extraData}" +
                    $"&message={data.message}" +
                    $"&orderId={data.orderId}" +
                    $"&orderInfo={data.orderInfo}" +
                    $"&orderType={data.orderType}" +
                    $"&partnerCode={data.partnerCode}" +
                    $"&payType={data.payType}" +
                    $"&requestId={data.requestId}" +
                    $"&responseTime={data.responseTime}" +
                    $"&resultCode={data.resultCode}" +
                    $"&transId={data.transId}";

                var computedSignature = HmacSHA256(rawSignature, secretKey);

                if (computedSignature != data.signature)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Invalid signature in IPN");
                    return Unauthorized();
                }

                // resultCode = 0 là thành công
                if (data.resultCode == 0 && !string.IsNullOrEmpty(data.orderId))
                {
                    var payment = await _context.Payments
                        .Include(p => p.Course)
                        .FirstOrDefaultAsync(p => p.MoMoOrderId == data.orderId);

                    if (payment != null && payment.Status == "Pending")
                    {
                        payment.Status = "Completed";
                        payment.PaidAt = DateTime.Now;
                        payment.TransactionId = data.transId;

                        if (payment.Course != null)
                            payment.Course.EnrollmentCount++;

                        await _context.SaveChangesAsync();

                        System.Diagnostics.Debug.WriteLine($"✅ Payment {payment.PaymentId} marked as Completed");
                    }
                    else if (payment != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Payment already processed: {payment.Status}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Payment not found for OrderId: {data.orderId}");
                    }
                }

                // Trả về status 204 No Content để MoMo biết đã nhận được
                return NoContent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ MoMoIPN Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();

                // ✅ Debug: In ra console
                Console.WriteLine($"========== CHECKOUT DEBUG ==========");
                Console.WriteLine($"UserID from claim: {userId}");
                Console.WriteLine($"CourseID: {model.CourseId}");
                Console.WriteLine($"Payment Method: {model.PaymentMethod}");

                if (userId == 0)
                {
                    Console.WriteLine("❌ ERROR: UserID is 0!");
                    TempData["ErrorMessage"] = "Cannot identify user. Please login again.";
                    return RedirectToAction("Login", "Account");
                }

                var course = await _context.Courses.FindAsync(model.CourseId);
                if (course == null || !course.IsPublished) return NotFound();

                if (!ModelState.IsValid)
                    return View(model);

                var price = course.Price ?? 0m;

                // ✅ Tạo payment với UserID đúng
                var payment = new Payment
                {
                    UserId = userId, // Đảm bảo dùng userId lấy từ claim
                    CourseId = course.CourseId,
                    Amount = price,
                    PaymentMethod = model.PaymentMethod,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Province = model.Province,
                    District = model.District,
                    Ward = model.Ward,
                    SpecificAddress = model.SpecificAddress,
                    Note = model.Note
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // ✅ Debug: Kiểm tra payment đã lưu
                Console.WriteLine($"✅ Payment created with ID: {payment.PaymentId}");
                Console.WriteLine($"✅ Payment UserID: {payment.UserId}");

                // Xử lý theo phương thức thanh toán
                switch (model.PaymentMethod)
                {
                    case "COD":
                        payment.Status = "CODPending";
                        payment.TransactionId = "COD_" + DateTime.Now.Ticks;
                        await _context.SaveChangesAsync();

                        Console.WriteLine($"✅ COD order created - Status: CODPending - PaymentID: {payment.PaymentId}");

                        TempData["SuccessMessage"] = "Đặt hàng COD thành công! Đơn hàng của bạn đang chờ được duyệt. Chúng tôi sẽ liên hệ sớm để xác nhận.";

                        return RedirectToAction("CODPending", new { paymentId = payment.PaymentId });

                    case "MoMo":
                        {
                            var fakeUrl = Url.Action(
                                "FakeMoMoPayment",
                                "Course",
                                new { paymentId = payment.PaymentId },
                                Request.Scheme
                            );

                            try
                            {
                                var redirectUrl = await _momoService.CreatePaymentUrl(
                                    payment,
                                    null,
                                    null,
                                    fakeUrl
                                );

                                return Redirect(redirectUrl);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ MoMo Error: {ex.Message}");
                                TempData["ErrorMessage"] = $"Lỗi kết nối MoMo: {ex.Message}";
                                return RedirectToAction("Checkout", new { id = course.CourseId });
                            }
                        }

                    case "PayPal":
                        {
                            try
                            {
                                var approvalUrl = await _paypalService.CreatePayPalOrder(
                                    payment,
                                    Url,
                                    Request.Scheme
                                );
                                return Redirect(approvalUrl);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ PayPal Error: {ex.Message}");
                                TempData["ErrorMessage"] = $"Lỗi kết nối PayPal: {ex.Message}";
                                return RedirectToAction("Checkout", new { id = course.CourseId });
                            }
                        }

                    case "VNPay":
                        {
                            try
                            {
                                Console.WriteLine("========== STARTING VNPAY PAYMENT ==========");

                                var paymentUrl = _vnPayService.CreatePaymentUrl(payment, HttpContext);

                                Console.WriteLine($"✅ Payment URL created successfully");
                                Console.WriteLine($"Redirecting to: {paymentUrl}");

                                return Redirect(paymentUrl);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ VNPay Error: {ex.Message}");
                                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                                TempData["ErrorMessage"] = $"Lỗi kết nối VNPay: {ex.Message}";
                                return RedirectToAction("Checkout", new { id = course.CourseId });
                            }
                        }

                    default:
                        TempData["ErrorMessage"] = "Phương thức thanh toán không hợp lệ.";
                        return RedirectToAction("Checkout", new { id = course.CourseId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Checkout Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình thanh toán.";
                return RedirectToAction("Public");
            }
        }
        public async Task<IActionResult> CODPending(long paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null || payment.Status != "CODPending")
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng COD hoặc đơn hàng đã được xử lý.";
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra quyền sở hữu (tùy chọn, nên kiểm tra userId)
            var userId = GetCurrentUserId();
            if (payment.UserId != userId)
            {
                return Forbid();
            }

            return View(payment);
        }

        public async Task<IActionResult> PayPalReturn(string token, int paymentId)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Thiếu token PayPal.";
                return RedirectToAction("Public", "Course");
            }

            var success = await _paypalService.CapturePayPalOrder(token, paymentId);

            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy giao dịch sau khi thanh toán.";
                return RedirectToAction("Public", "Course");
            }

            if (!success)
            {
                TempData["ErrorMessage"] = "Thanh toán PayPal thất bại hoặc chưa hoàn tất.";
                return RedirectToAction("Checkout", new { id = payment.CourseId });
            }

            // ✅ Đổi redirect:
            TempData["SuccessMessage"] = "Thanh toán PayPal thành công, khóa học đã được mở.";
            return RedirectToAction("PaymentSuccess", new { paymentId = payment.PaymentId });

        }
        public async Task<IActionResult> PayPalCancel(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                payment.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }

            TempData["ErrorMessage"] = "Bạn đã hủy thanh toán PayPal.";
            return RedirectToAction("Checkout", new { id = payment?.CourseId });
        }
        public async Task<IActionResult> PaymentSuccess(int paymentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var payment = await _context.Payments
                .Include(p => p.Course)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.UserId == userId);

            if (payment == null)
                return NotFound();

            // Chỉ tạo Enrollment khi payment thành công
            if (payment.Status == "Completed")
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == payment.CourseId);

                if (enrollment == null)
                {
                    enrollment = new Enrollment
                    {
                        UserId = userId,
                        CourseId = payment.CourseId,
                        EnrolledAt = DateTime.Now,
                        IsActive = true
                        // các field khác để default/null
                    };

                    _context.Enrollments.Add(enrollment);
                    await _context.SaveChangesAsync();
                }
            }

            return View(payment); // View: Views/Course/PaymentSuccess.cshtml
        }


        public async Task<IActionResult> Learning(int id, int? lessonId, int? attemptId)
        {
            var userId = GetCurrentUserId();

            // ✅ Kiểm tra đăng nhập
            if (userId == 0)
            {
                TempData["ErrorMessage"] = "Please login to access this course.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy course
            var course = await _context.Courses
                .Include(c => c.Lessons.Where(l => l.IsPublished == true))
                .FirstOrDefaultAsync(c => c.CourseId == id && c.IsPublished == true);

            if (course == null)
                return NotFound();

            var price = course.Price.GetValueOrDefault(0m);
            // ✅ Kiểm tra quyền truy cập
            if (price > 0)
            {
                // Khóa học có phí → phải thanh toán mới được học
                bool hasPaid = await _context.Payments
                    .AnyAsync(p =>
                        p.UserId == userId &&
                        p.CourseId == id &&
                        p.Status == "Completed");

                if (!hasPaid)
                {
                    TempData["ErrorMessage"] = "You must purchase this course before accessing it.";
                    return RedirectToAction("PublicDetails", new { id = course.CourseId });
                }
            }

            // ✅ Đảm bảo Enrollment tồn tại
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == id);

            if (enrollment == null)
            {
                enrollment = new Enrollment
                {
                    UserId = userId,
                    CourseId = id,
                    EnrolledAt = DateTime.Now,
                    IsActive = true
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();
            }

            var lessons = course.Lessons
                .OrderBy(l => l.LessonOrder)
                .ToList();

            if (!lessons.Any())
            {
                return View("Learning", new LearningViewModel
                {
                    Course = course,
                    Lessons = lessons
                });
            }

            var currentLessonId = lessonId ?? lessons.First().LessonId;
            var currentLesson = lessons.FirstOrDefault(l => l.LessonId == currentLessonId);

            var currentMaterials = await _context.CourseMaterials
                .Where(m => m.CourseId == id && m.LessonId == currentLessonId)
                .OrderBy(m => m.MaterialName)
                .ToListAsync();

            // Cập nhật LessonProgress
            var lp = await _context.LessonProgresses
                .FirstOrDefaultAsync(p =>
                    p.EnrollmentId == enrollment.EnrollmentId &&
                    p.LessonId == currentLessonId);

            if (lp == null)
            {
                lp = new LessonProgress
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    LessonId = currentLessonId,
                    IsCompleted = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    TimeSpent = 0,
                    LastPosition = 0
                };
                _context.LessonProgresses.Add(lp);
            }
            else
            {
                lp.UpdatedAt = DateTime.Now;
            }

            enrollment.LastAccessedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Map progress
            var lessonIds = lessons.Select(l => l.LessonId).ToList();
            var lessonProgresses = await _context.LessonProgresses
                .Where(p => p.EnrollmentId == enrollment.EnrollmentId &&
                            lessonIds.Contains(p.LessonId))
                .ToListAsync();

            var hasProgressDict = lessonProgresses
                .GroupBy(p => p.LessonId)
                .ToDictionary(g => g.Key, g => g.Any());
            bool canRate = false;

            // Kiểm tra có enrollment active
            if (enrollment != null && enrollment.IsActive == true)
            {
                // Đối với khóa học miễn phí: chỉ cần có enrollment
                if (price <= 0)
                {
                    canRate = true;
                }
                else
                {
                    // Đối với khóa học có phí: phải có payment completed
                    canRate = await _context.Payments
                        .AnyAsync(p => p.UserId == userId && p.CourseId == id && p.Status == "Completed");
                }
            }

            // ✅ DEBUG LOG
            Console.WriteLine($"===== RATING DEBUG =====");
            Console.WriteLine($"UserId: {userId}");
            Console.WriteLine($"CourseId: {id}");
            Console.WriteLine($"Price: {price}");
            Console.WriteLine($"Has Enrollment: {enrollment != null}");
            Console.WriteLine($"CanRate: {canRate}");

            // Kiểm tra payment chi tiết
            var paymentCheck = await _context.Payments
                .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == id);

            if (paymentCheck != null)
            {
                Console.WriteLine($"Payment found - Status: {paymentCheck.Status}, Method: {paymentCheck.PaymentMethod}");
            }
            else
            {
                Console.WriteLine("No payment found for this user and course");
            }

            CourseRatingInfo? userRating = null;
            if (canRate)
            {
                var existingRating = await _context.CourseRatings
                    .FirstOrDefaultAsync(r => r.CourseId == id && r.UserId == userId);

                if (existingRating != null)
                {
                    userRating = new CourseRatingInfo
                    {
                        RatingId = existingRating.RatingId,
                        Rating = existingRating.Rating,
                        Review = existingRating.Review,
                        CreatedAt = existingRating.CreatedAt
                    };
                }
            }
            // Quiz logic (giữ nguyên như cũ)
            CourseQuizVM.TakeQuizViewModel? quizToTake = null;
            CourseQuizVM.QuizResultViewModel? quizResult = null;


            if (currentLesson != null && currentLesson.LessonType == "quiz")
            {
                if (attemptId.HasValue)
                {
                    var attempt = await _context.UserQuizAttempts
                        .Include(a => a.User)
                        .Include(a => a.Quiz)
                        .Include(a => a.UserQuizAnswers)
                            .ThenInclude(ua => ua.Question)
                                .ThenInclude(q => q.QuizAnswers)
                        .FirstOrDefaultAsync(a => a.AttemptId == attemptId.Value
                                                  && a.Quiz.LessonId == currentLesson.LessonId);

                    if (attempt != null)
                    {
                        quizResult = new CourseQuizVM.QuizResultViewModel
                        {
                            AttemptId = attempt.AttemptId,
                            QuizTitle = attempt.Quiz.Title,
                            SubmittedAt = attempt.SubmittedAt,
                            TotalScore = attempt.TotalScore,
                            MaxScore = attempt.MaxScore,
                            PercentageScore = attempt.PercentageScore,
                            Status = attempt.Status,

                            Questions = attempt.UserQuizAnswers
                                .GroupBy(ua => ua.QuestionId)
                                .Select(g =>
                                {
                                    var any = g.First();
                                    var q = any.Question;

                                    // ✅ FIXED: Parse SelectedAnswerIds từ chuỗi comma-separated
                                    var selectedIds = new List<int>();
                                    if (!string.IsNullOrWhiteSpace(any.SelectedAnswerIds))
                                    {
                                        selectedIds = any.SelectedAnswerIds
                                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => int.Parse(s.Trim()))
                                            .Distinct()
                                            .ToList();
                                    }

                                    var correctIds = q.QuizAnswers
                                        .Where(a => a.IsCorrect)
                                        .Select(a => a.AnswerId)
                                        .ToList();

                                    // Điểm: lấy từ UserQuizAnswer
                                    var earned = any.EarnedPoints;

                                    return new CourseQuizVM.QuizQuestionResultViewModel
                                    {
                                        QuestionId = q.QuestionId,
                                        QuestionText = q.QuestionText,
                                        QuestionType = q.QuestionType,
                                        Points = q.Points,
                                        EarnedPoints = earned,
                                        AllowMultipleCorrect = q.QuizAnswers.Count(a => a.IsCorrect) > 1,
                                        SelectedAnswerIds = selectedIds, // ✅ List<int> đã parse
                                        CorrectAnswerIds = correctIds,
                                        Explanation = q.Explanation,
                                        Answers = q.QuizAnswers
                                            .OrderBy(a => a.AnswerOrder)
                                            .Select(a => new CourseQuizVM.QuizAnswerTakeViewModel
                                            {
                                                AnswerId = a.AnswerId,
                                                AnswerText = a.AnswerText
                                            }).ToList()
                                    };
                                })
                                .ToList()
                        };
                    }
                }
                else
                {
                    var quiz = await _context.Quizzes
                        .Include(q => q.QuizQuestions)
                            .ThenInclude(q => q.QuizAnswers)
                        .FirstOrDefaultAsync(q => q.LessonId == currentLesson.LessonId);

                    if (quiz != null)
                    {
                        var attempt = new UserQuizAttempt
                        {
                            UserId = userId,
                            QuizId = quiz.QuizId,
                            EnrollmentId = enrollment.EnrollmentId,
                            StartedAt = DateTime.Now,
                            Status = "in_progress"
                        };

                        _context.UserQuizAttempts.Add(attempt);
                        await _context.SaveChangesAsync();

                        var questions = quiz.IsRandomOrder
                            ? quiz.QuizQuestions.OrderBy(_ => Guid.NewGuid()).ToList()
                            : quiz.QuizQuestions.OrderBy(q => q.QuestionOrder).ToList();

                        quizToTake = new CourseQuizVM.TakeQuizViewModel
                        {
                            AttemptId = attempt.AttemptId,
                            LessonTitle = currentLesson.Title,
                            CourseTitle = course.Title,
                            Questions = questions.Select(q => new CourseQuizVM.QuizQuestionTakeViewModel
                            {
                                QuestionId = q.QuestionId,
                                QuestionText = q.QuestionText,
                                QuestionType = q.QuestionType,
                                Points = q.Points,
                                AllowMultipleCorrect = q.QuizAnswers.Count(a => a.IsCorrect) > 1,
                                Answers = q.QuizAnswers
                                    .OrderBy(a => a.AnswerOrder)
                                    .Select(a => new CourseQuizVM.QuizAnswerTakeViewModel
                                    {
                                        AnswerId = a.AnswerId,
                                        AnswerText = a.AnswerText
                                    }).ToList()
                            }).ToList()
                        };
                    }
                }
            }

            var vm = new LearningViewModel
            {
                Course = course,
                Lessons = lessons,
                CurrentLessonId = currentLessonId,
                CurrentMaterials = currentMaterials,
                HasProgressForLesson = hasProgressDict,
                QuizToTake = quizToTake,
                QuizResult = quizResult,
                CanRate = canRate,
                UserRating = userRating
            };

            return View(vm);
        }


        // === ACTION SUBMIT QUIZ (bạn đã có – giữ nguyên là tốt nhất) ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuizFromLearning(
    int courseId,
    int lessonId,
    CourseQuizVM.SubmitQuizViewModel model)
        {
            Console.WriteLine($"=== SUBMIT QUIZ DEBUG ===");
            Console.WriteLine($"AttemptId: {model.AttemptId}");
            Console.WriteLine($"Answers count: {model.Answers?.Count ?? 0}");

            if (model.Answers == null || model.Answers.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng trả lời ít nhất 1 câu hỏi.";
                return RedirectToAction("Learning", new { id = courseId, lessonId });
            }

            foreach (var ans in model.Answers)
            {
                var selectedStr = ans.SelectedAnswerIds != null && ans.SelectedAnswerIds.Any()
                    ? string.Join(",", ans.SelectedAnswerIds)
                    : "NONE";
                Console.WriteLine($"Q{ans.QuestionId}: Selected=[{selectedStr}], Essay={ans.EssayAnswer ?? "NULL"}");
            }

            try
            {
                // ✅ XÓA TẤT CẢ câu trả lời cũ
                var existing = await _context.UserQuizAnswers
                    .Where(x => x.AttemptId == model.AttemptId)
                    .ToListAsync();

                if (existing.Any())
                {
                    _context.UserQuizAnswers.RemoveRange(existing);
                    await _context.SaveChangesAsync();
                }

                // ✅ LƯU CÂU TRẢ LỜI MỚI - MỖI QUESTION CHỈ 1 DÒNG
                foreach (var ans in model.Answers)
                {
                    // Essay question
                    if (!string.IsNullOrWhiteSpace(ans.EssayAnswer))
                    {
                        _context.UserQuizAnswers.Add(new UserQuizAnswer
                        {
                            AttemptId = model.AttemptId,
                            QuestionId = ans.QuestionId,
                            EssayAnswer = ans.EssayAnswer,
                            SelectedAnswerIds = null, // ✅ Không dùng cho essay
                            CreatedAt = DateTime.Now
                        });
                        continue;
                    }

                    // Multiple choice question
                    if (ans.SelectedAnswerIds != null && ans.SelectedAnswerIds.Any())
                    {
                        // ✅ LƯU TẤT CẢ AnswerIDs VÀO 1 DÒNG (format: "1592,1593,1594,1595")
                        var selectedIdsString = string.Join(",", ans.SelectedAnswerIds.Distinct().OrderBy(x => x));

                        _context.UserQuizAnswers.Add(new UserQuizAnswer
                        {
                            AttemptId = model.AttemptId,
                            QuestionId = ans.QuestionId,
                            SelectedAnswerIds = selectedIdsString, // ✅ Lưu chuỗi comma-separated
                            EssayAnswer = null,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();
                
                // Chấm điểm
                await GradeQuizAnswers(model.AttemptId);

                TempData["SuccessMessage"] = "Quiz đã được nộp thành công!";
                return RedirectToAction("Learning", new { id = courseId, lessonId, attemptId = model.AttemptId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Lỗi khi nộp quiz: " + ex.Message;
                return RedirectToAction("Learning", new { id = courseId, lessonId });
            }
        }
        /// <summary>
        /// Chấm điểm quiz bằng C# logic (không dùng stored procedure)
        /// </summary>
        private async Task GradeQuizAnswers(int attemptId)
        {
            // Lấy tất cả câu trả lời của user (giờ mỗi question chỉ 1 dòng)
            var userAnswers = await _context.UserQuizAnswers
                .Include(ua => ua.Question)
                .Where(ua => ua.AttemptId == attemptId)
                .ToListAsync();

            decimal totalScore = 0;
            int maxScore = 0;

            foreach (var userAnswer in userAnswers)
            {
                var question = userAnswer.Question;
                if (question == null) continue;

                maxScore += question.Points;

                // Essay: chưa tự chấm
                if (question.QuestionType == "essay")
                {
                    userAnswer.IsCorrect = null;
                    userAnswer.EarnedPoints = 0;
                    continue;
                }

                // Multiple choice: Parse SelectedAnswerIds
                var selectedIds = new List<int>();
                if (!string.IsNullOrWhiteSpace(userAnswer.SelectedAnswerIds))
                {
                    selectedIds = userAnswer.SelectedAnswerIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.Parse(s.Trim()))
                        .OrderBy(x => x)
                        .ToList();
                }

                // Lấy đáp án đúng từ DB
                var correctIds = await _context.QuizAnswers
                    .Where(a => a.QuestionId == question.QuestionId && a.IsCorrect)
                    .Select(a => a.AnswerId)
                    .OrderBy(x => x)
                    .ToListAsync();

                // ✅ FULL MATCH: User phải chọn đúng TẤT CẢ và KHÔNG CHỌN DƯ
                bool isCorrect =
                    selectedIds.Count == correctIds.Count &&
                    !selectedIds.Except(correctIds).Any();

                if (isCorrect)
                {
                    totalScore += question.Points;
                    userAnswer.IsCorrect = true;
                    userAnswer.EarnedPoints = question.Points;
                }
                else
                {
                    userAnswer.IsCorrect = false;
                    userAnswer.EarnedPoints = 0;
                }
            }

            _context.UserQuizAnswers.UpdateRange(userAnswers);
            await _context.SaveChangesAsync();

            // Cập nhật UserQuizAttempt
            var attempt = await _context.UserQuizAttempts.FirstOrDefaultAsync(a => a.AttemptId == attemptId);
            if (attempt != null)
            {
                attempt.TotalScore = totalScore;
                attempt.MaxScore = maxScore;
                attempt.PercentageScore = maxScore > 0 ? (totalScore * 100 / maxScore) : 0;

                bool hasEssay = userAnswers.Any(ua => ua.Question?.QuestionType == "essay");
                attempt.Status = hasEssay ? "submitted" : "graded";
                attempt.SubmittedAt = DateTime.Now;

                _context.UserQuizAttempts.Update(attempt);
                await _context.SaveChangesAsync();
            }
        }

        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            try
            {
                Console.WriteLine("========== VNPAY RETURN ==========");

                // In ra tất cả query params để debug
                foreach (var param in Request.Query)
                {
                    Console.WriteLine($"{param.Key}: {param.Value}");
                }

                var result = _vnPayService.ProcessReturn(Request.Query);

                if (result.Success && result.PaymentId.HasValue)
                {
                    TempData["SuccessMessage"] = "Thanh toán VNPay thành công!";
                    return RedirectToAction("PaymentSuccess", new { paymentId = result.PaymentId });
                }

                TempData["ErrorMessage"] = result.Message ?? "Thanh toán thất bại";
                return RedirectToAction("Public");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ VnPayReturn Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý kết quả thanh toán VNPay.";
                return RedirectToAction("Public");
            }
        }

        [HttpPost]
        public async Task<IActionResult> VnPayIPN()
        {
            // VNPay IPN callback (nếu cần)
            await Task.CompletedTask;
            return NoContent();
        }
        // Thêm vào CourseController để debug
        [HttpGet]
        public IActionResult DebugAuth()
        {
            var userId = GetCurrentUserId();
            var isAuthenticated = User.Identity.IsAuthenticated;
            var roleName = HttpContext.Session.GetString("RoleName");
            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            var claims = User.Claims.Select(c => new
            {
                Type = c.Type,
                Value = c.Value
            }).ToList();

            var debugInfo = new
            {
                IsAuthenticated = isAuthenticated,
                UserIdFromClaim = userId,
                RoleNameFromSession = roleName ?? "NULL",
                UserIdFromSession = sessionUserId?.ToString() ?? "NULL",
                IsLoggedIn = IsLoggedIn(),
                IsAdminOrInstructor = IsAdminOrInstructor(),
                IsAdmin = IsAdmin(),
                Claims = claims
            };

            return Json(debugInfo);
        }
        // =============================================
        // PAYMENT MANAGEMENT (Admin & Instructor Only)
        // =============================================

        // GET: Course/ManagePayments
        public async Task<IActionResult> ManagePayments(
            string? status,
            string? method,
            string? q,
            DateTime? from,
            DateTime? to)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to access this page.";
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdminOrInstructor())
            {
                TempData["ErrorMessage"] = "Access denied. Only Instructors and Administrators can access payment management.";
                return RedirectToAction("Public", "Course");
            }

            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            IQueryable<Payment> payments = _context.Payments
                .Include(p => p.Course)
                    .ThenInclude(c => c.Instructor)
                .Include(p => p.User);

            // ✅ Instructor chỉ thấy payment của course do mình sở hữu
            if (!isAdmin)
            {
                payments = payments.Where(p => p.Course.InstructorId == userId);
            }

            // Filters
            if (!string.IsNullOrWhiteSpace(status))
                payments = payments.Where(p => p.Status == status);

            if (!string.IsNullOrWhiteSpace(method))
                payments = payments.Where(p => p.PaymentMethod == method);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                payments = payments.Where(p =>
                    p.Course.Title.Contains(q) ||
                    (p.User.Username != null && p.User.Username.Contains(q)) ||
                    ((p.User.FirstName + " " + p.User.LastName).Contains(q)) ||
                    (p.TransactionId != null && p.TransactionId.Contains(q)) ||
                    (p.MoMoOrderId != null && p.MoMoOrderId.Contains(q)) ||
                    (p.VnPayOrderId != null && p.VnPayOrderId.Contains(q))
                );
            }

            if (from.HasValue)
                payments = payments.Where(p => p.CreatedAt >= from.Value);

            if (to.HasValue)
                payments = payments.Where(p => p.CreatedAt <= to.Value.AddDays(1).AddTicks(-1));

            // Sort newest first
            payments = payments.OrderByDescending(p => p.CreatedAt);

            // ViewBags for UI
            ViewBag.IsAdmin = isAdmin;
            ViewBag.Status = status ?? "";
            ViewBag.Method = method ?? "";
            ViewBag.Q = q ?? "";
            ViewBag.From = from?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.To = to?.ToString("yyyy-MM-dd") ?? "";

            // Quick stats (optional nhưng “xịn”)
            var list = await payments.ToListAsync();
            ViewBag.TotalCount = list.Count;
            ViewBag.TotalRevenueCompleted = list
                .Where(p => p.Status == "Completed")
                .Sum(p => p.Amount);

            return View(list);
        }
        [HttpGet]
        public async Task<IActionResult> RateCourse(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                TempData["ErrorMessage"] = "Please login to rate this course.";
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == id && c.IsPublished == true);

            if (course == null)
                return NotFound();

            // Kiểm tra user đã mua khóa học chưa
            var hasPaid = await _context.Payments
                .AnyAsync(p => p.UserId == userId
                            && p.CourseId == id
                            && p.Status == "Completed");

            if (!hasPaid)
            {
                TempData["ErrorMessage"] = "You must purchase this course before rating it.";
                return RedirectToAction("PublicDetails", new { id });
            }

            // Kiểm tra đã đánh giá chưa
            var existingRating = await _context.CourseRatings
                .FirstOrDefaultAsync(r => r.CourseId == id && r.UserId == userId);

            var vm = new CourseRatingViewModel
            {
                CourseId = course.CourseId,
                CourseTitle = course.Title,
                AverageRating = course.AverageRating ?? 0,
                RatingCount = course.RatingCount ?? 0,
                HasRated = existingRating != null
            };

            if (existingRating != null)
            {
                vm.ExistingRating = new CourseRatingInfo
                {
                    RatingId = existingRating.RatingId,
                    Rating = existingRating.Rating,
                    Review = existingRating.Review,
                    CreatedAt = existingRating.CreatedAt
                };
            }

            return View(vm);
        }

        /// <summary>
        /// Xử lý submit đánh giá
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRating(SubmitRatingViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                TempData["ErrorMessage"] = "Please login to rate this course.";
                return RedirectToAction("Login", "Account");
            }

            // Validate Rating
            if (model.Rating < 1 || model.Rating > 5)
            {
                TempData["ErrorMessage"] = "Rating must be between 1 and 5 stars.";
                return RedirectToAction("RateCourse", new { id = model.CourseId });
            }

            // Kiểm tra đã mua khóa học
            var hasPaid = await _context.Payments
                .AnyAsync(p => p.UserId == userId
                            && p.CourseId == model.CourseId
                            && p.Status == "Completed");

            if (!hasPaid)
            {
                TempData["ErrorMessage"] = "You must purchase this course before rating it.";
                return RedirectToAction("PublicDetails", new { id = model.CourseId });
            }

            try
            {
                var existingRating = await _context.CourseRatings
                    .FirstOrDefaultAsync(r => r.CourseId == model.CourseId && r.UserId == userId);

                if (existingRating != null)
                {
                    // Cập nhật rating cũ
                    existingRating.Rating = model.Rating;
                    existingRating.Review = model.Review?.Trim();
                    existingRating.UpdatedAt = DateTime.Now;

                    _context.CourseRatings.Update(existingRating);
                }
                else
                {
                    // Tạo rating mới
                    var newRating = new CourseRating
                    {
                        CourseId = model.CourseId,
                        UserId = userId,
                        Rating = model.Rating,
                        Review = model.Review?.Trim(),
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.CourseRatings.Add(newRating);
                }

                await _context.SaveChangesAsync();

                // ✅ CẬP NHẬT AverageRating VÀ RatingCount CHO COURSE
                await UpdateCourseRatingStats(model.CourseId);

                TempData["SuccessMessage"] = existingRating != null
                    ? "Your rating has been updated successfully!"
                    : "Thank you for rating this course!";

                return RedirectToAction("PaymentSuccess", new { paymentId = await GetPaymentId(userId, model.CourseId) });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error submitting rating: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while submitting your rating.";
                return RedirectToAction("RateCourse", new { id = model.CourseId });
            }
        }

        /// <summary>
        /// Cập nhật AverageRating và RatingCount cho Course
        /// </summary>
        private async Task UpdateCourseRatingStats(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return;

            var ratings = await _context.CourseRatings
                .Where(r => r.CourseId == courseId && r.IsApproved == true)
                .ToListAsync();

            if (ratings.Any())
            {
                course.RatingCount = ratings.Count;
                course.AverageRating = Math.Round((decimal)ratings.Average(r => r.Rating), 2);
            }
            else
            {
                course.RatingCount = 0;
                course.AverageRating = 0;
            }

            _context.Courses.Update(course);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy PaymentId để redirect
        /// </summary>
        private async Task<int> GetPaymentId(int userId, int courseId)
        {
            var payment = await _context.Payments
                .Where(p => p.UserId == userId && p.CourseId == courseId && p.Status == "Completed")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            return payment?.PaymentId ?? 0;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRatingInline(int courseId, byte rating, string? review)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please login to rate this course." });
            }

            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Rating must be between 1 and 5 stars." });
            }

            var hasPaid = await _context.Payments
                .AnyAsync(p => p.UserId == userId && p.CourseId == courseId && p.Status == "Completed");
            var hasPrice = await _context.Courses
                .AnyAsync(p => p.Price > 0);

            if (!hasPaid && !hasPrice)
            {
                return Json(new { success = false, message = "You must purchase this course before rating it." });
            }

            try
            {
                var existingRating = await _context.CourseRatings
                    .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId);

                if (existingRating != null)
                {
                    existingRating.Rating = rating;
                    existingRating.Review = review?.Trim();
                    existingRating.UpdatedAt = DateTime.Now;
                    _context.CourseRatings.Update(existingRating);
                }
                else
                {
                    var newRating = new CourseRating
                    {
                        CourseId = courseId,
                        UserId = userId,
                        Rating = rating,
                        Review = review?.Trim(),
                        IsApproved = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.CourseRatings.Add(newRating);
                }

                await _context.SaveChangesAsync();
                await UpdateCourseRatingStats(courseId);

                var course = await _context.Courses.FindAsync(courseId);

                return Json(new
                {
                    success = true,
                    message = existingRating != null ? "Rating updated successfully!" : "Thank you for rating!",
                    averageRating = course?.AverageRating ?? 0,
                    ratingCount = course?.RatingCount ?? 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while submitting your rating." });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCODPayment(long paymentId)
        {
            try
            {
                // ✅ Kiểm tra đăng nhập và quyền
                if (!IsLoggedIn())
                {
                    TempData["ErrorMessage"] = "Please login to access this page.";
                    return RedirectToAction("Login", "Account");
                }

                if (!IsAdminOrInstructor())
                {
                    TempData["ErrorMessage"] = "Access denied. Only Instructors and Administrators can approve payments.";
                    return RedirectToAction("Public", "Course");
                }

                var userId = GetCurrentUserId();
                var isAdmin = IsAdmin();

                // ✅ Lấy payment với các thông tin liên quan
                var payment = await _context.Payments
                    .Include(p => p.Course)
                        .ThenInclude(c => c.Instructor)
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null)
                {
                    TempData["ErrorMessage"] = "Payment not found.";
                    return RedirectToAction("ManagePayments");
                }

                // ✅ Kiểm tra quyền: Instructor chỉ approve payment của course của mình
                if (!isAdmin && payment.Course?.InstructorId != userId)
                {
                    TempData["ErrorMessage"] = "You can only approve payments for your own courses.";
                    return RedirectToAction("ManagePayments");
                }

                // ✅ Kiểm tra status hiện tại
                if (payment.Status != "CODPending")
                {
                    TempData["ErrorMessage"] = $"Cannot approve payment. Current status: {payment.Status}";
                    return RedirectToAction("ManagePayments");
                }

                // ✅ Cập nhật payment status
                payment.Status = "Completed";
                payment.PaidAt = DateTime.Now;
                payment.TransactionId = $"COD_APPROVED_{DateTime.Now.Ticks}";

                // ✅ Tăng EnrollmentCount cho course
                if (payment.Course != null)
                {
                    payment.Course.EnrollmentCount++;
                }

                // ✅ Tạo Enrollment nếu chưa có
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == payment.UserId && e.CourseId == payment.CourseId);

                if (enrollment == null)
                {
                    enrollment = new Enrollment
                    {
                        UserId = payment.UserId,
                        CourseId = payment.CourseId,
                        EnrolledAt = DateTime.Now,
                        IsActive = true
                    };
                    _context.Enrollments.Add(enrollment);
                }

                await _context.SaveChangesAsync();

                // ✅ Gửi email thông báo
                try
                {
                    if (payment.User != null && !string.IsNullOrEmpty(payment.User.Email))
                    {
                        var studentName = $"{payment.User.FirstName} {payment.User.LastName}".Trim();
                        var courseTitle = payment.Course?.Title ?? "Your Course";

                        await _emailService.SendCODApprovalEmailAsync(
                            payment.User.Email,
                            studentName,
                            courseTitle,
                            payment.Amount
                        );

                        Console.WriteLine($"✅ Email sent to {payment.User.Email}");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ User email not found, skipping email notification");
                    }
                }
                catch (Exception emailEx)
                {
                    // Log lỗi email nhưng không fail toàn bộ transaction
                    Console.WriteLine($"⚠️ Email sending failed: {emailEx.Message}");
                    TempData["WarningMessage"] = "Payment approved successfully, but email notification failed.";
                }

                TempData["SuccessMessage"] = $"✅ COD Payment #{paymentId} has been approved successfully! Email notification sent to student.";
                return RedirectToAction("ManagePayments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error approving COD payment: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"An error occurred while approving payment: {ex.Message}";
                return RedirectToAction("ManagePayments");
            }
        }
    }
}