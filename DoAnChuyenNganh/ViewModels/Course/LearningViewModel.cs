using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Course;

namespace DoAnChuyenNganh.ViewModels.CoursesViewModels
{
    public class LearningViewModel
    {
        public DoAnChuyenNganh.Models.Course Course { get; set; }
        public List<Lesson> Lessons { get; set; } = new();
        public int? CurrentLessonId { get; set; }
        public List<CourseMaterial> CurrentMaterials { get; set; } = new();
        public Dictionary<int, bool> HasProgressForLesson { get; set; } = new();
        public int EnrollmentId { get; set; }
        public int? QuizId { get; set; }
        public DoAnChuyenNganh.ViewModels.Course.TakeQuizViewModel? QuizToTake { get; set; }
        public DoAnChuyenNganh.ViewModels.Course.QuizResultViewModel? QuizResult { get; set; }

        public Lesson CurrentLesson =>
            Lessons?.FirstOrDefault(l => l.LessonId == (CurrentLessonId ?? 0))
            ?? Lessons.FirstOrDefault();
    }
}
