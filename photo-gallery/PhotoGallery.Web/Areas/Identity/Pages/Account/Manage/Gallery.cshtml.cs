using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Models;
using PhotoGallery.Web.Services;

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

                photosToAdd.Add(new Photo
                {
                    GalleryId   = id,
                    OriginalPath    = url,       
                    ThumbPath   = null,       
                    ContentType = file.ContentType,
                    SizeBytes   = file.Length,
                    CreatedUtc  = now
                    // If you have StorageKey/Provider columns, set them here:
                    // StorageKey = key, StorageProvider = "AzureBlob"
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
    }
}