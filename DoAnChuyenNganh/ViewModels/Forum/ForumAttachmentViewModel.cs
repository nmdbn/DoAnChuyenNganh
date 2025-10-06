namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class ForumAttachmentViewModel
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = null!;
        public string OriginalFileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string FileType { get; set; } = null!; // 'image' or 'file'
        public string MimeType { get; set; } = null!;
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }
}

