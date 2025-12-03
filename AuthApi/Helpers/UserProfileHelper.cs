using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AuthApi.DTOs;
using AuthApi.Models;
using Microsoft.AspNetCore.Http;

namespace AuthApi.Helpers
{
    public static class UserProfileHelper
    {
        public static void UpdateUserFields(AuthUser user, UpdateProfileDto dto)
        {
            if (!string.IsNullOrEmpty(dto.FullName))
                user.FullName = dto.FullName;

            if (!string.IsNullOrEmpty(dto.EmployeeNumber))
                user.EmployeeNumber = dto.EmployeeNumber;

            if (!string.IsNullOrEmpty(dto.ExtensionNumber))
                user.ExtensionNumber = dto.ExtensionNumber;

            if (!string.IsNullOrEmpty(dto.Location))
                user.Location = dto.Location;

            if (!string.IsNullOrEmpty(dto.Unit))
                user.Unit = dto.Unit;

            if (!string.IsNullOrEmpty(dto.Title))
                user.Title = dto.Title;
        }

        public static async Task<string> HandleProfilePictureAsync(IFormFile file, string oldEmployeeNumber, string newEmployeeNumber, string oldProfilePictureUrl)
        {
            if (file == null)
                return null;

            ValidateFile(file);

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string fileName = $"{Guid.NewGuid()}{extension}";

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            string userFolder = Path.Combine(uploadsFolder, newEmployeeNumber);

            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }

            if (oldEmployeeNumber != newEmployeeNumber && !string.IsNullOrEmpty(oldEmployeeNumber))
            {
                string oldUserFolder = Path.Combine(uploadsFolder, oldEmployeeNumber);
                if (Directory.Exists(oldUserFolder) && oldUserFolder != userFolder)
                {
                    if (!Directory.Exists(userFolder))
                    {
                        Directory.Move(oldUserFolder, userFolder);
                    }
                    else
                    {
                        foreach (var oldFilePath in Directory.GetFiles(oldUserFolder))
                        {
                            string fileNameToCopy = Path.GetFileName(oldFilePath);
                            string destPath = Path.Combine(userFolder, fileNameToCopy);
                            if (File.Exists(destPath))
                            {
                                File.Delete(destPath);
                            }
                            File.Move(oldFilePath, destPath);
                        }

                        if (Directory.GetFiles(oldUserFolder).Length == 0)
                        {
                            Directory.Delete(oldUserFolder);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(oldProfilePictureUrl))
            {
                DeleteOldProfilePicture(oldProfilePictureUrl);
            }

            string fullFilePath = Path.Combine(userFolder, fileName);
            using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"uploads/{newEmployeeNumber}/{fileName}";
        }

        public static void DeleteOldProfilePicture(string oldProfilePictureUrl)
        {
            if (string.IsNullOrEmpty(oldProfilePictureUrl))
                return;

            try
            {
                string relativePath = oldProfilePictureUrl[oldProfilePictureUrl.IndexOf("uploads/")..];
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception)
            {
            }
        }

        public static string UpdateProfilePictureLocation(string oldEmployeeNumber, string newEmployeeNumber, string oldProfilePictureUrl)
        {
            if (string.IsNullOrEmpty(oldProfilePictureUrl) || string.IsNullOrEmpty(oldEmployeeNumber) || string.IsNullOrEmpty(newEmployeeNumber))
                return oldProfilePictureUrl;

            try
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                string oldUserFolder = Path.Combine(uploadsFolder, oldEmployeeNumber);
                string newUserFolder = Path.Combine(uploadsFolder, newEmployeeNumber);
                string relativePath = oldProfilePictureUrl[oldProfilePictureUrl.IndexOf("uploads/")..];
                string oldFileName = Path.GetFileName(relativePath);
                string oldFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                string newFullPath = Path.Combine(newUserFolder, oldFileName);

                if (!Directory.Exists(newUserFolder))
                {
                    Directory.CreateDirectory(newUserFolder);
                }

                if (File.Exists(oldFullPath))
                {
                    if (File.Exists(newFullPath))
                    {
                        File.Delete(newFullPath);
                    }

                    File.Move(oldFullPath, newFullPath);

                    if (Directory.Exists(oldUserFolder) && Directory.GetFiles(oldUserFolder).Length == 0)
                    {
                        Directory.Delete(oldUserFolder);
                    }

                    return $"uploads/{newEmployeeNumber}/{oldFileName}";
                }

                return oldProfilePictureUrl;
            }
            catch (Exception)
            {
                return oldProfilePictureUrl;
            }
        }

        public static void ValidateFile(IFormFile file)
        {
            var maxFileSize = 2 * 1024 * 1024;
            if (file.Length > maxFileSize)
                throw new InvalidOperationException("Dosya boyutu 2 MB'dan fazla olamaz.");

            var allowedMimeTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new InvalidOperationException("Geçersiz dosya MIME tipi.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Geçersiz dosya uzantısı.");
        }
    }
}