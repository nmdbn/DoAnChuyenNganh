using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnChuyenNganh.Models
{
    // Bảng Quiz
    public partial class Quiz
    {
        public int QuizId { get; set; }
        public int LessonId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }
        public bool IsRandomOrder { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public virtual Lesson Lesson { get; set; } = null!;
        [ForeignKey(nameof(CreatedBy))]
        public virtual User CreatedByNavigation { get; set; } = null!;

        [ForeignKey(nameof(UpdatedBy))]
        public virtual User? UpdatedByNavigation { get; set; }
        public virtual ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
        public virtual ICollection<UserQuizAttempt> UserQuizAttempts { get; set; } = new List<UserQuizAttempt>();
    }

    // Bảng QuizQuestion
    public partial class QuizQuestion
    {
        //public int QuestionId { get; set; }
        //public int QuizId { get; set; }

        //[Required]
        //public string QuestionText { get; set; } = null!;

        //[Required]
        //[StringLength(20)]
        //public string QuestionType { get; set; } = "multiple_choice"; // "multiple_choice" hoặc "essay"

        //public int QuestionOrder { get; set; }
        //public int Points { get; set; } = 10;
        //public string? Explanation { get; set; }
        //public DateTime? CreatedAt { get; set; }

        //// Navigation
        //public virtual Quiz Quiz { get; set; } = null!;
        //public virtual ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
        //public virtual ICollection<UserQuizAnswer> UserQuizAnswers { get; set; } = new List<UserQuizAnswer>();
        public QuizQuestion()
        {
            QuizAnswers = new HashSet<QuizAnswer>();
            UserQuizAnswers = new HashSet<UserQuizAnswer>();
        }

        [Key]
        public int QuestionId { get; set; }

        [Required]
        public int QuizId { get; set; }

        [Required]
        public string QuestionText { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string QuestionType { get; set; } = "multiple_choice"; // 'multiple_choice' hoặc 'essay'

        [Required]
        public int QuestionOrder { get; set; }

        public int Points { get; set; } = 10;

        public string? Explanation { get; set; }

        // NEW: Biến để đánh dấu câu hỏi có nhiều đáp án đúng
        public bool AllowMultipleCorrect { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("QuizId")]
        public virtual Quiz Quiz { get; set; } = null!;

        public virtual ICollection<QuizAnswer> QuizAnswers { get; set; }
        public virtual ICollection<UserQuizAnswer> UserQuizAnswers { get; set; }
    }

    // Bảng QuizAnswer - Tối đa 4 đáp án, 1 đúng
    public partial class QuizAnswer
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }

        [Required]
        public string AnswerText { get; set; } = null!;

        public bool IsCorrect { get; set; }

        [Range(1, 5)]
        public byte AnswerOrder { get; set; }
        //[ForeignKey(nameof(QuestionId))]
        //public virtual QuizQuestion QuizQuestion { get; set; } = null!;

        // Navigation
        public virtual QuizQuestion Question { get; set; } = null!;
        public virtual ICollection<UserQuizAnswer> UserQuizAnswers { get; set; } = new List<UserQuizAnswer>();
    }

    // Bảng UserQuizAttempt
    public partial class UserQuizAttempt
    {
        public int AttemptId { get; set; }
        public int UserId { get; set; }
        public int QuizId { get; set; }
        public int EnrollmentId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal TotalScore { get; set; }
        public int MaxScore { get; set; }
        public decimal PercentageScore { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "in_progress"; // "in_progress", "submitted", "graded"

        // Navigation
        public virtual User User { get; set; } = null!;
        public virtual Quiz Quiz { get; set; } = null!;
        public virtual Enrollment Enrollment { get; set; } = null!;
        public virtual ICollection<UserQuizAnswer> UserQuizAnswers { get; set; } = new List<UserQuizAnswer>();
    }

    // Bảng UserQuizAnswer
    public partial class UserQuizAnswer
    {
        public int UserAnswerId { get; set; }
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public int? SelectedAnswerId { get; set; } // NULL nếu essay
        public string? EssayAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal EarnedPoints { get; set; }
        public string? TeacherFeedback { get; set; }
        public int? GradedBy { get; set; }
        public DateTime? GradedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public virtual UserQuizAttempt Attempt { get; set; } = null!;
        public virtual QuizQuestion Question { get; set; } = null!;
        public virtual QuizAnswer? SelectedAnswer { get; set; }
        public virtual User? GradedByNavigation { get; set; }
    }

    // ===================================
    // VIEW MODELS
    // ===================================

    // ViewModel để tạo/edit quiz
    public class QuizViewModel
    {
        public int QuizId { get; set; }
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsRandomOrder { get; set; }

        public List<QuizQuestionViewModel> Questions { get; set; } = new List<QuizQuestionViewModel>();
    }

    public class QuizQuestionViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = "multiple_choice";
        public int QuestionOrder { get; set; }
        public int Points { get; set; } = 10;
        public string? Explanation { get; set; }

        public List<QuizAnswerViewModel> Answers { get; set; } = new List<QuizAnswerViewModel>();
    }

    public class QuizAnswerViewModel
    {
        public int AnswerId { get; set; }
        public string AnswerText { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public byte AnswerOrder { get; set; }
    }

    // ViewModel để học viên làm quiz
    public class TakeQuizViewModel
    {
        public int QuizId { get; set; }
        public int AttemptId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string LessonTitle { get; set; } = null!;
        public string CourseTitle { get; set; } = null!;

        public List<QuizQuestionTakeViewModel> Questions { get; set; } = new List<QuizQuestionTakeViewModel>();
    }

    public class QuizQuestionTakeViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public int Points { get; set; }

        public List<QuizAnswerTakeViewModel> Answers { get; set; } = new List<QuizAnswerTakeViewModel>();
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
        public List<QuizAnswerSubmitViewModel> Answers { get; set; } = new List<QuizAnswerSubmitViewModel>();
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
        public string StudentName { get; set; } = null!;
        public string QuizTitle { get; set; } = null!;
        public DateTime? SubmittedAt { get; set; }
        public decimal TotalScore { get; set; }
        public int MaxScore { get; set; }
        public decimal PercentageScore { get; set; }
        public string Status { get; set; } = null!;

        public List<QuizQuestionResultViewModel> Questions { get; set; } = new List<QuizQuestionResultViewModel>();
    }

    public class QuizQuestionResultViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public int Points { get; set; }
        public decimal EarnedPoints { get; set; }
        public bool? IsCorrect { get; set; }
        public string? Explanation { get; set; }

        // Cho multiple choice
        public int? SelectedAnswerId { get; set; }
        public int? CorrectAnswerId { get; set; }
        public List<QuizAnswerTakeViewModel> Answers { get; set; } = new List<QuizAnswerTakeViewModel>();

        // Cho essay
        public string? EssayAnswer { get; set; }
        public string? TeacherFeedback { get; set; }
        public DateTime? GradedAt { get; set; }
    }

    // ViewModel cho giáo viên chấm bài
    public class GradeEssayViewModel
    {
        public int UserAnswerId { get; set; }
        public string StudentName { get; set; } = null!;
        public string QuizTitle { get; set; } = null!;
        public string QuestionText { get; set; } = null!;
        public int MaxPoints { get; set; }
        public string EssayAnswer { get; set; } = null!;

        [Range(0, int.MaxValue)]
        public decimal EarnedPoints { get; set; }

        public string? TeacherFeedback { get; set; }
    }
}