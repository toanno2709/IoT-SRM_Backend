using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels.Commons;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AppBackend.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(Cloudinary cloudinary, ILogger<CloudinaryService> logger)
    {
        _cloudinary = cloudinary;
        _logger = logger;
    }

    public async Task<ResultModel<CloudinaryUploadResponseDto>> UploadAsync(CloudinaryUploadRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return new ResultModel<CloudinaryUploadResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "No file uploaded."
            };
        }

        try
        {
            _logger.LogInformation("Uploading file: {FileName}, Size: {FileSize} bytes, ContentType: {ContentType}, Folder: {Folder}",
                request.File.FileName, request.File.Length, request.File.ContentType, request.Folder);

            // Extract filename without extension for public_id
            var fileName = Path.GetFileNameWithoutExtension(request.File.FileName);
            var fileExtension = Path.GetExtension(request.File.FileName);
            
            // Clean filename to make it URL-safe
            var safeFileName = CleanFileName(fileName);
            
            // Create public_id with folder path and original filename
            var publicId = string.IsNullOrEmpty(request.Folder) 
                ? safeFileName 
                : $"{request.Folder}/{safeFileName}";

            // Determine if it's an image or raw file (document, video, etc.)
            var isImage = request.File.ContentType?.StartsWith("image/") ?? false;
            
            UploadResult uploadResult;

            if (isImage)
            {
                // Use ImageUploadParams for images
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(request.File.FileName, request.File.OpenReadStream()),
                    PublicId = publicId,
                    Folder = request.Folder,
                    UseFilename = true,
                    UniqueFilename = false, // Don't add random string
                    Overwrite = true // Allow overwriting existing file
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                // Use RawUploadParams for documents, videos, and other files
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(request.File.FileName, request.File.OpenReadStream()),
                    PublicId = publicId,
                    Folder = request.Folder,
                    UseFilename = true,
                    UniqueFilename = false, // Don't add random string
                    Overwrite = true // Allow overwriting existing file
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {ErrorMessage}", uploadResult.Error.Message);
                return new ResultModel<CloudinaryUploadResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = $"Upload failed: {uploadResult.Error.Message}"
                };
            }

            _logger.LogInformation("File uploaded successfully. PublicId: {PublicId}, Url: {Url}",
                uploadResult.PublicId, uploadResult.SecureUrl);

            // For raw files (PDF, ZIP, etc.), create a download URL with fl_attachment flag
            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (!isImage && !string.IsNullOrEmpty(secureUrl))
            {
                secureUrl = ConvertToDownloadUrl(secureUrl, request.File.FileName);
                _logger.LogInformation("Created download URL: {DownloadUrl}", secureUrl);
            }

            var response = new CloudinaryUploadResponseDto
            {
                PublicId = uploadResult.PublicId,
                Url = uploadResult.Url?.ToString(),
                SecureUrl = secureUrl,
                Format = uploadResult.Format,
                Bytes = uploadResult.Bytes
            };

            return new ResultModel<CloudinaryUploadResponseDto>
            {
                IsSuccess = true,
                StatusCode = 200,
                Message = "Upload successful",
                Data = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during file upload: {FileName}", request.File.FileName);
            return new ResultModel<CloudinaryUploadResponseDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Upload failed: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<CloudinaryDeleteResponseDto>> DeleteAsync(string publicId)
    {
        try
        {
            _logger.LogInformation("Deleting file with publicId: {PublicId}", publicId);
            
            var delParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Raw // Try raw first for documents
            };
            var delResult = await _cloudinary.DestroyAsync(delParams);

            // If not found as raw, try as image
            if (delResult.Result == "not found")
            {
                delParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };
                delResult = await _cloudinary.DestroyAsync(delParams);
            }

            var response = new CloudinaryDeleteResponseDto
            {
                PublicId = publicId,
                Status = delResult.Result
            };

            return new ResultModel<CloudinaryDeleteResponseDto>
            {
                IsSuccess = delResult.Result == "ok",
                StatusCode = delResult.Result == "ok" ? 200 : 400,
                Message = delResult.Result == "ok" ? "Delete successful" : $"Delete failed: {delResult.Result}",
                Data = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during file deletion: {PublicId}", publicId);
            return new ResultModel<CloudinaryDeleteResponseDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Delete failed: {ex.Message}",
                Data = new CloudinaryDeleteResponseDto { PublicId = publicId, Status = "error" }
            };
        }
    }

    // Helper method for simpler file upload
    public async Task<CloudinaryUploadResponseDto?> UploadFileAsync(IFormFile file, string? folder = null)
    {
        _logger.LogInformation("UploadFileAsync called for: {FileName}, Folder: {Folder}", 
            file?.FileName, folder);

        var request = new CloudinaryUploadRequestDto
        {
            File = file,
            Folder = folder ?? "SWP391/submissions"
        };

        var result = await UploadAsync(request);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Upload failed for {FileName}: {Message}", 
                file?.FileName, result.Message);
        }
        
        return result.IsSuccess ? result.Data : null;
    }

    // Helper method for simpler file deletion
    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
            return false;

        try
        {
            // Extract public ID from Cloudinary URL
            var uri = new Uri(fileUrl);
            var pathSegments = uri.AbsolutePath.Split('/');
            
            // Cloudinary URL format: .../upload/v{version}/{folder}/{publicId}.{format}
            // Or for raw files: .../raw/upload/v{version}/{folder}/{publicId}.{format}
            var uploadIndex = Array.IndexOf(pathSegments, "upload");
            if (uploadIndex >= 0 && uploadIndex + 2 < pathSegments.Length)
            {
                // Get segments after version number
                var publicIdParts = pathSegments.Skip(uploadIndex + 2).ToArray();
                var publicIdWithExtension = string.Join("/", publicIdParts);
                
                // Remove file extension
                var lastDotIndex = publicIdWithExtension.LastIndexOf('.');
                var publicId = lastDotIndex > 0 
                    ? publicIdWithExtension.Substring(0, lastDotIndex) 
                    : publicIdWithExtension;

                _logger.LogInformation("Extracted publicId from URL: {PublicId}", publicId);

                var result = await DeleteAsync(publicId);
                return result.IsSuccess;
            }

            _logger.LogWarning("Could not extract publicId from URL: {FileUrl}", fileUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from URL: {FileUrl}", fileUrl);
            return false;
        }
    }

    /// <summary>
    /// Convert Cloudinary URL to download URL with fl_attachment flag
    /// This forces the browser to download the file instead of displaying it
    /// </summary>
    /// <param name="cloudinaryUrl">Original Cloudinary URL</param>
    /// <param name="originalFileName">Original filename to use for download</param>
    /// <returns>Download URL with fl_attachment flag</returns>
    private string ConvertToDownloadUrl(string cloudinaryUrl, string originalFileName)
    {
        try
        {
            // Cloudinary URL format: https://res.cloudinary.com/{cloud_name}/raw/upload/v{version}/{path}
            // We need to insert "fl_attachment:{filename}" after "upload/"
            
            var uri = new Uri(cloudinaryUrl);
            var path = uri.AbsolutePath;
            
            // Find the "upload/" part
            var uploadIndex = path.IndexOf("/upload/");
            if (uploadIndex == -1)
            {
                _logger.LogWarning("Could not find '/upload/' in URL: {Url}", cloudinaryUrl);
                return cloudinaryUrl;
            }

            // Encode the filename for URL
            var encodedFileName = Uri.EscapeDataString(originalFileName);
            
            // Insert the fl_attachment transformation
            var beforeUpload = path.Substring(0, uploadIndex + 8); // Include "/upload/"
            var afterUpload = path.Substring(uploadIndex + 8);
            
            // Build the new path with fl_attachment
            var newPath = $"{beforeUpload}fl_attachment:{encodedFileName}/{afterUpload}";
            
            // Reconstruct the full URL
            var downloadUrl = $"{uri.Scheme}://{uri.Host}{newPath}";
            
            return downloadUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting URL to download URL: {Url}", cloudinaryUrl);
            return cloudinaryUrl; // Return original URL if conversion fails
        }
    }

    /// <summary>
    /// Clean filename to make it URL-safe and valid for Cloudinary public_id
    /// </summary>
    private string CleanFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return $"file_{DateTime.UtcNow:yyyyMMddHHmmss}";

        // Replace spaces with underscores
        fileName = fileName.Replace(" ", "_");
        
        // Remove special characters except underscore, hyphen, and dot
        fileName = new string(fileName.Where(c => 
            char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.').ToArray());
        
        // If filename is empty after cleaning, generate a default name
        if (string.IsNullOrEmpty(fileName))
            return $"file_{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        // Limit length to prevent issues
        if (fileName.Length > 200)
            fileName = fileName.Substring(0, 200);
        
        return fileName;
    }
}
