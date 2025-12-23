namespace DoAnChuyenNganh.ViewModels.Course
{
    public class CourseRatingViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public byte Rating { get; set; }
        public string? Review { get; set; }

        // Thông tin hiện tại
        public bool HasRated { get; set; }
        public CourseRatingInfo? ExistingRating { get; set; }
        public decimal AverageRating { get; set; }
        public int RatingCount { get; set; }
    }

    public class CourseRatingInfo
    {
        public int RatingId { get; set; }
        public byte Rating { get; set; }
        public string? Review { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class SubmitRatingViewModel
    {
        public int CourseId { get; set; }
        public byte Rating { get; set; }
        public string? Review { get; set; }
    }

}
