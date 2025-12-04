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
namespace DoAnChuyenNganh.Controllers
{
    public class CourseController : Controller
    {
        private readonly IPaypalService _paypalService;
        private readonly DoAnChuyenNganhContext _context;
        private readonly IMomoService _momoService;
        public CourseController(DoAnChuyenNganhContext context, IMomoService momoService, IPaypalService paypalService)
        {
            _context = context;
            _momoService = momoService;
            _paypalService = paypalService;
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

            var courses = _context.Courses
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.Grade)
                .Include(c => c.Instructor)
                .Include(c => c.Subject)
                .Include(c => c.UpdatedByNavigation)
                .OrderByDescending(c => c.CreatedAt);

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

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Slug,Description,ShortDescription,SubjectId,GradeId,ThumbnailUrl,DifficultyLevel,EstimatedHours,Price")] Course course)
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

            // CRITICAL: Remove all navigation property validations
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

                course.InstructorId = currentUserId;
                course.CreatedBy = currentUserId;
                course.CreatedAt = DateTime.Now;
                course.UpdatedAt = DateTime.Now;
                course.IsPublished = false;
                course.IsFeatured = false;
                course.ViewCount = 0;
                course.EnrollmentCount = 0;
                course.AverageRating = 0;
                course.RatingCount = 0;
                course.Price ??= 0;

                _context.Add(course);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Course \"{course.Title}\" created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Debug ModelState errors
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
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
        public async Task<IActionResult> Edit(int id, [Bind("CourseId,Title,Slug,Description,ShortDescription,SubjectId,GradeId,ThumbnailUrl,DifficultyLevel,EstimatedHours,Price,IsPublished,IsFeatured")] Course course)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to edit courses.";
                return RedirectToAction("Login", "Account");
            }

            if (id != course.CourseId) return NotFound();

