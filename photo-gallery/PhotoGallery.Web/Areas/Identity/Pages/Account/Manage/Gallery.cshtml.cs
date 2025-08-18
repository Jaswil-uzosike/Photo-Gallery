using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Models;
using PhotoGallery.Web.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace PhotoGallery.Web.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class GalleryModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileStorage _storage;
        private readonly IWebHostEnvironment _env;

        public GalleryModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, IFileStorage storage)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
            _storage = storage;
        }

        [BindProperty]
        public IFormFileCollection? Uploads { get; set; }
        [BindProperty]
        public string? EditedImageData { get; set; }  
        [BindProperty]
        public string? EditedFileName { get; set; }   
        public Gallery? Gallery { get; private set; }
        public IList<Photo> Photos { get; private set; } = new List<Photo>();

        private async Task<bool> LoadAsync(int id)
        {
            var uid = _userManager.GetUserId(User)!;

            Gallery = await _db.Galleries.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == uid);

            if (Gallery is null) return false;

            Photos = await _db.Photos.AsNoTracking()
                .Where(p => p.GalleryId == id)
                .OrderByDescending(p => p.CreatedUtc)
                .ToListAsync();

            return true;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!await LoadAsync(id)) return NotFound();
            return Page();
        }

        //Upload
        private const long MaxFileBytes = 20 * 1024 * 1024; // 20 MB per file

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };

        public async Task<IActionResult> OnPostUploadAsync(int id)
        {
            if (!await LoadAsync(id)) return NotFound();

            if (Uploads is null || Uploads.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please choose at least one image.");
                return Page();
            }

            var now = DateTime.UtcNow;
            var uid = _userManager.GetUserId(User)!;
            var photosToAdd = new List<Photo>();

            foreach (var file in Uploads)
            {
                if (file.Length == 0) continue;

                if (file.Length > MaxFileBytes)
                {
                    ModelState.AddModelError(string.Empty, $"{file.FileName} is too large (>{MaxFileBytes / (1024 * 1024)} MB).");
                    continue;
                }

                if (!AllowedContentTypes.Contains(file.ContentType))
                {
                    ModelState.AddModelError(string.Empty, $"{file.FileName}: unsupported type ({file.ContentType}).");
                    continue;
                }

                var (url, key) = await _storage.SaveOriginalAsync(uid, id, file, HttpContext.RequestAborted);
                string thumbUrl = string.Empty;
                string thumbKey = string.Empty;

                await using (var src = file.OpenReadStream())
                using (var image = await Image.LoadAsync(src, HttpContext.RequestAborted))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(600, 600)
                    }));

                    await using var ms = new MemoryStream();
                    var encoder = new JpegEncoder { Quality = 85 };
                    await image.SaveAsJpegAsync(ms, encoder, HttpContext.RequestAborted);
                    ms.Position = 0;
                    (thumbUrl, thumbKey) = await _storage.SaveThumbnailAsync(uid, id, ms, "image/jpeg", HttpContext.RequestAborted);
                }

                photosToAdd.Add(new Photo
                {
                    GalleryId = id,
                    OriginalPath = url,
                    ThumbPath = thumbUrl,                
                    ThumbStorageKey = thumbKey,           
                    ContentType = file.ContentType,
                    SizeBytes = file.Length,
                    CreatedUtc = now,
                    StorageKey = key,          //Turns out I did need it
                    StorageProvider = "AzureBlob"
                });
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (photosToAdd.Count > 0)
            {
                _db.Photos.AddRange(photosToAdd);
                await _db.SaveChangesAsync();
                TempData["StatusMessage"] = $"{photosToAdd.Count} photo(s) uploaded.";
            }
            else
            {
                TempData["StatusMessage"] = "No photos were uploaded.";
            }

            return RedirectToPage(new { id });
        }
        
        //Edit for crop\rotate
        public async Task<IActionResult> OnPostUploadEditedAsync(int id)
        {
            if (!await LoadAsync(id)) return NotFound();

            if (string.IsNullOrWhiteSpace(EditedImageData))
            {
                ModelState.AddModelError(string.Empty, "No edited image data received.");
                return Page();
            }

            (string contentType, byte[] bytes) = ParseDataUrl(EditedImageData);

            if (bytes.LongLength > MaxFileBytes)
            {
                ModelState.AddModelError(string.Empty, $"Edited image too large (> {MaxFileBytes / (1024 * 1024)} MB).");
                return Page();
            }

            var ms = new MemoryStream(bytes);
            var fileName = !string.IsNullOrWhiteSpace(EditedFileName)
                ? EditedFileName
                : $"edited-{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";

            var formFile = new FormFile(ms, 0, ms.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            var uid = _userManager.GetUserId(User)!;
            var (url, key) = await _storage.SaveOriginalAsync(uid, id, formFile, HttpContext.RequestAborted);
            string thumbUrl = string.Empty;
            string thumbKey = string.Empty;

            await using (var msIn = new MemoryStream(bytes))
            using (var image = await Image.LoadAsync(msIn, HttpContext.RequestAborted))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(600, 600)
                }));

                await using var msOut = new MemoryStream();
                var encoder = new JpegEncoder { Quality = 85 };
                await image.SaveAsJpegAsync(msOut, encoder, HttpContext.RequestAborted);
                msOut.Position = 0;

                (thumbUrl, thumbKey) = await _storage.SaveThumbnailAsync(uid, id, msOut, "image/jpeg", HttpContext.RequestAborted);
            }

            var photo = new Photo
            {
                GalleryId = id,
                OriginalPath = url,
                ThumbPath = thumbUrl,                 
                ThumbStorageKey = thumbKey,           
                ContentType = contentType,
                SizeBytes = bytes.LongLength,
                CreatedUtc = DateTime.UtcNow,
                StorageKey = key,
                StorageProvider = "AzureBlob"
            };

            _db.Photos.Add(photo);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Edited photo uploaded.";
            return RedirectToPage(new { id });
        }

        private static (string ContentType, byte[] Bytes) ParseDataUrl(string dataUrl)
        {
            var comma = dataUrl.IndexOf(',');
            if (comma < 0) throw new InvalidOperationException("Invalid data URL.");

            var header = dataUrl.Substring(0, comma);
            var payload = dataUrl.Substring(comma + 1);

            var match = Regex.Match(header, @"data:(?<ct>[^;]+);base64", RegexOptions.IgnoreCase);
            var ct = match.Success ? match.Groups["ct"].Value : "image/jpeg";

            byte[] bytes = Convert.FromBase64String(payload);
            return (ct, bytes);
        }

        // Download
        public async Task<IActionResult> OnGetDownloadAsync(int id, int photoId)
        {
            var uid = _userManager.GetUserId(User)!;

            var photo = await _db.Photos
                .Include(p => p.Gallery)
                .Where(p => p.Id == photoId
                        && p.GalleryId == id
                        && p.Gallery != null
                        && p.Gallery.OwnerId == uid)
                .FirstOrDefaultAsync();

            if (photo is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(photo.StorageKey))
            {
                (Stream stream, string contentType) = await _storage.OpenReadAsync(photo.StorageKey, HttpContext.RequestAborted);

                string ext = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ""
                };

                string fileName = $"photo-{photo.Id}{ext}";
                return File(stream, contentType, fileName);
            }
            else if (!string.IsNullOrWhiteSpace(photo.OriginalPath))
            {
                return Redirect(photo.OriginalPath);
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int photoId)
        {
            var uid = _userManager.GetUserId(User)!;

            var photo = await _db.Photos
                .Include(p => p.Gallery)
                .Where(p => p.Id == photoId
                        && p.GalleryId == id
                        && p.Gallery != null
                        && p.Gallery.OwnerId == uid)
                .FirstOrDefaultAsync();

            if (photo is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(photo.StorageKey))
            {
                await _storage.DeleteAsync(photo.StorageKey, HttpContext.RequestAborted);
            }

            _db.Photos.Remove(photo);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Photo deleted.";
            return RedirectToPage(new { id });
        }

    }
}
