using Microsoft.AspNetCore.Http;

namespace PhotoGallery.Web.Services
{
    public interface IFileStorage
    {
        Task<(string Url, string Key)> SaveOriginalAsync(string userId, int galleryId, IFormFile file, CancellationToken ct = default);
    }
}
