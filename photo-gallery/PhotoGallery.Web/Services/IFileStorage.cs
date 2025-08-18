using Microsoft.AspNetCore.Http;

namespace PhotoGallery.Web.Services
{
    public interface IFileStorage
    {
        Task<(string Url, string Key)> SaveOriginalAsync(string userId, int galleryId, IFormFile file, CancellationToken ct = default);
        Task<(string Url, string Key)> SaveThumbnailAsync(string userId, int galleryId, Stream stream, string contentType, CancellationToken ct = default);
        string GetReadUrl(string key, TimeSpan ttl);
        Task<(Stream Stream, string ContentType)> OpenReadAsync(string key, CancellationToken ct = default);
        Task<bool> DeleteAsync(string key, CancellationToken ct = default);
    }
}