            var existingCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseId == id);
            if (existingCourse == null) return NotFound();

            if (!CanEditCourse(existingCourse))
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this course.";
                return RedirectToAction(nameof(Index));
            }

            course.InstructorId = existingCourse.InstructorId;

            // CRITICAL: Remove all navigation property validations
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

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserId = GetCurrentUserId();

                    course.CreatedAt = existingCourse.CreatedAt;
                    course.CreatedBy = existingCourse.CreatedBy;
                    course.ViewCount = existingCourse.ViewCount;
                    course.EnrollmentCount = existingCourse.EnrollmentCount;
                    course.AverageRating = existingCourse.AverageRating;
                    course.RatingCount = existingCourse.RatingCount;

                    course.UpdatedAt = DateTime.Now;
                    course.UpdatedBy = currentUserId;

                    if (course.IsPublished && !existingCourse.IsPublished)
                        course.PublishedAt = DateTime.Now;
                    else if (!course.IsPublished)
                        course.PublishedAt = null;
                    else
                        course.PublishedAt = existingCourse.PublishedAt;

                    _context.Update(course);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Course updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving: " + ex.Message);
                }
            }

            var instructor = await _context.Users.FindAsync(course.InstructorId);
            ViewData["CurrentInstructorName"] = instructor != null
                ? $"{instructor.FirstName} {instructor.LastName} (@{instructor.Username})"
                : "Unknown";

            PopulateDropDownLists(course);
            return View(course);
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

        // =============================================
        // PUBLIC ACTIONS (All authenticated users)
        // =============================================

        // GET: Course/Public - Public course list (Requires login: User, Instructor, Admin)
        // GET: Course/Public - Public course list (Requires login: User, Instructor, Admin)
        public async Task<IActionResult> Public(string searchString, int? subjectId, int? gradeId)
        {
            // REQUIRE LOGIN
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login to view courses.";
                return RedirectToAction("Login", "Account");
            }

            // Base query - CHỈ HIỂN THỊ COURSE ĐÃ PUBLISHED
            IQueryable<Course> courses = _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Grade)
                .Include(c => c.Instructor)
                .Where(c => c.IsPublished == true);   // 👈 thêm dòng này

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

            // Sort: ưu tiên PublishedAt, fallback CreatedAt
            courses = courses.OrderByDescending(c => c.PublishedAt ?? c.CreatedAt);

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

            var model = await courses.ToListAsync();
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

            // Tăng view
            course.ViewCount++;
            _context.Update(course);
            await _context.SaveChangesAsync();

            var userId = GetCurrentUserId();
            var price = course.Price.GetValueOrDefault(0m); // xử lý nullable

            if (userId > 0)
            {
                // --------- CASE 1: KHÓA HỌC FREE (Price <= 0) ---------
                if (price <= 0)
                {
                    // Nếu chưa có payment nào (kể cả free) thì tạo 1 payment free
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

                    // Free course → luôn được học như đã thanh toán
                    return View("Learning", course);
                }

                // --------- CASE 2: KHÓA HỌC CÓ THU PHÍ ---------
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

            // Chưa login hoặc chưa thanh toán (với course có phí) → hiển thị trang PublicDetails
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

        private bool IsLoggedIn()
        {
            var userId = GetCurrentUserId();
            return userId > 0;
        }

        private int GetCurrentUserId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            return userId.GetValueOrDefault(0);
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

        [HttpGet]
        public async Task<IActionResult> FakeMoMoPayment(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Course)
                .Include(p => p.User)   // nếu Payment có navigation User
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                return NotFound();

            return View(payment); // View: FakeMoMoPayment.cshtml
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
        public async Task<IActionResult> MoMoReturn()
        {
            var result = await _momoService.ProcessReturn(Request.Query);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Thanh toán thành công! Chào mừng bạn đến với khóa học!";
                // nếu trong PaymentResult có PaymentId thì dùng:
                return RedirectToAction("PaymentSuccess", new { paymentId = result.PaymentId });

                // nếu PaymentResult hiện chỉ có CourseId: tạm thời vẫn redirect PublicDetails,
                // hoặc bạn chỉnh PaymentResult để thêm PaymentId.
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Public");
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

            // Nếu free hoặc đã thanh toán thì vào học luôn
            bool isFree = price <= 0m;
            bool hasPaid = await _context.Payments
                .AnyAsync(p => p.UserId == userId && p.CourseId == id && p.Status == "Completed");

            if (isFree || hasPaid)
                return RedirectToAction("Learning", new { id = course.CourseId });


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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course == null || !course.IsPublished) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var price = course.Price ?? 0m;

            // Tạo payment record
            var payment = new Payment
            {
                UserId = userId,
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

            // EVENT theo phương thức thanh toán
            switch (model.PaymentMethod)
            {
                case "COD":
                    // Lưu DB + coi như thanh toán xong
                    payment.Status = "Completed";
                    payment.PaidAt = DateTime.Now;

                    course.EnrollmentCount++;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đặt hàng COD thành công, khóa học đã được mở.";
                    return RedirectToAction("PaymentSuccess", new { paymentId = payment.PaymentId });


                case "MoMo":
                    {
                        var fakeUrl = Url.Action(
                            "FakeMoMoPayment",
                            "Course",
                            new { paymentId = payment.PaymentId },
                            Request.Scheme
                        );

                        var returnUrl = Url.Action("MoMoReturn", "Course", null, Request.Scheme);
                        var ipnUrl = Url.Action("MoMoNotify", "Course", null, Request.Scheme);

                        var redirectUrl = await _momoService.CreatePaymentUrl(
                            payment,
                            returnUrl,
                            ipnUrl,
                            fakeUrl
                        );

                        // Chuyển sang trang thanh toán MoMo (fake)
                        return Redirect(redirectUrl);
                    }

                case "PayPal":
                    {
                        // Gọi service PayPal tạo order → redirect sang PayPal
                        var approvalUrl = await _paypalService.CreatePayPalOrder(
                            payment,
                            Url,
                            Request.Scheme
                        );

                        return Redirect(approvalUrl);
                    }

                default:
                    TempData["ErrorMessage"] = "Phương thức thanh toán không hợp lệ.";
                    return RedirectToAction("Checkout", new { id = course.CourseId });
            }
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


        public async Task<IActionResult> Learning(int id, int? lessonId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            // Lấy course + các lesson đã publish
            var course = await _context.Courses
                .Include(c => c.Lessons.Where(l => l.IsPublished == true))
                .FirstOrDefaultAsync(c => c.CourseId == id && c.IsPublished == true);

            if (course == null)
                return NotFound();

            // Đảm bảo đã có Enrollment (có thể được tạo từ PaymentSuccess, nhưng nếu chưa thì tạo)
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

            int currentLessonId = 0;
            if (lessons.Any())
                currentLessonId = lessonId ?? lessons.First().LessonId;

            // Lấy tài liệu của lesson hiện tại
            var currentMaterials = await _context.CourseMaterials
                .Where(m => m.CourseId == id && m.LessonId == currentLessonId)
                .OrderBy(m => m.MaterialName)
                .ToListAsync();

            // Tạo/ cập nhật LessonProgress cho bài hiện tại
            if (currentLessonId != 0)
            {
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
            }

            // Map các bài đã có progress
            var lessonIds = lessons.Select(l => l.LessonId).ToList();
            var lessonProgresses = await _context.LessonProgresses
                .Where(p => p.EnrollmentId == enrollment.EnrollmentId &&
                            lessonIds.Contains(p.LessonId))
                .ToListAsync();

            var hasProgressDict = lessonProgresses
                .GroupBy(p => p.LessonId)
                .ToDictionary(g => g.Key, g => g.Any());

            var vm = new LearningViewModel
            {
                Course = course,
                Lessons = lessons,
                CurrentLessonId = currentLessonId == 0 ? (int?)null : currentLessonId,
                CurrentMaterials = currentMaterials,
                HasProgressForLesson = hasProgressDict
            };

            return View(vm); // Views/Course/Learning.cshtml
        }


    }
}