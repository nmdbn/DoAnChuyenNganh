using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class ForumSearchViewModel
    {
        [Display(Name = "Search")]
        public string? Query { get; set; }

        [Display(Name = "Category")]
        public short? CategoryId { get; set; }

        [Display(Name = "Author")]
        public string? Author { get; set; }

        [Display(Name = "Search In")]
        public string SearchIn { get; set; } = "all"; // all, titles, content, replies

        [Display(Name = "Sort By")]
        public string SortBy { get; set; } = "relevance"; // relevance, date, replies, likes

        [Display(Name = "Date From")]
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [Display(Name = "Date To")]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

        public List<ForumPostViewModel> Results { get; set; } = new List<ForumPostViewModel>();
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
        public int TotalResults { get; set; }
    }
}

