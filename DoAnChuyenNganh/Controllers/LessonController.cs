using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAnChuyenNganh.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace DoAnChuyenNganh.Controllers
{
    public class LessonController : Controller
    {
        private readonly DoAnChuyenNganhContext _context;

        public LessonController(DoAnChuyenNganhContext context)
        {
            _context = context;
        }

        // ==== Helper permission: chỉ Instructor + Admin ====
        private bool HasInstructorOrAdminPermission()
        {
            // 1. Thử đọc từ Claims (nếu bạn có set khi login)
            var roleClaim = User.FindFirst("RoleName")?.Value;
            if (!string.IsNullOrEmpty(roleClaim))
            {
                if (roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    roleClaim.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // 2. Thử đọc từ Session
            var roleName = HttpContext.Session.GetString("RoleName");
            if (!string.IsNullOrEmpty(roleName))
            {
                if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    roleName.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // 3. PermissionLevel trong Session (3 = Instructor, 4 = Admin)
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

        // Hàm helper lấy UserId từ user đang đăng nhập
        private int GetCurrentUserId()
        {
            // Cách 1: Nếu bạn lưu UserId trong Claims
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            // Cách 2: Nếu bạn lưu UserId trong Session
            // var userId = HttpContext.Session.GetInt32("UserId");
            // if (userId.HasValue) return userId.Value;

            // Mặc định trả về 1 nếu không tìm thấy (hoặc throw exception)
            return 1; // TODO: Thay đổi logic này theo cách bạn quản lý session
        }

        // GET: Lesson
        public async Task<IActionResult> Index()
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            var lessons = _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.CreatedByNavigation)
                .Include(l => l.UpdatedByNavigation)
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

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title");
            // Không cần ViewData cho CreatedBy và UpdatedBy nữa
            return View();
        }

        // POST: Lesson/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lesson lesson, string? quizRawText, string? quizAnswerKey)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            // Loại bỏ validation cho các field navigation properties
            ModelState.Remove("Course");
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("UpdatedByNavigation");
            ModelState.Remove("CourseMaterials");
            ModelState.Remove("LessonProgresses");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("UpdatedBy");

            if (ModelState.IsValid)
            {
                try
                {
                    int currentUserId = GetCurrentUserId();

                    lesson.CreatedBy = currentUserId;
                    lesson.UpdatedBy = currentUserId;
                    lesson.CreatedAt = DateTime.Now;
                    lesson.UpdatedAt = DateTime.Now;

                    _context.Add(lesson);
                    await _context.SaveChangesAsync();   // để có LessonId

                    // Nếu lesson là quiz → tạo Quiz từ text
                    if (lesson.LessonType == "quiz")
                    {
                        await CreateOrReplaceQuizAsync(lesson, currentUserId, quizRawText, quizAnswerKey);
                    }

                    TempData["SuccessMessage"] = "Lesson created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating lesson: {ex.Message}";
                }
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", lesson.CourseId);
            return View(lesson);
        }


        // GET: Lesson/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            if (id == null)
                return NotFound();

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
                return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", lesson.CourseId);
            return View(lesson);
        }

        // POST: Lesson/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Lesson lesson, string? quizRawText, string? quizAnswerKey)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            if (id != lesson.LessonId)
                return NotFound();

            ModelState.Remove("Course");
            ModelState.Remove("CreatedByNavigation");
            ModelState.Remove("UpdatedByNavigation");
            ModelState.Remove("CourseMaterials");
            ModelState.Remove("LessonProgresses");
            ModelState.Remove("UpdatedBy");

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Lessons
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.LessonId == id);

                    if (existing == null)
                        return NotFound();

                    // giữ created
                    lesson.CreatedAt = existing.CreatedAt;
                    lesson.CreatedBy = existing.CreatedBy;

                    lesson.UpdatedBy = GetCurrentUserId();
                    lesson.UpdatedAt = DateTime.Now;

                    _context.Update(lesson);
                    await _context.SaveChangesAsync();

                    // rebuild quiz nếu là quiz
                    if (lesson.LessonType == "quiz")
                    {
                        await CreateOrReplaceQuizAsync(
                            lesson,
                            lesson.UpdatedBy ?? GetCurrentUserId(),
                            quizRawText,
                            quizAnswerKey);
                    }

                    TempData["SuccessMessage"] = "Lesson updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LessonExists(lesson.LessonId))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error updating lesson: {ex.Message}";
                }
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", lesson.CourseId);
            return View(lesson);
        }


        // GET: Lesson/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!HasInstructorOrAdminPermission())
                return ForbidToHome();

            if (id == null)
                return NotFound();

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.CreatedByNavigation)
                .Include(l => l.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.LessonId == id);

            if (lesson == null)
                return NotFound();

            return View(lesson);
        }

        // POST: Lesson/Delete/5
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
            public char Label { get; set; }  // a,b,c,d
            public string Text { get; set; }
        }

        private class ParsedQuestion
        {
            public int Number { get; set; }  // 1,2,3...
            public string QuestionText { get; set; }
            public List<ParsedAnswer> Answers { get; set; } = new();
        }

        // Parse chuỗi "1.a 2.b 3.d"
        private Dictionary<int, char> ParseAnswerKey(string? key)
        {
            var dict = new Dictionary<int, char>();

            if (string.IsNullOrWhiteSpace(key))
                return dict;

            var tokens = Regex.Split(key, @"[;\r\n\s,]+"); // tách theo space, xuống dòng, ; , 
            foreach (var raw in tokens)
            {
                var t = raw.Trim();
                if (string.IsNullOrEmpty(t)) continue;

                // Hỗ trợ dạng "1.a", "1:a", "1-a", "1 A"
                var m = Regex.Match(t, @"^(\d+)\s*[\.\:\-]?\s*([a-dA-D])$");
                if (!m.Success) continue;

                int qNum = int.Parse(m.Groups[1].Value);
                char letter = char.ToLower(m.Groups[2].Value[0]);
                dict[qNum] = letter;
            }

            return dict;
        }

        // Parse nội dung câu hỏi, ví dụ:
        // Câu 1: Vai trò chính...
        //
        // a. ...
        // b. ...
        // c. ...
        // d. ...
        private List<ParsedQuestion> ParseQuizRaw(string raw)
        {
            var result = new List<ParsedQuestion>();

            if (string.IsNullOrWhiteSpace(raw))
                return result;

            // Tách theo "Câu x:"
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

                // dòng đầu: "Câu 1: ...."
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

                // các dòng còn lại: "a. .....", "b. ....."
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

        // Tạo (hoặc replace) Quiz cho 1 lesson
        private async Task CreateOrReplaceQuizAsync(
            Lesson lesson,
            int currentUserId,
            string? quizRawText,
            string? quizAnswerKey)
        {
            if (lesson == null) return;
            if (string.IsNullOrWhiteSpace(quizRawText)) return;

            // 1. Xoá quiz cũ (nếu có)
            var existingQuiz = await _context.Quizzes
                .Include(q => q.QuizQuestions)
                    .ThenInclude(qq => qq.QuizAnswers)
                .FirstOrDefaultAsync(q => q.LessonId == lesson.LessonId);

            if (existingQuiz != null)
            {
                // xoá theo thứ tự con → cha
                _context.QuizAnswers.RemoveRange(
                    existingQuiz.QuizQuestions.SelectMany(x => x.QuizAnswers));

                _context.QuizQuestions.RemoveRange(existingQuiz.QuizQuestions);
                _context.Quizzes.Remove(existingQuiz);

                await _context.SaveChangesAsync();
            }

            // 2. Parse text
            var parsedQuestions = ParseQuizRaw(quizRawText);
            var answerKey = ParseAnswerKey(quizAnswerKey);

            if (!parsedQuestions.Any())
                return;

            // 3. Tạo quiz mới
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

                // câu đúng theo answer key (nếu có)
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
        // ================== END QUIZ HELPER ==================

    }
}
