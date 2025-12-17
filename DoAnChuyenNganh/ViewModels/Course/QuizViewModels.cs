using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.ViewModels.Course
{
    public class TakeQuizViewModel
    {
        public int AttemptId { get; set; }
        public string LessonTitle { get; set; } = null!;
        public string CourseTitle { get; set; } = null!;
        public List<QuizQuestionTakeViewModel> Questions { get; set; } = new();
    }

    public class QuizQuestionTakeViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public int Points { get; set; }

        // ✅ CRITICAL: Biến này cho phép frontend biết câu hỏi có cho phép chọn nhiều đáp án không
        public bool AllowMultipleCorrect { get; set; }

        public List<QuizAnswerTakeViewModel> Answers { get; set; } = new();
    }

    public class QuizAnswerTakeViewModel
    {
        public int AnswerId { get; set; }
        public string AnswerText { get; set; } = null!;
    }

    // ViewModel submit quiz
    public class SubmitQuizViewModel
    {
        public int AttemptId { get; set; }
        public List<QuizAnswerSubmitViewModel> Answers { get; set; } = new();
    }

    public class QuizAnswerSubmitViewModel
    {
        public int QuestionId { get; set; }
        public int? SelectedAnswerId { get; set; }
        // ✅ CHANGED: Hỗ trợ nhiều đáp án (cho câu hỏi AllowMultipleCorrect = true)
        public List<int>? SelectedAnswerIds { get; set; }

        // Cho essay
        public string? EssayAnswer { get; set; }
    }

    // ViewModel xem kết quả
    public class QuizResultViewModel
    {
        public int AttemptId { get; set; }
        public string QuizTitle { get; set; } = null!;
        public decimal TotalScore { get; set; }
        public int MaxScore { get; set; }
        public decimal PercentageScore { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? SubmittedAt { get; set; }
        public List<QuizQuestionResultViewModel> Questions { get; set; } = new();
    }

    public class QuizQuestionResultViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public int Points { get; set; }
        public decimal EarnedPoints { get; set; }
        public bool AllowMultipleCorrect { get; set; }
        // ✅ CHANGED: Hỗ trợ nhiều đáp án đã chọn
        public List<int>? SelectedAnswerIds { get; set; }

        // ✅ CHANGED: Hỗ trợ nhiều đáp án đúng
        public List<int>? CorrectAnswerIds { get; set; }

        public List<QuizAnswerTakeViewModel> Answers { get; set; } = new();
        public string? Explanation { get; set; }
    }

    // ViewModel cho Create/Edit Lesson với Quiz
    public class LessonQuizUpsertViewModel
    {
        public int LessonId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khóa học.")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Tiêu đề bài học không được trống.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được quá 200 ký tự.")]
        public string Title { get; set; } = "";

        public string? Content { get; set; }

        [Required]
        public string LessonType { get; set; } = "text"; // text, video, quiz

        public string? VideoUrl { get; set; }
        public IFormFile? ThumbnailFile { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập thứ tự bài học.")]
        [Range(1, 9999, ErrorMessage = "Thứ tự phải từ 1 đến 9999.")]
        public int LessonOrder { get; set; }

        public int? VideoDuration { get; set; }
        public bool IsPreviewable { get; set; }
        public bool IsPublished { get; set; }

        // Quiz fields (chỉ sử dụng khi LessonType = "quiz")
        public string? QuizTitle { get; set; }
        public string? QuizDescription { get; set; }
        public bool IsRandomOrder { get; set; }

        public List<QuizQuestionVM> Questions { get; set; } = new();
    }

    public class QuizQuestionVM
    {
        public int? QuestionId { get; set; } // null khi tạo mới

        [Required(ErrorMessage = "Câu hỏi không được trống.")]
        public string QuestionText { get; set; } = "";

        [Required]
        public string QuestionType { get; set; } = "multiple_choice";

        [Range(1, 100, ErrorMessage = "Điểm phải từ 1 đến 100.")]
        public int Points { get; set; } = 10;

        public string? Explanation { get; set; }

        [MinLength(2, ErrorMessage = "Mỗi câu phải có ít nhất 2 đáp án.")]
        [MaxLength(5, ErrorMessage = "Mỗi câu tối đa 5 đáp án.")]
        public List<QuizAnswerVM> Answers { get; set; } = new();

        // ✅ COMPUTED PROPERTY: Tự động tính dựa trên số đáp án đúng
        // > 1 đáp án đúng = cho phép chọn nhiều
        // = 1 đáp án đúng = chỉ chọn 1
        public bool AllowMultipleCorrect => Answers?.Count(a => a.IsCorrect) > 1;
    }

    public class QuizAnswerVM
    {
        public int? AnswerId { get; set; } // null khi tạo mới

        [Required(ErrorMessage = "Đáp án không được trống.")]
        public string AnswerText { get; set; } = "";

        public bool IsCorrect { get; set; } = false;

        public byte AnswerOrder { get; set; } = 1;
    }
}