using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using DoAnChuyenNganh.ViewModels.Course;
using System.Text.Json;

namespace DoAnChuyenNganh.Controllers
{
    public class LessonController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LessonController(DoAnChuyenNganhContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==== Helper permission ====
        private bool HasInstructorOrAdminPermission()
        {
            var roleClaim = User.FindFirst("RoleName")?.Value;
            if (!string.IsNullOrEmpty(roleClaim))
            {
                if (roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    roleClaim.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var roleName = HttpContext.Session.GetString("RoleName");
            if (!string.IsNullOrEmpty(roleName))
            {
                if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    roleName.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

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

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue)
            {
                return sessionUserId.Value;
            }

            return 1;
        }

        private IQueryable<Course> GetUserCourses()
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            if (isAdmin)
            {
                return _context.Courses.OrderBy(c => c.Title);
            }
            else
            {
                return _context.Courses
                    .Where(c => c.InstructorId == currentUserId)
                    .OrderBy(c => c.Title);
            }
        }

        private async Task<string> UploadFileAsync(IFormFile file, string folderName = "lessons")
        {
            if (file == null || file.Length == 0)
                return null;

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return $"/uploads/{folderName}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                return null;
            }
        }

        private string GetMaterialType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(extension))
                return "image";

            if (new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm" }.Contains(extension))
                return "video";

            if (new[] { ".mp3", ".wav", ".ogg", ".m4a" }.Contains(extension))
                return "audio";

            if (new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" }.Contains(extension))
                return "document";

            return "document";
        }

        private async Task SaveToCourseMaterialsAsync(
            int lessonId,
            string fileUrl,
            string fileName,
            long fileSize,
            int createdBy)
        {
            try
            {
                var materialType = GetMaterialType(fileName);

                var lesson = await _context.Lessons
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId);

                if (lesson == null)
                {
                    Console.WriteLine($"Lesson {lessonId} not found!");
                    return;
                }

                var courseMaterial = new CourseMaterial
                {
                    LessonId = lessonId,
                    CourseId = lesson.CourseId,
                    MaterialName = fileName,
                    MaterialType = materialType,
                    FileUrl = fileUrl,
                    FileSize = fileSize,
                    DownloadCount = 0,
                    IsPublic = false,
                    CreatedAt = DateTime.Now,
                    CreatedBy = createdBy
                };

                _context.CourseMaterials.Add(courseMaterial);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to CourseMaterials: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            }
        }

