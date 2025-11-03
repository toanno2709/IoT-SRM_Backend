using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels.Commons;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(Cloudinary cloudinary)
    {
        _cloudinary = cloudinary;
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

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(request.File.FileName, request.File.OpenReadStream()),
            Folder = request.Folder
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        var response = new CloudinaryUploadResponseDto
        {
            PublicId = uploadResult.PublicId,
            Url = uploadResult.Url?.ToString(),
            SecureUrl = uploadResult.SecureUrl?.ToString(),
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

    public async Task<ResultModel<CloudinaryDeleteResponseDto>> DeleteAsync(string publicId)
    {
        var delParams = new DeletionParams(publicId);
        var delResult = await _cloudinary.DestroyAsync(delParams);

        var response = new CloudinaryDeleteResponseDto
        {
            PublicId = publicId,
            Status = delResult.Result
        };

        return new ResultModel<CloudinaryDeleteResponseDto>
        {
            IsSuccess = delResult.Result == "ok",
            StatusCode = delResult.Result == "ok" ? 200 : 400,
            Message = delResult.Result == "ok" ? "Delete successful" : "Delete failed",
            Data = response
        };
    }

    // Helper method for simpler file upload
    public async Task<CloudinaryUploadResponseDto?> UploadFileAsync(IFormFile file, string? folder = null)
    {
        var request = new CloudinaryUploadRequestDto
        {
            File = file,
            Folder = folder ?? "SWP391/submissions"
        };

        var result = await UploadAsync(request);
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

                var result = await DeleteAsync(publicId);
                return result.IsSuccess;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
