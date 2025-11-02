namespace DoAnChuyenNganh.ViewModels.Chat
{
    /// <summary>
    /// ViewModel for chat message attachments
    /// </summary>
    public class ChatAttachmentViewModel
    {
        public int AttachmentId { get; set; }
        
        public int MessageId { get; set; }
        
        public string FileName { get; set; } = null!;
        
        public string OriginalFileName { get; set; } = null!;
        
        public string FilePath { get; set; } = null!;
        
        public string FileType { get; set; } = null!; // 'image' or 'file'
        
        public string MimeType { get; set; } = null!;
        
        public long FileSize { get; set; }
        
        public int UploadedBy { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        // Helper properties
        public bool IsImage => FileType == "image";
        
        public string FileSizeFormatted
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = FileSize;
                int order = 0;
                
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                
                return $"{len:0.##} {sizes[order]}";
            }
        }
        
        public string FileIcon
        {
            get
            {
                if (IsImage)
                    return "bi-image";
                
                var extension = Path.GetExtension(OriginalFileName).ToLower();
                return extension switch
                {
                    ".pdf" => "bi-file-pdf",
                    ".doc" or ".docx" => "bi-file-word",
                    ".zip" or ".rar" => "bi-file-zip",
                    _ => "bi-file-earmark"
                };
            }
        }
    }
}

