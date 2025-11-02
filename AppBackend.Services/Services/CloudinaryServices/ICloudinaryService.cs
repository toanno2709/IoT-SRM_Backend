using AppBackend.BusinessObjects.Dtos;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services;

public interface ICloudinaryService
{
    Task<ResultModel<CloudinaryUploadResponseDto>> UploadAsync(CloudinaryUploadRequestDto request);
    Task<ResultModel<CloudinaryDeleteResponseDto>> DeleteAsync(string publicId);
    
    // Helper methods for easier usage
    Task<CloudinaryUploadResponseDto?> UploadFileAsync(IFormFile file, string? folder = null);
    Task<bool> DeleteFileAsync(string fileUrl);
}