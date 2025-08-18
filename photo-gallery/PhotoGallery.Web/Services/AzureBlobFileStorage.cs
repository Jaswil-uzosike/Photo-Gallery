using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Sas;

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

        public string GetReadUrl(string key, TimeSpan ttl)
        {
            var container = _svc.GetBlobContainerClient(_containerName);
            var blob = container.GetBlobClient(key);
            return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(ttl)).ToString();
        }

        public async Task<(Stream Stream, string ContentType)> OpenReadAsync(string key, CancellationToken ct = default)
        {
            var container = _svc.GetBlobContainerClient(_containerName);
            var blob = container.GetBlobClient(key);
            var resp = await blob.DownloadStreamingAsync(cancellationToken: ct);
            var contentType = resp.Value.Details.ContentType ?? "application/octet-stream";
            return (resp.Value.Content, contentType);
        }

        public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
        {
            var container = _svc.GetBlobContainerClient(_containerName);
            var blob = container.GetBlobClient(key);
            var result = await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
            return result.Value;
        }
        public async Task<(string Url, string Key)> SaveThumbnailAsync(string userId, int galleryId, Stream stream, string contentType, CancellationToken ct = default)
        {
            var container = _svc.GetBlobContainerClient(_containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            // use jpg for thumbs regardless of source, keeps things small
            var key = $"{userId}/{galleryId}/thumbs/{Guid.NewGuid():N}.jpg";
            var blob = container.GetBlobClient(key);

            var opts = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
            };

            // ensure stream is at start
            if (stream.CanSeek) stream.Position = 0;
            await blob.UploadAsync(stream, opts, ct);

            return (blob.Uri.ToString(), key);
        }
    }
}
