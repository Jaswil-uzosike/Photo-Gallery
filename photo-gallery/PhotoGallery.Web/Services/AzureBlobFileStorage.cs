using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace PhotoGallery.Web.Services
{
    // Azure Blob implementation. Stores objects at: photogallery/{userId}/{galleryId}/original/{guid}{ext}
    public class AzureBlobFileStorage : IFileStorage
    {
        private readonly BlobServiceClient _svc;
        private readonly string _containerName;

        public AzureBlobFileStorage(IConfiguration cfg)
        {
            var cs = cfg["Storage:Azure:ConnectionString"] ?? throw new InvalidOperationException("Missing Storage:Azure:ConnectionString");
            _svc = new BlobServiceClient(cs);
            _containerName = cfg["Storage:Azure:Container"] ?? "photos";
        }

        public async Task<(string Url, string Key)> SaveOriginalAsync(string userId, int galleryId, IFormFile file, CancellationToken ct = default)
        {
            var container = _svc.GetBlobContainerClient(_containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

            var ext = Path.GetExtension(file.FileName);
            var key = $"{userId}/{galleryId}/original/{Guid.NewGuid():N}{ext}";

            var blob = container.GetBlobClient(key);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            };

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, options, ct);

            // If you use a CDN, replace blob.Uri with your CDN base + key.
            return (blob.Uri.ToString(), key);
        }
    }
}
