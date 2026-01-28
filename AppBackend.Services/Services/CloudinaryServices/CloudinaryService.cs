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
    private readonly HttpClient _httpClient;

    public CloudinaryService(Cloudinary cloudinary, ILogger<CloudinaryService> logger, IHttpClientFactory httpClientFactory)
    {
        _cloudinary = cloudinary;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // Large files may take time
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
            
            // ? FIX: Don't include folder in publicId - Cloudinary adds it automatically
            // Just use the safe filename
            var publicId = safeFileName;

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
                    // ? REMOVED: AccessMode - not supported in CloudinaryDotNet library
                    // Raw files are public by default
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

            _logger.LogInformation("File uploaded successfully. PublicId: {PublicId}, Url: {Url}, IsImage: {IsImage}",
                uploadResult.PublicId, uploadResult.SecureUrl, isImage);

            // ? FIX: Return the SecureUrl as-is from Cloudinary
            // We'll handle download through the API endpoint, not direct browser access
            var secureUrl = uploadResult.SecureUrl?.ToString();

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

    /// <summary>
    /// Download file from Cloudinary URL
    /// </summary>
    public async Task<(byte[] fileData, string fileName, string contentType)?> DownloadFileAsync(string cloudinaryUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(cloudinaryUrl))
            {
                _logger.LogWarning("Download requested with empty URL");
                return null;
            }

            _logger.LogInformation("Downloading file from Cloudinary: {Url}", cloudinaryUrl);

            // Extract public_id and resource type
            var (publicId, resourceType) = ExtractPublicIdAndResourceType(cloudinaryUrl);
            
            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogError("Could not extract public_id from URL: {Url}", cloudinaryUrl);
                return null;
            }

            _logger.LogInformation("Extracted PublicId: {PublicId}, ResourceType: {ResourceType}", publicId, resourceType);

            string downloadUrl;
            
            // For raw files, use Admin API to get fresh download URL with authentication
            if (resourceType == "raw")
            {
                try
                {
                    // Use Admin API GetResource to get authenticated URL
                    var getResourceParams = new GetResourceParams(publicId)
                    {
                        ResourceType = ResourceType.Raw
                    };
                    
                    _logger.LogInformation("Calling Admin API GetResource for: {PublicId}", publicId);
                    var resourceResult = await _cloudinary.GetResourceAsync(getResourceParams);
                    
                    if (resourceResult?.SecureUrl != null)
                    {
                        downloadUrl = resourceResult.SecureUrl;
                        _logger.LogInformation("Got authenticated URL from Admin API: {Url}", downloadUrl);
                    }
                    else
                    {
                        _logger.LogWarning("GetResource returned null, falling back to fl_attachment transformation");
                        // Fallback: add fl_attachment flag to force download
                        downloadUrl = cloudinaryUrl.Replace("/upload/", "/upload/fl_attachment/");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GetResource failed, using fl_attachment transformation");
                    // Fallback: add fl_attachment flag to force download
                    downloadUrl = cloudinaryUrl.Replace("/upload/", "/upload/fl_attachment/");
                }
            }
            else
            {
                // For images, use direct URL with attachment flag
                downloadUrl = cloudinaryUrl.Replace("/upload/", "/upload/fl_attachment/");
            }

            // Download the file
            _logger.LogInformation("Attempting download from: {Url}", downloadUrl);
            var response = await _httpClient.GetAsync(downloadUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Download failed. Status: {StatusCode}, Reason: {Reason}", 
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var fileData = await response.Content.ReadAsByteArrayAsync();
            
            // Extract filename from original URL
            var uri = new Uri(cloudinaryUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            
            // Decode URL-encoded filename
            fileName = Uri.UnescapeDataString(fileName);
            
            if (string.IsNullOrEmpty(fileName))
            {
                var extension = Path.GetExtension(uri.LocalPath);
                fileName = "download" + extension;
            }

            // Get content type from response or infer from extension
            var contentType = response.Content.Headers.ContentType?.MediaType;
            
            if (string.IsNullOrEmpty(contentType))
            {
                var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
                contentType = extension switch
                {
                    ".pdf" => "application/pdf",
                    ".zip" => "application/zip",
                    ".txt" => "text/plain",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".ppt" => "application/vnd.ms-powerpoint",
                    ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                    ".rar" => "application/x-rar-compressed",
                    ".7z" => "application/x-7z-compressed",
                    _ => "application/octet-stream"
                };
            }
            
            _logger.LogInformation("Downloaded {Size} bytes, filename: {FileName}, contentType: {ContentType}", 
                fileData.Length, fileName, contentType);

            return (fileData, fileName, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from Cloudinary: {Url}", cloudinaryUrl);
            return null;
        }
    }

    /// <summary>
    /// Extract public_id and resource type from Cloudinary URL
    /// </summary>
    private (string publicId, string resourceType) ExtractPublicIdAndResourceType(string cloudinaryUrl)
    {
        try
        {
            var uri = new Uri(cloudinaryUrl);
            var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // Cloudinary URL format: 
            // Images: .../image/upload/v{version}/{folder}/{publicId}.{format}
            // Raw files: .../raw/upload/v{version}/{folder}/{publicId}.{format}
            
            // Find resource type (image or raw)
            string resourceType = "image"; // default
            int uploadIndex = -1;
            
            for (int i = 0; i < pathSegments.Length; i++)
            {
                if (pathSegments[i] == "raw")
                {
                    resourceType = "raw";
                }
                
                if (pathSegments[i] == "upload")
                {
                    uploadIndex = i;
                    break;
                }
            }
            
            if (uploadIndex < 0 || uploadIndex + 2 >= pathSegments.Length)
            {
                _logger.LogWarning("Could not find upload index in URL: {Url}", cloudinaryUrl);
                return (string.Empty, resourceType);
            }
            
            // Get segments after version number (skip "v{version}")
            var publicIdParts = pathSegments.Skip(uploadIndex + 2).ToArray();
            var publicIdWithExtension = string.Join("/", publicIdParts);
            
            // Remove file extension for public_id
            var lastDotIndex = publicIdWithExtension.LastIndexOf('.');
            var publicId = lastDotIndex > 0 
                ? publicIdWithExtension.Substring(0, lastDotIndex) 
                : publicIdWithExtension;
            
            _logger.LogInformation("Extracted from URL - PublicId: {PublicId}, ResourceType: {ResourceType}", 
                publicId, resourceType);
            
            return (publicId, resourceType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting public_id from URL: {Url}", cloudinaryUrl);
            return (string.Empty, "image");
        }
    }
}
