using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Text.RegularExpressions;

namespace DoAnChuyenNganh.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;

        // Allowed image extensions and MIME types
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedImageMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        // Allowed file extensions and MIME types
        private readonly string[] _allowedFileExtensions = { ".pdf", ".doc", ".docx", ".zip", ".rar" };
        private readonly string[] _allowedFileMimeTypes = { 
            "application/pdf", 
            "application/msword", 
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/zip",
            "application/x-zip-compressed",
            "application/x-rar-compressed",
            "application/vnd.rar"
        };

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<(bool Success, string? FilePath, string Message)> UploadImageAsync(
            IFormFile file, 
            string uploadFolder, 
            int? maxWidth = null, 
            int? maxHeight = null,
            long maxSizeBytes = 5242880)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return (false, null, "No file uploaded");
                }

                if (file.Length > maxSizeBytes)
                {
                    return (false, null, $"File size exceeds maximum allowed size of {GetFileSizeString(maxSizeBytes)}");
                }

                if (!IsValidImageType(file.FileName, file.ContentType))
                {
                    return (false, null, "Invalid image type. Allowed types: JPG, PNG, GIF, WebP");
                }

                // Generate safe filename
                var safeFileName = GetSafeFileName(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                var uploadPath = Path.Combine(_environment.WebRootPath, uploadFolder);
                
                // Ensure directory exists
                Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, uniqueFileName);
                var relativePath = $"/{uploadFolder}/{uniqueFileName}".Replace("\\", "/");

                // Process and save image
                using (var image = await Image.LoadAsync(file.OpenReadStream()))
                {
                    // Resize if dimensions specified
                    if (maxWidth.HasValue || maxHeight.HasValue)
                    {
                        var resizeOptions = new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth ?? image.Width, maxHeight ?? image.Height)
                        };
                        image.Mutate(x => x.Resize(resizeOptions));
                    }

                    // Save as JPEG with quality 85
                    await image.SaveAsJpegAsync(filePath, new JpegEncoder { Quality = 85 });
                }

                _logger.LogInformation($"Image uploaded successfully: {relativePath}");
                return (true, relativePath, "Image uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return (false, null, "An error occurred while uploading the image");
            }
        }

        public async Task<(bool Success, string? FilePath, string Message)> UploadFileAsync(
            IFormFile file, 
            string uploadFolder, 
            long maxSizeBytes = 10485760)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return (false, null, "No file uploaded");
                }

                if (file.Length > maxSizeBytes)
                {
                    return (false, null, $"File size exceeds maximum allowed size of {GetFileSizeString(maxSizeBytes)}");
                }

                if (!IsValidFileType(file.FileName, file.ContentType))
                {
                    return (false, null, "Invalid file type. Allowed types: PDF, DOC, DOCX, ZIP, RAR");
                }

                // Generate safe filename
                var safeFileName = GetSafeFileName(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                var uploadPath = Path.Combine(_environment.WebRootPath, uploadFolder);
                
                // Ensure directory exists
                Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, uniqueFileName);
                var relativePath = $"/{uploadFolder}/{uniqueFileName}".Replace("\\", "/");

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"File uploaded successfully: {relativePath}");
                return (true, relativePath, "File uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return (false, null, "An error occurred while uploading the file");
            }
        }

        public async Task<(bool Success, string? FilePath, string Message)> UploadAvatarAsync(
            IFormFile file, 
            int width = 200, 
            int height = 200,
            long maxSizeBytes = 2097152)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return (false, null, "No file uploaded");
                }

                if (file.Length > maxSizeBytes)
                {
                    return (false, null, $"File size exceeds maximum allowed size of {GetFileSizeString(maxSizeBytes)}");
                }

                if (!IsValidImageType(file.FileName, file.ContentType))
                {
                    return (false, null, "Invalid image type. Allowed types: JPG, PNG, GIF");
                }

                // Generate safe filename
                var safeFileName = GetSafeFileName(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                
                // Ensure directory exists
                Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, uniqueFileName);
                var relativePath = $"/uploads/avatars/{uniqueFileName}";

                // Process and save avatar
                using (var image = await Image.LoadAsync(file.OpenReadStream()))
                {
                    // Resize to exact dimensions (crop if necessary)
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Crop,
                        Size = new Size(width, height)
                    }));

                    // Save as JPEG with quality 90
                    await image.SaveAsJpegAsync(filePath, new JpegEncoder { Quality = 90 });
                }

                _logger.LogInformation($"Avatar uploaded successfully: {relativePath}");
                return (true, relativePath, "Avatar uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return (false, null, "An error occurred while uploading the avatar");
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                
                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    _logger.LogInformation($"File deleted successfully: {filePath}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
                return false;
            }
        }

        public bool IsValidImageType(string fileName, string mimeType)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _allowedImageExtensions.Contains(extension) && 
                   _allowedImageMimeTypes.Contains(mimeType.ToLowerInvariant());
        }

        public bool IsValidFileType(string fileName, string mimeType)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _allowedFileExtensions.Contains(extension) && 
                   _allowedFileMimeTypes.Contains(mimeType.ToLowerInvariant());
        }

        public string GetSafeFileName(string fileName)
        {
            // Remove path information
            fileName = Path.GetFileName(fileName);
            
            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeFileName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Remove any remaining potentially dangerous characters
            safeFileName = Regex.Replace(safeFileName, @"[^\w\.-]", "_");
            
            // Limit length
            if (safeFileName.Length > 200)
            {
                var extension = Path.GetExtension(safeFileName);
                safeFileName = safeFileName.Substring(0, 200 - extension.Length) + extension;
            }

            return safeFileName;
        }

        public string GetFileSizeString(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}

