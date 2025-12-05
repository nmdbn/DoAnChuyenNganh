using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyenNganh.Controllers
{
    public class QuizController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;

        public QuizController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        // Helper lấy UserId hiện tại
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return 1; // TODO: Thay đổi theo logic authentication của bạn
        }

        // ========================================
        // QUẢN LÝ QUIZ (CHO GIÁO VIÊN)
        // ========================================

        // GET: Quiz/Create?lessonId=5
        public IActionResult Create(int lessonId)
        {
            var lesson = _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefault(l => l.LessonId == lessonId);

            if (lesson == null)
                return NotFound();

            var viewModel = new QuizViewModel
            {
                LessonId = lessonId,
                LessonTitle = lesson.Title,
                IsRandomOrder = false
            };

            return View(viewModel);
        }

        // POST: Quiz/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizViewModel model)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var quiz = new Quiz
                {
                    LessonId = model.LessonId,
                    Title = model.Title,
                    Description = model.Description,
                    IsRandomOrder = model.IsRandomOrder,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = currentUserId,
                    UpdatedBy = currentUserId
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                // Lưu câu hỏi
                foreach (var q in model.Questions)
                {
                    var question = new QuizQuestion
                    {
                        QuizId = quiz.QuizId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        QuestionOrder = q.QuestionOrder,
                        Points = q.Points,
                        Explanation = q.Explanation,
                        CreatedAt = DateTime.Now
                    };

                    _context.QuizQuestions.Add(question);
                    await _context.SaveChangesAsync();

                    // Lưu đáp án (nếu là multiple choice)
                    if (q.QuestionType == "multiple_choice")
                    {
                        foreach (var a in q.Answers.Take(4)) // Tối đa 4 đáp án
                        {
                            var answer = new QuizAnswer
                            {
                                QuestionId = question.QuestionId,
                                AnswerText = a.AnswerText,
                                IsCorrect = a.IsCorrect,
                                AnswerOrder = a.AnswerOrder
                            };
                            _context.QuizAnswers.Add(answer);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Quiz created successfully!";
                return RedirectToAction("Edit", "Lesson", new { id = model.LessonId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating quiz: {ex.Message}";
                return View(model);
            }
        }

        // GET: Quiz/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Course)
                .Include(q => q.QuizQuestions)
                    .ThenInclude(qq => qq.QuizAnswers.OrderBy(a => a.AnswerOrder))
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            var viewModel = new QuizViewModel
            {
                QuizId = quiz.QuizId,
                LessonId = quiz.LessonId,
                LessonTitle = quiz.Lesson.Title,
                Title = quiz.Title,
                Description = quiz.Description,
                IsRandomOrder = quiz.IsRandomOrder,
                Questions = quiz.QuizQuestions.OrderBy(q => q.QuestionOrder).Select(q => new QuizQuestionViewModel
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    QuestionOrder = q.QuestionOrder,
                    Points = q.Points,
                    Explanation = q.Explanation,
                    Answers = q.QuizAnswers.Select(a => new QuizAnswerViewModel
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect,
                        AnswerOrder = a.AnswerOrder
                    }).ToList()
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Quiz/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QuizViewModel model)
        {
            if (id != model.QuizId)
                return NotFound();

            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.QuizQuestions)
                        .ThenInclude(qq => qq.QuizAnswers)
                    .FirstOrDefaultAsync(q => q.QuizId == id);

                if (quiz == null)
                    return NotFound();

                // Update quiz info
                quiz.Title = model.Title;
                quiz.Description = model.Description;
                quiz.IsRandomOrder = model.IsRandomOrder;
                quiz.UpdatedAt = DateTime.Now;
                quiz.UpdatedBy = GetCurrentUserId();

                // Xóa câu hỏi cũ
                _context.QuizQuestions.RemoveRange(quiz.QuizQuestions);
                await _context.SaveChangesAsync();

                // Thêm câu hỏi mới
                foreach (var q in model.Questions)
                {
                    var question = new QuizQuestion
                    {
                        QuizId = quiz.QuizId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        QuestionOrder = q.QuestionOrder,
                        Points = q.Points,
                        Explanation = q.Explanation,
                        CreatedAt = DateTime.Now
                    };

                    _context.QuizQuestions.Add(question);
                    await _context.SaveChangesAsync();

                    if (q.QuestionType == "multiple_choice")
                    {
                        foreach (var a in q.Answers.Take(4))
                        {
                            var answer = new QuizAnswer
                            {
                                QuestionId = question.QuestionId,
                                AnswerText = a.AnswerText,
                                IsCorrect = a.IsCorrect,
                                AnswerOrder = a.AnswerOrder
                            };
                            _context.QuizAnswers.Add(answer);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Quiz updated successfully!";
                return RedirectToAction("Edit", "Lesson", new { id = model.LessonId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating quiz: {ex.Message}";
                return View(model);
            }
        }

        // ========================================
        // HỌC VIÊN LÀM QUIZ
        // ========================================

        // GET: Quiz/Take/5?enrollmentId=10
        public async Task<IActionResult> Take(int id, int enrollmentId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Course)
                .Include(q => q.QuizQuestions.OrderBy(qq => qq.QuestionOrder))
                    .ThenInclude(qq => qq.QuizAnswers.OrderBy(a => a.AnswerOrder))
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();

            // Tạo attempt mới
            var attempt = new UserQuizAttempt
            {
                UserId = currentUserId,
                QuizId = id,
                EnrollmentId = enrollmentId,
                StartedAt = DateTime.Now,
                Status = "in_progress"
            };

            _context.UserQuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            var questions = quiz.IsRandomOrder
                ? quiz.QuizQuestions.OrderBy(x => Guid.NewGuid()).ToList()
                : quiz.QuizQuestions.ToList();

            var viewModel = new TakeQuizViewModel
            {
                QuizId = quiz.QuizId,
                AttemptId = attempt.AttemptId,
                Title = quiz.Title,
                Description = quiz.Description,
                LessonTitle = quiz.Lesson.Title,
                CourseTitle = quiz.Lesson.Course.Title,
                Questions = questions.Select(q => new QuizQuestionTakeViewModel
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Points = q.Points,
                    Answers = q.QuizAnswers.Select(a => new QuizAnswerTakeViewModel
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText
                    }).ToList()
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Quiz/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(SubmitQuizViewModel model)
        {
            try
            {
                var attempt = await _context.UserQuizAttempts
                    .Include(a => a.Quiz)
                        .ThenInclude(q => q.QuizQuestions)
                    .FirstOrDefaultAsync(a => a.AttemptId == model.AttemptId);

                if (attempt == null)
                    return NotFound();

                // Lưu câu trả lời
                foreach (var answer in model.Answers)
                {
                    var userAnswer = new UserQuizAnswer
                    {
                        AttemptId = model.AttemptId,
                        QuestionId = answer.QuestionId,
                        SelectedAnswerId = answer.SelectedAnswerId,
                        EssayAnswer = answer.EssayAnswer,
                        CreatedAt = DateTime.Now
                    };

                    _context.UserQuizAnswers.Add(userAnswer);
                }

                await _context.SaveChangesAsync();

                // Gọi stored procedure để tự động chấm trắc nghiệm
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_GradeMultipleChoiceQuiz @AttemptID = {0}",
                    model.AttemptId);

                TempData["SuccessMessage"] = "Quiz submitted successfully!";
                return RedirectToAction("Result", new { id = model.AttemptId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error submitting quiz: {ex.Message}";
                return RedirectToAction("Take", new { id = model.AttemptId });
            }
        }

        // GET: Quiz/Result/5 (AttemptId)
        public async Task<IActionResult> Result(int id)
        {
            var attempt = await _context.UserQuizAttempts
                .Include(a => a.User)
                .Include(a => a.Quiz)
                .Include(a => a.UserQuizAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuizAnswers)
                .FirstOrDefaultAsync(a => a.AttemptId == id);

            if (attempt == null)
                return NotFound();

            var viewModel = new QuizResultViewModel
            {
                AttemptId = attempt.AttemptId,
                StudentName = $"{attempt.User.FirstName} {attempt.User.LastName}",
                QuizTitle = attempt.Quiz.Title,
                SubmittedAt = attempt.SubmittedAt,
                TotalScore = attempt.TotalScore,
                MaxScore = attempt.MaxScore,
                PercentageScore = attempt.PercentageScore,
                Status = attempt.Status,
                Questions = attempt.UserQuizAnswers.Select(ua => new QuizQuestionResultViewModel
                {
                    QuestionId = ua.QuestionId,
                    QuestionText = ua.Question.QuestionText,
                    QuestionType = ua.Question.QuestionType,
                    Points = ua.Question.Points,
                    EarnedPoints = ua.EarnedPoints,
                    IsCorrect = ua.IsCorrect,
                    Explanation = ua.Question.Explanation,
                    SelectedAnswerId = ua.SelectedAnswerId,
                    CorrectAnswerId = ua.Question.QuizAnswers.FirstOrDefault(a => a.IsCorrect)?.AnswerId,
                    Answers = ua.Question.QuizAnswers.Select(a => new QuizAnswerTakeViewModel
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText
                    }).ToList(),
                    EssayAnswer = ua.EssayAnswer,
                    TeacherFeedback = ua.TeacherFeedback,
                    GradedAt = ua.GradedAt
                }).ToList()
            };

            return View(viewModel);
        }

        // ========================================
        // GIÁO VIÊN CHẤM BÀI ESSAY
        // ========================================

        // GET: Quiz/EssaysToGrade?courseId=5
        public async Task<IActionResult> EssaysToGrade(int? courseId)
        {
            var query = _context.UserQuizAnswers
                .Include(ua => ua.Attempt)
                    .ThenInclude(a => a.User)
                .Include(ua => ua.Question)
                    .ThenInclude(q => q.Quiz)
                        .ThenInclude(quiz => quiz.Lesson)
                            .ThenInclude(l => l.Course)
                .Where(ua => ua.Question.QuestionType == "essay"
                          && ua.GradedBy == null
                          && ua.Attempt.Status == "submitted");

            if (courseId.HasValue)
                query = query.Where(ua => ua.Question.Quiz.Lesson.Course.CourseId == courseId.Value);

            var essays = await query
                .OrderBy(ua => ua.CreatedAt)
                .ToListAsync();

            return View(essays);
        }

        // GET: Quiz/GradeEssay/5 (UserAnswerId)
        public async Task<IActionResult> GradeEssay(int id)
        {
            var userAnswer = await _context.UserQuizAnswers
                .Include(ua => ua.Attempt)
                    .ThenInclude(a => a.User)
                .Include(ua => ua.Question)
                    .ThenInclude(q => q.Quiz)
                .FirstOrDefaultAsync(ua => ua.UserAnswerId == id);

            if (userAnswer == null)
                return NotFound();

            var viewModel = new GradeEssayViewModel
            {
                UserAnswerId = userAnswer.UserAnswerId,
                StudentName = $"{userAnswer.Attempt.User.FirstName} {userAnswer.Attempt.User.LastName}",
                QuizTitle = userAnswer.Question.Quiz.Title,
                QuestionText = userAnswer.Question.QuestionText,
                MaxPoints = userAnswer.Question.Points,
                EssayAnswer = userAnswer.EssayAnswer ?? "",
                EarnedPoints = userAnswer.EarnedPoints,
                TeacherFeedback = userAnswer.TeacherFeedback
            };

            return View(viewModel);
        }

        // POST: Quiz/GradeEssay/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GradeEssay(int id, GradeEssayViewModel model)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_GradeEssayAnswer @UserAnswerID = {0}, @EarnedPoints = {1}, @TeacherFeedback = {2}, @GradedBy = {3}",
                    id, model.EarnedPoints, model.TeacherFeedback ?? "", currentUserId);

                TempData["SuccessMessage"] = "Essay graded successfully!";
                return RedirectToAction("EssaysToGrade");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error grading essay: {ex.Message}";
                return View(model);
            }
        }

        // POST: Quiz/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var quiz = await _context.Quizzes.FindAsync(id);
                if (quiz != null)
                {
                    var lessonId = quiz.LessonId;
                    _context.Quizzes.Remove(quiz);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Quiz deleted successfully!";
                    return RedirectToAction("Edit", "Lesson", new { id = lessonId });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting quiz: {ex.Message}";
            }

            return RedirectToAction("Index", "Lesson");
        }
    }
}