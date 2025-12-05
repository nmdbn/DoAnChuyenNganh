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
        public int? SelectedAnswerId { get; set; } // Cho multiple choice
        public string? EssayAnswer { get; set; } // Cho essay
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
        public int? SelectedAnswerId { get; set; }
        public int? CorrectAnswerId { get; set; }
        public List<QuizAnswerTakeViewModel> Answers { get; set; } = new();
        public string? Explanation { get; set; }
    }

}