        // GET: Lesson
        public async Task<IActionResult> Index()
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            var currentUserId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            var lessons = _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.CreatedByNavigation)
                .Include(l => l.UpdatedByNavigation)
                .Where(l => isAdmin || l.Course.InstructorId == currentUserId)
                .OrderBy(l => l.CourseId)
                .ThenBy(l => l.LessonOrder);

            return View(await lessons.ToListAsync());
        }

        // GET: Lesson/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            if (id == null)
                return NotFound();

            if (!await CanAccessLessonAsync(id.Value))
                return ForbidToHome();

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
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            var userCourses = GetUserCourses();
            ViewData["CourseId"] = new SelectList(userCourses, "CourseId", "Title");

            var model = new LessonQuizUpsertViewModel();
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    LessonQuizUpsertViewModel model,
    IFormFile ThumbnailFile)
        {
            Console.WriteLine("========== CREATE POST STARTED ==========");
            Console.WriteLine($"LessonType: {model.LessonType}");
            Console.WriteLine($"Title: {model.Title}");
            Console.WriteLine($"CourseId: {model.CourseId}");
            Console.WriteLine($"Questions Count: {model.Questions?.Count ?? 0}");

            if (!HasInstructorOrAdminPermission())
            {
                Console.WriteLine("Permission denied!");
                return ForbidToHome();
            }

            // Remove từ ModelState validation
            ModelState.Remove("ThumbnailFile");
            ModelState.Remove("Questions");

            // Validate quiz if type is quiz
            if (model.LessonType == "quiz")
            {
                Console.WriteLine("Validating quiz...");
                ValidateQuizModel(model);
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("========== MODEL STATE INVALID ==========");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        Console.WriteLine($"Key: {key}");
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"  - Error: {error.ErrorMessage}");
                        }
                    }
                }

                var userCourses = GetUserCourses();
                ViewData["CourseId"] = new SelectList(userCourses, "CourseId", "Title", model.CourseId);
                return View(model);
            }

            try
            {
                int currentUserId = GetCurrentUserId();
                Console.WriteLine($"Current User ID: {currentUserId}");

                // ✅ FIX: Nếu LessonOrder = 0 hoặc null, tự động lấy order tiếp theo
                if (model.LessonOrder <= 0)
                {
                    model.LessonOrder = await GetNextLessonOrderAsync(model.CourseId);
                    Console.WriteLine($"Auto-assigned LessonOrder: {model.LessonOrder}");
                }

                // ✅ FIX: Kiểm tra duplicate LessonOrder trước khi insert
                var existingLesson = await _context.Lessons
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.CourseId == model.CourseId && l.LessonOrder == model.LessonOrder);

                if (existingLesson != null)
                {
                    ModelState.AddModelError("LessonOrder",
                        $"Lesson order {model.LessonOrder} đã tồn tại trong khóa học này. Vui lòng chọn số khác.");

                    var userCourses = GetUserCourses();
                    ViewData["CourseId"] = new SelectList(userCourses, "CourseId", "Title", model.CourseId);
                    return View(model);
                }

                // Create Lesson entity
                var lesson = new Lesson
                {
                    CourseId = model.CourseId,
                    Title = model.Title,
                    Content = model.Content,
                    LessonType = model.LessonType,
                    LessonOrder = (short)model.LessonOrder,
                    VideoDuration = model.VideoDuration,
                    IsPreviewable = model.IsPreviewable,
                    IsPublished = model.IsPublished,
                    CreatedBy = currentUserId,
                    UpdatedBy = currentUserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                Console.WriteLine("Lesson entity created");

                // Handle file upload
                if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                {
                    Console.WriteLine($"Uploading file: {ThumbnailFile.FileName}");
                    var uploadedFileUrl = await UploadFileAsync(ThumbnailFile);
                    if (!string.IsNullOrEmpty(uploadedFileUrl))
                    {
                        lesson.VideoUrl = uploadedFileUrl;
                        Console.WriteLine($"File uploaded: {uploadedFileUrl}");
                    }
                }
                else if (!string.IsNullOrEmpty(model.VideoUrl))
                {
                    lesson.VideoUrl = model.VideoUrl;
                    Console.WriteLine($"Video URL set: {model.VideoUrl}");
                }

                Console.WriteLine("Adding lesson to context...");
                _context.Lessons.Add(lesson);

                Console.WriteLine("Saving lesson to database...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"Lesson saved with ID: {lesson.LessonId}");

                // Save file to CourseMaterials if uploaded
                if (ThumbnailFile != null && ThumbnailFile.Length > 0 && !string.IsNullOrEmpty(lesson.VideoUrl))
                {
                    Console.WriteLine("Saving to CourseMaterials...");
                    await SaveToCourseMaterialsAsync(
                        lesson.LessonId,
                        lesson.VideoUrl,
                        ThumbnailFile.FileName,
                        ThumbnailFile.Length,
                        currentUserId);
                }

                // Create quiz if lesson type is quiz
                if (model.LessonType == "quiz" && model.Questions != null && model.Questions.Any())
                {
                    Console.WriteLine($"Creating quiz with {model.Questions.Count} questions...");
                    await CreateQuizFromViewModelAsync(lesson.LessonId, model, currentUserId);
                    Console.WriteLine("Quiz created successfully");
                }

                Console.WriteLine("========== CREATE SUCCESS ==========");
                TempData["SuccessMessage"] = "Lesson created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== CREATE ERROR ==========");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                TempData["ErrorMessage"] = $"Error creating lesson: {ex.Message}";

                var userCourses = GetUserCourses();
                ViewData["CourseId"] = new SelectList(userCourses, "CourseId", "Title", model.CourseId);
                return View(model);
            }
        }

        // GET: Lesson/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            if (id == null)
                return NotFound();

            if (!await CanAccessLessonAsync(id.Value))
                return ForbidToHome();

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
                return NotFound();

            var model = new LessonQuizUpsertViewModel
            {
                LessonId = lesson.LessonId,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                Content = lesson.Content,
                LessonType = lesson.LessonType,
                LessonOrder = lesson.LessonOrder,
                VideoUrl = lesson.VideoUrl,
                VideoDuration = lesson.VideoDuration,
                // ✅ FIX: Chuyển nullable bool sang non-nullable bool
                IsPreviewable = lesson.IsPreviewable ?? false,  // Nếu null thì mặc định false
                IsPublished = lesson.IsPublished ?? false       // Nếu null thì mặc định false
            };

            // Load quiz data if exists
            if (lesson.LessonType == "quiz")
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.QuizQuestions.OrderBy(qq => qq.QuestionOrder))
                        .ThenInclude(qq => qq.QuizAnswers.OrderBy(qa => qa.AnswerOrder))
                    .FirstOrDefaultAsync(q => q.LessonId == lesson.LessonId);

                if (quiz != null)
                {
                    model.QuizTitle = quiz.Title;
                    model.QuizDescription = quiz.Description;
                    model.IsRandomOrder = quiz.IsRandomOrder;

                    model.Questions = quiz.QuizQuestions.Select(q => new QuizQuestionVM
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Points = q.Points,
                        Explanation = q.Explanation,
                        Answers = q.QuizAnswers.Select(a => new QuizAnswerVM
                        {
                            AnswerId = a.AnswerId,
                            AnswerText = a.AnswerText,
                            IsCorrect = a.IsCorrect,
                            AnswerOrder = a.AnswerOrder
                        }).ToList()
                    }).ToList();

                    // Pass quiz data as JSON for JavaScript
                    ViewData["ExistingQuizData"] = JsonSerializer.Serialize(new
                    {
                        QuizId = quiz.QuizId,
                        Title = quiz.Title,
                        Description = quiz.Description,
                        IsRandomOrder = quiz.IsRandomOrder,
                        Questions = model.Questions
                    });
                }
            }

            var userCourses = GetUserCourses();
            ViewData["CourseId"] = new SelectList(userCourses, "CourseId", "Title", lesson.CourseId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LessonQuizUpsertViewModel model, IFormFile ThumbnailFile)
        {
            Console.WriteLine("========== EDIT POST STARTED ==========");
            Console.WriteLine($"ID from route: {id}");
            Console.WriteLine($"Model.LessonId: {model.LessonId}");
            Console.WriteLine($"Model.Title: {model.Title}");
            Console.WriteLine($"Model.LessonType: {model.LessonType}");
            Console.WriteLine($"Questions Count: {model.Questions?.Count ?? 0}");

            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            if (id != model.LessonId)
                return NotFound();

            // ✅ GIỐNG CREATE: file là optional ở Edit (và cả Create bạn đã remove rồi)
            ModelState.Remove("ThumbnailFile");

            // ❌ ĐỪNG remove Questions nếu bạn cần validate quiz
            // ModelState.Remove("Questions");

            if (model.LessonType == "quiz")
            {
                ValidateQuizModel(model);
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("========== EDIT MODEL STATE INVALID ==========");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        Console.WriteLine($"Key: {key}");
                        foreach (var error in errors)
                            Console.WriteLine($"  - Error: {error.ErrorMessage}");
                    }
                }

                // ✅ trả lại quiz JSON để UI dựng lại (tránh “mất quiz” sau reload)
                if (model.LessonType == "quiz")
                {
                    ViewData["ExistingQuizData"] = JsonSerializer.Serialize(new
                    {
                        Title = model.QuizTitle,
                        Description = model.QuizDescription,
                        IsRandomOrder = model.IsRandomOrder,
                        Questions = model.Questions
                    });
                }

                var userCourses = GetUserCourses();
                ViewData["CourseId"] = new SelectList(userCourses, "CourseId", "Title", model.CourseId);
                return View(model);
            }

            try
            {
                var existing = await _context.Lessons.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LessonId == id);

                if (existing == null)
                    return NotFound();

                int currentUserId = GetCurrentUserId();

                var lesson = new Lesson
                {
                    LessonId = model.LessonId,
                    CourseId = model.CourseId,
                    Title = model.Title,
                    Content = model.Content,
                    LessonType = model.LessonType,
                    LessonOrder = (short)model.LessonOrder,
                    VideoDuration = model.VideoDuration,
                    IsPreviewable = model.IsPreviewable,
                    IsPublished = model.IsPublished,

                    CreatedAt = existing.CreatedAt,
                    CreatedBy = existing.CreatedBy,
                    UpdatedBy = currentUserId,
                    UpdatedAt = DateTime.Now,

                    // ✅ mặc định giữ url cũ
                    VideoUrl = existing.VideoUrl
                };

                // ✅ Nếu user upload file mới -> replace
                if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                {
                    var uploadedFileUrl = await UploadFileAsync(ThumbnailFile);
                    if (!string.IsNullOrEmpty(uploadedFileUrl))
                    {
                        lesson.VideoUrl = uploadedFileUrl;

                        await SaveToCourseMaterialsAsync(
                            lesson.LessonId,
                            uploadedFileUrl,
                            ThumbnailFile.FileName,
                            ThumbnailFile.Length,
                            currentUserId
                        );
                    }
                }
                else if (!string.IsNullOrEmpty(model.VideoUrl))
                {
                    // ✅ nếu user sửa link bằng text box
                    lesson.VideoUrl = model.VideoUrl;
                }

                _context.Update(lesson);
                await _context.SaveChangesAsync();

                // Quiz update/delete
                if (model.LessonType == "quiz" && model.Questions != null && model.Questions.Any())
                {
                    await UpdateQuizFromViewModelAsync(lesson.LessonId, model, currentUserId);
                }
                else if (model.LessonType != "quiz")
                {
                    var existingQuiz = await _context.Quizzes
                        .FirstOrDefaultAsync(q => q.LessonId == lesson.LessonId);
                    if (existingQuiz != null)
                    {
                        _context.Quizzes.Remove(existingQuiz);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Lesson updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating lesson: {ex.Message}";
                Console.WriteLine($"Full error: {ex}");

                var userCourses = GetUserCourses();
                ViewData["CourseId"] = new SelectList(userCourses, "CourseId", "Title", model.CourseId);
                return View(model);
            }
        }

        private async Task<int> GetNextLessonOrderAsync(int courseId)
        {
            var maxOrder = await _context.Lessons
                .Where(l => l.CourseId == courseId)
                .MaxAsync(l => (int?)l.LessonOrder);

            return (maxOrder ?? 0) + 1;
        }
        private void ValidateQuizModel(LessonQuizUpsertViewModel model)
        {
            if (model.Questions == null || !model.Questions.Any())
            {
                ModelState.AddModelError("", "Phải có ít nhất 1 câu hỏi cho quiz.");
                return;
            }

            for (int i = 0; i < model.Questions.Count; i++)
            {
                var q = model.Questions[i];

                if (string.IsNullOrWhiteSpace(q.QuestionText))
                {
                    ModelState.AddModelError($"Questions[{i}].QuestionText", "Câu hỏi không được trống.");
                }

                if (q.QuestionType == "multiple_choice")
                {
                    if (q.Answers == null || q.Answers.Count < 2)
                    {
                        ModelState.AddModelError($"Questions[{i}].Answers", "Mỗi câu phải có ít nhất 2 đáp án.");
                    }
                    else if (q.Answers.Count > 5)
                    {
                        ModelState.AddModelError($"Questions[{i}].Answers", "Mỗi câu tối đa 5 đáp án.");
                    }

                    var correctCount = q.Answers?.Count(a => a.IsCorrect) ?? 0;

                    // ✅ FIX: CHỈ YÊU CẦU ÍT NHẤT 1 ĐÁP ÁN ĐÚNG
                    if (correctCount == 0)
                    {
                        ModelState.AddModelError($"Questions[{i}]", "Phải có ít nhất 1 đáp án đúng.");
                    }
                }
            }
        }

        // =====================================================
        // PHẦN 4: SỬA CREATE QUIZ FROM VIEWMODEL
        // =====================================================

        private async Task CreateQuizFromViewModelAsync(int lessonId, LessonQuizUpsertViewModel model, int currentUserId)
        {
            try
            {
                var lesson = await _context.Lessons.FindAsync(lessonId);
                if (lesson == null) return;

                // Create quiz
                var quiz = new Quiz
                {
                    LessonId = lessonId,
                    Title = !string.IsNullOrWhiteSpace(model.QuizTitle) ? model.QuizTitle : $"{lesson.Title} - Quiz",
                    Description = model.QuizDescription,
                    IsRandomOrder = model.IsRandomOrder,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = currentUserId,
                    UpdatedBy = currentUserId
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                // Create questions and answers
                int questionOrder = 1;
                foreach (var qVM in model.Questions)
                {
                    // ✅ FIX: Tính AllowMultipleCorrect dựa trên số đáp án đúng
                    int correctCount = qVM.Answers?.Count(a => a.IsCorrect) ?? 0;

                    var question = new QuizQuestion
                    {
                        QuizId = quiz.QuizId,
                        QuestionText = qVM.QuestionText,
                        QuestionType = qVM.QuestionType,
                        QuestionOrder = questionOrder++,
                        Points = qVM.Points,
                        Explanation = qVM.Explanation,
                        AllowMultipleCorrect = correctCount > 1, // ✅ TRUE nếu > 1 đáp án đúng
                        CreatedAt = DateTime.Now
                    };

                    _context.QuizQuestions.Add(question);
                    await _context.SaveChangesAsync();

                    // Add answers (only for multiple_choice)
                    if (question.QuestionType == "multiple_choice" && qVM.Answers != null)
                    {
                        byte answerOrder = 1;
                        foreach (var aVM in qVM.Answers.OrderBy(a => a.AnswerOrder))
                        {
                            var answer = new QuizAnswer
                            {
                                QuestionId = question.QuestionId,
                                AnswerText = aVM.AnswerText,
                                IsCorrect = aVM.IsCorrect,
                                AnswerOrder = answerOrder++
                            };

                            _context.QuizAnswers.Add(answer);
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating quiz from ViewModel: {ex.Message}");
                throw;
            }
        }

        // =====================================================
        // PHẦN 5: SỬA UPDATE QUIZ FROM VIEWMODEL
        // =====================================================

        private async Task UpdateQuizFromViewModelAsync(int lessonId, LessonQuizUpsertViewModel model, int currentUserId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.QuizQuestions)
                        .ThenInclude(qq => qq.QuizAnswers)
                    .FirstOrDefaultAsync(q => q.LessonId == lessonId);

                if (quiz == null)
                {
                    await CreateQuizFromViewModelAsync(lessonId, model, currentUserId);
                    return;
                }

                // Check if quiz has attempts
                var hasAttempts = await _context.UserQuizAttempts
                    .AnyAsync(a => a.QuizId == quiz.QuizId);

                if (hasAttempts)
                {
                    throw new InvalidOperationException("Quiz đã có học viên làm. Không thể chỉnh sửa nội dung quiz.");
                }

                // Update quiz metadata
                quiz.Title = !string.IsNullOrWhiteSpace(model.QuizTitle) ? model.QuizTitle : quiz.Title;
                quiz.Description = model.QuizDescription;
                quiz.IsRandomOrder = model.IsRandomOrder;
                quiz.UpdatedAt = DateTime.Now;
                quiz.UpdatedBy = currentUserId;

                // Track existing IDs
                var existingQuestionIds = quiz.QuizQuestions.Select(q => q.QuestionId).ToHashSet();
                var newQuestionIds = model.Questions
                    .Where(q => q.QuestionId.HasValue)
                    .Select(q => q.QuestionId.Value)
                    .ToHashSet();

                // Remove deleted questions
                var questionsToRemove = quiz.QuizQuestions
                    .Where(q => !newQuestionIds.Contains(q.QuestionId))
                    .ToList();
                _context.QuizQuestions.RemoveRange(questionsToRemove);

                // Update or create questions
                int questionOrder = 1;
                foreach (var qVM in model.Questions)
                {
                    // ✅ FIX: Tính AllowMultipleCorrect
                    int correctCount = qVM.Answers?.Count(a => a.IsCorrect) ?? 0;

                    QuizQuestion question;

                    if (qVM.QuestionId.HasValue && existingQuestionIds.Contains(qVM.QuestionId.Value))
                    {
                        // Update existing question
                        question = quiz.QuizQuestions.First(q => q.QuestionId == qVM.QuestionId.Value);
                        question.QuestionText = qVM.QuestionText;
                        question.QuestionType = qVM.QuestionType;
                        question.QuestionOrder = questionOrder;
                        question.Points = qVM.Points;
                        question.Explanation = qVM.Explanation;
                        question.AllowMultipleCorrect = correctCount > 1; // ✅ FIX
                    }
                    else
                    {
                        // Create new question
                        question = new QuizQuestion
                        {
                            QuizId = quiz.QuizId,
                            QuestionText = qVM.QuestionText,
                            QuestionType = qVM.QuestionType,
                            QuestionOrder = questionOrder,
                            Points = qVM.Points,
                            Explanation = qVM.Explanation,
                            AllowMultipleCorrect = correctCount > 1, // ✅ FIX
                            CreatedAt = DateTime.Now
                        };
                        _context.QuizQuestions.Add(question);
                        await _context.SaveChangesAsync();
                    }

                    questionOrder++;

                    // Handle answers (only for multiple_choice)
                    if (question.QuestionType == "multiple_choice" && qVM.Answers != null)
                    {
                        var existingAnswerIds = question.QuizAnswers.Select(a => a.AnswerId).ToHashSet();
                        var newAnswerIds = qVM.Answers
                            .Where(a => a.AnswerId.HasValue)
                            .Select(a => a.AnswerId.Value)
                            .ToHashSet();

                        // Remove deleted answers
                        var answersToRemove = question.QuizAnswers
                            .Where(a => !newAnswerIds.Contains(a.AnswerId))
                            .ToList();
                        _context.QuizAnswers.RemoveRange(answersToRemove);

                        // Update or create answers
                        byte answerOrder = 1;
                        foreach (var aVM in qVM.Answers.OrderBy(a => a.AnswerOrder))
                        {
                            if (aVM.AnswerId.HasValue && existingAnswerIds.Contains(aVM.AnswerId.Value))
                            {
                                // Update existing answer
                                var answer = question.QuizAnswers.First(a => a.AnswerId == aVM.AnswerId.Value);
                                answer.AnswerText = aVM.AnswerText;
                                answer.IsCorrect = aVM.IsCorrect;
                                answer.AnswerOrder = answerOrder;
                            }
                            else
                            {
                                // Create new answer
                                var answer = new QuizAnswer
                                {
                                    QuestionId = question.QuestionId,
                                    AnswerText = aVM.AnswerText,
                                    IsCorrect = aVM.IsCorrect,
                                    AnswerOrder = answerOrder
                                };
                                _context.QuizAnswers.Add(answer);
                            }
                            answerOrder++;
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating quiz: {ex.Message}");
                throw;
            }
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            if (id == null)
                return NotFound();

            if (!await CanAccessLessonAsync(id.Value))
                return ForbidToHome();

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.CreatedByNavigation)
                .Include(l => l.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.LessonId == id);

            if (lesson == null)
                return NotFound();

            return View(lesson);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

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

        // ================== QUIZ HELPER ==================
        private class ParsedAnswer
        {
            public char Label { get; set; }
            public string Text { get; set; }
        }

        private class ParsedQuestion
        {
            public int Number { get; set; }
            public string QuestionText { get; set; }
            public List<ParsedAnswer> Answers { get; set; } = new();
        }

        private class QuizTextData
        {
            public string RawText { get; set; }
            public string AnswerKey { get; set; }
        }

        // ✅ NEW: Convert quiz từ DB về text format để edit
        private QuizTextData ConvertQuizToText(Quiz quiz)
        {
            var sb = new StringBuilder();
            var answerKeys = new StringBuilder();

            var questions = quiz.QuizQuestions
                .OrderBy(q => q.QuestionOrder)
                .ToList();

            foreach (var question in questions)
            {
                sb.AppendLine($"Câu {question.QuestionOrder}: {question.QuestionText}");
                sb.AppendLine();

                var answers = question.QuizAnswers
                    .OrderBy(a => a.AnswerOrder)
                    .ToList();

                foreach (var answer in answers)
                {
                    char label = (char)('a' + answer.AnswerOrder - 1);
                    sb.AppendLine($"{label}. {answer.AnswerText}");

                    if (answer.IsCorrect)
                    {
                        if (answerKeys.Length > 0)
                            answerKeys.Append(" ");
                        answerKeys.Append($"{question.QuestionOrder}.{label}");
                    }
                }

                sb.AppendLine();
            }

            return new QuizTextData
            {
                RawText = sb.ToString().Trim(),
                AnswerKey = answerKeys.ToString()
            };
        }

        private Dictionary<int, char> ParseAnswerKey(string? key)
        {
            var dict = new Dictionary<int, char>();
            if (string.IsNullOrWhiteSpace(key))
                return dict;

            var tokens = Regex.Split(key, @"[;\r\n\s,]+");
            foreach (var raw in tokens)
            {
                var t = raw.Trim();
                if (string.IsNullOrEmpty(t)) continue;

                var m = Regex.Match(t, @"^(\d+)\s*[\.\:\-]?\s*([a-dA-D])$");
                if (!m.Success) continue;

                int qNum = int.Parse(m.Groups[1].Value);
                char letter = char.ToLower(m.Groups[2].Value[0]);
                dict[qNum] = letter;
            }
            return dict;
        }

        private List<ParsedQuestion> ParseQuizRaw(string raw)
        {
            var result = new List<ParsedQuestion>();
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            var blocks = Regex.Split(raw.Trim(), @"(?=Câu\s+\d+\s*:)", RegexOptions.IgnoreCase);

            foreach (var block in blocks)
            {
                var b = block.Trim();
                if (string.IsNullOrEmpty(b)) continue;

                var lines = b
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .ToList();

                if (lines.Count == 0) continue;

                var first = lines[0];
                var m = Regex.Match(first, @"Câu\s+(\d+)\s*:(.+)", RegexOptions.IgnoreCase);
                int number = 0;
                string qText;

                if (m.Success)
                {
                    number = int.Parse(m.Groups[1].Value);
                    qText = m.Groups[2].Value.Trim();
                }
                else
                {
                    number = result.Count + 1;
                    qText = first;
                }

                var parsed = new ParsedQuestion
                {
                    Number = number,
                    QuestionText = qText
                };

                for (int i = 1; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var am = Regex.Match(line, @"^([a-dA-D])\.\s*(.+)$");
                    if (!am.Success) continue;

                    char label = char.ToLower(am.Groups[1].Value[0]);
                    string text = am.Groups[2].Value.Trim();

                    parsed.Answers.Add(new ParsedAnswer
                    {
                        Label = label,
                        Text = text
                    });
                }

                if (parsed.Answers.Count > 0)
                {
                    result.Add(parsed);
                }
            }
            return result;
        }

        private async Task CreateOrReplaceQuizAsync(
    Lesson lesson,
    int currentUserId,
    string? quizRawText,
    string? quizAnswerKey)
        {
            if (lesson == null) return;
            if (string.IsNullOrWhiteSpace(quizRawText)) return;

            // ✅ FIX 1: Use AsNoTracking để tránh tracking conflict
            var existingQuiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.QuizQuestions)
                    .ThenInclude(qq => qq.QuizAnswers)
                .FirstOrDefaultAsync(q => q.LessonId == lesson.LessonId);
            var existingQuizId = await _context.Quizzes
    .Where(q => q.LessonId == lesson.LessonId)
    .Select(q => (int?)q.QuizId)
    .FirstOrDefaultAsync();

            if (existingQuizId.HasValue)
            {
                var hasAttempts = await _context.UserQuizAttempts
                    .AnyAsync(a => a.QuizId == existingQuizId.Value);

                if (hasAttempts)
                {
                    // Không được "đập đi xây lại" nữa
                    throw new InvalidOperationException("Quiz đã có học viên làm. Không thể chỉnh sửa nội dung quiz (để giữ lịch sử).");
                }
            }

            if (existingQuiz != null)
            {
                // ✅ FIX 2: Xóa trực tiếp bằng raw SQL hoặc load lại để track
                var quizToDelete = await _context.Quizzes
                    .Include(q => q.QuizQuestions)
                        .ThenInclude(qq => qq.QuizAnswers)
                    .FirstOrDefaultAsync(q => q.QuizId == existingQuiz.QuizId);

                if (quizToDelete != null)
                {
                    // Xóa answers trước
                    _context.QuizAnswers.RemoveRange(
                        quizToDelete.QuizQuestions.SelectMany(x => x.QuizAnswers));

                    // Xóa questions
                    _context.QuizQuestions.RemoveRange(quizToDelete.QuizQuestions);

                    // Xóa quiz
                    _context.Quizzes.Remove(quizToDelete);

                    // ✅ FIX 3: SaveChanges NGAY để clear tracking
                    await _context.SaveChangesAsync();

                    // ✅ FIX 4: Clear ChangeTracker để đảm bảo không còn tracked entities
                    _context.ChangeTracker.Clear();
                }
            }

            // Parse quiz data
            var parsedQuestions = ParseQuizRaw(quizRawText);
            var answerKey = ParseAnswerKey(quizAnswerKey);

            if (!parsedQuestions.Any())
                return;

            // ✅ FIX 5: Tạo quiz mới sau khi đã clear hết entities cũ
            var quiz = new Quiz
            {
                LessonId = lesson.LessonId,
                Title = $"{lesson.Title} - Quiz",
                Description = null,
                IsRandomOrder = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = currentUserId,
                UpdatedBy = currentUserId
            };

            int order = 1;
            foreach (var pq in parsedQuestions.OrderBy(x => x.Number))
            {
                var qq = new QuizQuestion
                {
                    Quiz = quiz,
                    QuestionText = pq.QuestionText,
                    QuestionType = "multiple_choice",
                    QuestionOrder = order,
                    Points = 10,
                    Explanation = null,
                    CreatedAt = DateTime.Now
                };

                answerKey.TryGetValue(pq.Number, out char correctLetter);
                if (correctLetter == '\0') correctLetter = '\0';

                byte answerOrder = 1;
                foreach (var ans in pq.Answers)
                {
                    var qa = new QuizAnswer
                    {
                        Question = qq,
                        AnswerText = ans.Text,
                        AnswerOrder = answerOrder,
                        IsCorrect = (ans.Label == correctLetter)
                    };
                    qq.QuizAnswers.Add(qa);
                    answerOrder++;
                }

                quiz.QuizQuestions.Add(qq);
                order++;
            }

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
        }

        private bool IsAdmin()
        {
            var roleClaim = User.FindFirst("RoleName")?.Value;
            if (!string.IsNullOrEmpty(roleClaim) &&
                roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return true;

            var roleName = HttpContext.Session.GetString("RoleName");
            if (!string.IsNullOrEmpty(roleName) &&
                roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return true;

            var permissionLevel = HttpContext.Session.GetInt32("PermissionLevel");
            return permissionLevel.HasValue && permissionLevel.Value >= 4;
        }

        private async Task<bool> CanAccessLessonAsync(int lessonId)
        {
            if (IsAdmin()) return true;

            var currentUserId = GetCurrentUserId();

            return await _context.Lessons
                .Include(l => l.Course)
                .AnyAsync(l => l.LessonId == lessonId && l.Course.InstructorId == currentUserId);
        }
        private void ValidateQuiz(LessonQuizUpsertViewModel vm)
        {
            if (vm.Questions == null || vm.Questions.Count == 0)
            {
                ModelState.AddModelError("", "Phải có ít nhất 1 câu hỏi.");
                return;
            }

            for (int i = 0; i < vm.Questions.Count; i++)
            {
                var q = vm.Questions[i];

                if (q.Answers == null || q.Answers.Count < 2)
                    ModelState.AddModelError($"Questions[{i}].Answers", "Mỗi câu phải có ít nhất 2 đáp án.");

                if (q.Answers != null && q.Answers.Count > 5)
                    ModelState.AddModelError($"Questions[{i}].Answers", "Mỗi câu tối đa 5 đáp án.");

                var correctCount = q.Answers?.Count(a => a.IsCorrect) ?? 0;

                // Theo yêu cầu của bạn: ít nhất 2 đáp án đúng
                if (correctCount < 2)
                    ModelState.AddModelError($"Questions[{i}]", "Mỗi câu phải có ít nhất 2 đáp án đúng.");
            }
        }
        private async Task UpdateQuizFromJsonAsync(int lessonId, string quizJson, int currentUserId)
        {
            try
            {
                using var doc = JsonDocument.Parse(quizJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Questions", out var questionsElement))
                    return;

                // Get or create quiz
                var quiz = await _context.Quizzes
                    .Include(q => q.QuizQuestions)
                        .ThenInclude(qq => qq.QuizAnswers)
                    .FirstOrDefaultAsync(q => q.LessonId == lessonId);

                if (quiz == null)
                {
                    // Create new quiz if doesn't exist
                    await CreateQuizFromJsonAsync(lessonId, quizJson, currentUserId);
                    return;
                }

                // Update quiz metadata
                if (root.TryGetProperty("Title", out var titleProp))
                    quiz.Title = titleProp.GetString() ?? quiz.Title;
                if (root.TryGetProperty("Description", out var descProp))
                    quiz.Description = descProp.GetString();
                if (root.TryGetProperty("IsRandomOrder", out var randomProp))
                    quiz.IsRandomOrder = randomProp.GetBoolean();

                quiz.UpdatedAt = DateTime.Now;
                quiz.UpdatedBy = currentUserId;

                // Track existing IDs
                var existingQuestionIds = quiz.QuizQuestions.Select(q => q.QuestionId).ToHashSet();
                var newQuestionIds = new HashSet<int>();

                // Parse new question IDs
                foreach (var qElement in questionsElement.EnumerateArray())
                {
                    if (qElement.TryGetProperty("QuestionId", out var qIdProp) &&
                        qIdProp.TryGetInt32(out int qId))
                    {
                        newQuestionIds.Add(qId);
                    }
                }

                // Remove deleted questions
                var questionsToRemove = quiz.QuizQuestions
                    .Where(q => !newQuestionIds.Contains(q.QuestionId))
                    .ToList();
                _context.QuizQuestions.RemoveRange(questionsToRemove);

                // Update or create questions
                foreach (var qElement in questionsElement.EnumerateArray())
                {
                    QuizQuestion question;
                    int? questionId = qElement.TryGetProperty("QuestionId", out var qIdProp) &&
                                     qIdProp.TryGetInt32(out int qId) ? qId : null;

                    if (questionId.HasValue && existingQuestionIds.Contains(questionId.Value))
                    {
                        // Update existing question
                        question = quiz.QuizQuestions.First(q => q.QuestionId == questionId.Value);
                        question.QuestionText = qElement.GetProperty("QuestionText").GetString();
                        question.QuestionType = qElement.GetProperty("QuestionType").GetString();
                        question.QuestionOrder = qElement.GetProperty("QuestionOrder").GetInt32();
                        question.Points = qElement.TryGetProperty("Points", out var pointsProp)
                            ? pointsProp.GetInt32()
                            : 10;
                        question.Explanation = qElement.TryGetProperty("Explanation", out var explProp)
                            ? explProp.GetString()
                            : null;
                    }
                    else
                    {
                        // Create new question
                        question = new QuizQuestion
                        {
                            QuizId = quiz.QuizId,
                            QuestionText = qElement.GetProperty("QuestionText").GetString(),
                            QuestionType = qElement.GetProperty("QuestionType").GetString() ?? "multiple_choice",
                            QuestionOrder = qElement.GetProperty("QuestionOrder").GetInt32(),
                            Points = qElement.TryGetProperty("Points", out var pointsProp)
                                ? pointsProp.GetInt32()
                                : 10,
                            Explanation = qElement.TryGetProperty("Explanation", out var explProp)
                                ? explProp.GetString()
                                : null,
                            CreatedAt = DateTime.Now
                        };
                        _context.QuizQuestions.Add(question);
                        await _context.SaveChangesAsync(); // Save to get QuestionId
                    }

                    // Handle answers (only for multiple_choice)
                    if (question.QuestionType == "multiple_choice" &&
                        qElement.TryGetProperty("Answers", out var answersElement))
                    {
                        var existingAnswerIds = question.QuizAnswers.Select(a => a.AnswerId).ToHashSet();
                        var newAnswerIds = new HashSet<int>();

                        // Parse new answer IDs
                        foreach (var aElement in answersElement.EnumerateArray())
                        {
                            if (aElement.TryGetProperty("AnswerId", out var aIdProp) &&
                                aIdProp.TryGetInt32(out int aId))
                            {
                                newAnswerIds.Add(aId);
                            }
                        }

                        // Remove deleted answers
                        var answersToRemove = question.QuizAnswers
                            .Where(a => !newAnswerIds.Contains(a.AnswerId))
                            .ToList();
                        _context.QuizAnswers.RemoveRange(answersToRemove);

                        // Update or create answers
                        foreach (var aElement in answersElement.EnumerateArray())
                        {
                            int? answerId = aElement.TryGetProperty("AnswerId", out var aIdProp) &&
                                           aIdProp.TryGetInt32(out int aId) ? aId : null;

                            if (answerId.HasValue && existingAnswerIds.Contains(answerId.Value))
                            {
                                // Update existing answer
                                var answer = question.QuizAnswers.First(a => a.AnswerId == answerId.Value);
                                answer.AnswerText = aElement.GetProperty("AnswerText").GetString();
                                answer.IsCorrect = aElement.GetProperty("IsCorrect").GetBoolean();
                                answer.AnswerOrder = (byte)aElement.GetProperty("AnswerOrder").GetInt32();
                            }
                            else
                            {
                                // Create new answer
                                var answer = new QuizAnswer
                                {
                                    QuestionId = question.QuestionId,
                                    AnswerText = aElement.GetProperty("AnswerText").GetString(),
                                    IsCorrect = aElement.GetProperty("IsCorrect").GetBoolean(),
                                    AnswerOrder = (byte)aElement.GetProperty("AnswerOrder").GetInt32()
                                };
                                _context.QuizAnswers.Add(answer);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating quiz: {ex.Message}");
                throw;
            }
        }
        // ✅ Create quiz from JSON (using JsonElement)
        private async Task CreateQuizFromJsonAsync(int lessonId, string quizJson, int currentUserId)
        {
            try
            {
                using var doc = JsonDocument.Parse(quizJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Questions", out var questionsElement))
                    return;

                var questions = questionsElement.EnumerateArray().ToList();
                if (!questions.Any())
                    return;

                // Get lesson info
                var lesson = await _context.Lessons.FindAsync(lessonId);
                if (lesson == null) return;

                // Create quiz
                var quiz = new Quiz
                {
                    LessonId = lessonId,
                    Title = root.TryGetProperty("Title", out var titleProp)
                        ? titleProp.GetString() ?? $"{lesson.Title} - Quiz"
                        : $"{lesson.Title} - Quiz",
                    Description = root.TryGetProperty("Description", out var descProp)
                        ? descProp.GetString()
                        : null,
                    IsRandomOrder = root.TryGetProperty("IsRandomOrder", out var randomProp)
                        && randomProp.GetBoolean(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = currentUserId,
                    UpdatedBy = currentUserId
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                // Create questions and answers
                foreach (var qElement in questions)
                {
                    var question = new QuizQuestion
                    {
                        QuizId = quiz.QuizId,
                        QuestionText = qElement.GetProperty("QuestionText").GetString(),
                        QuestionType = qElement.GetProperty("QuestionType").GetString() ?? "multiple_choice",
                        QuestionOrder = qElement.GetProperty("QuestionOrder").GetInt32(),
                        Points = qElement.TryGetProperty("Points", out var pointsProp)
                            ? pointsProp.GetInt32()
                            : 10,
                        Explanation = qElement.TryGetProperty("Explanation", out var explProp)
                            ? explProp.GetString()
                            : null,
                        CreatedAt = DateTime.Now
                    };

                    _context.QuizQuestions.Add(question);
                    await _context.SaveChangesAsync();

                    // Add answers (only for multiple_choice)
                    if (question.QuestionType == "multiple_choice" &&
                        qElement.TryGetProperty("Answers", out var answersElement))
                    {
                        foreach (var aElement in answersElement.EnumerateArray())
                        {
                            var answer = new QuizAnswer
                            {
                                QuestionId = question.QuestionId,
                                AnswerText = aElement.GetProperty("AnswerText").GetString(),
                                IsCorrect = aElement.GetProperty("IsCorrect").GetBoolean(),
                                AnswerOrder = (byte)aElement.GetProperty("AnswerOrder").GetInt32()
                            };

                            _context.QuizAnswers.Add(answer);
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating quiz from JSON: {ex.Message}");
                throw;
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(
    int courseId,
    string title,
    string lessonType,
    string? content,
    string? videoUrl,
    bool isPublished,
    bool isPreviewable)
        {
            if (!HasInstructorOrAdminPermission())
                return Json(new { success = false, message = "Permission denied." });

            if (courseId <= 0)
                return Json(new { success = false, message = "CourseId invalid." });

            if (string.IsNullOrWhiteSpace(title))
                return Json(new { success = false, message = "Title is required." });

            lessonType = (lessonType ?? "text").Trim().ToLower();
            var allowedTypes = new[] { "text", "video", "quiz" };
            if (!allowedTypes.Contains(lessonType))
                return Json(new { success = false, message = "LessonType invalid." });

            var currentUserId = GetCurrentUserId();
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseId == courseId);
            if (course == null)
                return Json(new { success = false, message = "Course not found." });

            if (!IsAdmin() && course.InstructorId != currentUserId)
                return Json(new { success = false, message = "You can only add lessons to your own course." });

            try
            {
                // ✅ FIX: Luôn lấy order tiếp theo để tránh conflict
                var nextOrder = await GetNextLessonOrderAsync(courseId);

                var lesson = new Lesson
                {
                    CourseId = courseId,
                    Title = title.Trim(),
                    Content = content,
                    LessonType = lessonType,
                    LessonOrder = (short)nextOrder, // ✅ Dùng nextOrder
                    VideoUrl = lessonType == "video" ? videoUrl : null,
                    IsPublished = isPublished,
                    IsPreviewable = isPreviewable,
                    CreatedBy = currentUserId,
                    UpdatedBy = currentUserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    lessonId = lesson.LessonId,
                    lessonOrder = lesson.LessonOrder,
                    title = lesson.Title,
                    lessonType = lesson.LessonType
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateInline Error: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

    }
}