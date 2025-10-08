using Microsoft.AspNetCore.Http;

namespace DoAnChuyenNganh.Services
{
    public interface IFileUploadService
    {
        /// <summary>
        /// Uploads an image file with validation and optional resizing
        /// </summary>
        Task<(bool Success, string? FilePath, string Message)> UploadImageAsync(
            IFormFile file, 
            string uploadFolder, 
            int? maxWidth = null, 
            int? maxHeight = null,
            long maxSizeBytes = 5242880); // 5MB default

        /// <summary>
        /// Uploads a file with validation
        /// </summary>
        Task<(bool Success, string? FilePath, string Message)> UploadFileAsync(
            IFormFile file, 
            string uploadFolder, 
            long maxSizeBytes = 10485760); // 10MB default

        /// <summary>
        /// Uploads an avatar image with resizing
        /// </summary>
        Task<(bool Success, string? FilePath, string Message)> UploadAvatarAsync(
            IFormFile file, 
            int width = 200, 
            int height = 200,
            long maxSizeBytes = 2097152); // 2MB default

        /// <summary>
        /// Deletes a file from the file system
        /// </summary>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Validates if a file is an allowed image type
        /// </summary>
        bool IsValidImageType(string fileName, string mimeType);

        /// <summary>
        /// Validates if a file is an allowed file type
        /// </summary>
        bool IsValidFileType(string fileName, string mimeType);

        /// <summary>
        /// Gets a safe filename by removing invalid characters
        /// </summary>
        string GetSafeFileName(string fileName);

        /// <summary>
        /// Gets the file size in a human-readable format
        /// </summary>
        string GetFileSizeString(long bytes);
    }
}

