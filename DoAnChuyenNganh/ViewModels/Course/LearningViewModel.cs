using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModels.CoursesViewModels
{
    public class LearningViewModel
    {
        public Course Course { get; set; }
        public List<Lesson> Lessons { get; set; } = new();
        public int? CurrentLessonId { get; set; }
        public List<CourseMaterial> CurrentMaterials { get; set; } = new();
        public Dictionary<int, bool> HasProgressForLesson { get; set; } = new();

        public Lesson CurrentLesson =>
            Lessons?.FirstOrDefault(l => l.LessonId == (CurrentLessonId ?? 0))
            ?? Lessons.FirstOrDefault();
    }
}
