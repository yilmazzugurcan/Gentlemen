using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Gentlemen.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile file);
        Task<string> UploadFileAsync(IFormFile file, string folder);
        void DeleteFile(string filePath);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadsFolder;

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            
            // Ensure uploads folder exists
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            return await UploadFileAsync(file, "");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file was provided");
            }

            // Validate file type
            var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(extension))
            {
                throw new ArgumentException("Invalid file type. Only image files are allowed.");
            }

            try
            {
                // Create folder if specified
                var targetFolder = _uploadsFolder;
                if (!string.IsNullOrEmpty(folder))
                {
                    targetFolder = Path.Combine(_uploadsFolder, folder);
                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }
                }

                // Create unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(targetFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Return the relative path for web access
                return !string.IsNullOrEmpty(folder) 
                    ? $"/uploads/{folder}/{uniqueFileName}"
                    : $"/uploads/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"File upload failed: {ex.Message}", ex);
            }
        }

        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                // Convert web path to physical path
                var fileName = Path.GetFileName(filePath.TrimStart('/'));
                var folder = Path.GetDirectoryName(filePath.TrimStart('/'))?.Replace("uploads/", "") ?? "";
                var physicalPath = string.IsNullOrEmpty(folder) 
                    ? Path.Combine(_uploadsFolder, fileName)
                    : Path.Combine(_uploadsFolder, folder, fileName);

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"File deletion failed: {ex.Message}", ex);
            }
        }
    }
} 