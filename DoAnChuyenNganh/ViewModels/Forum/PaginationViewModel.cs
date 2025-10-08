namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartItem => (CurrentPage - 1) * PageSize + 1;
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

        public List<int> GetPageNumbers(int maxPages = 10)
        {
            var pages = new List<int>();
            var startPage = Math.Max(1, CurrentPage - maxPages / 2);
            var endPage = Math.Min(TotalPages, startPage + maxPages - 1);

            if (endPage - startPage < maxPages - 1)
            {
                startPage = Math.Max(1, endPage - maxPages + 1);
            }

            for (int i = startPage; i <= endPage; i++)
            {
                pages.Add(i);
            }

            return pages;
        }
    }
}

