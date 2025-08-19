using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Models;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Controllers
{
    [AllowAnonymous]
    public class GalleriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorage _storage;

        public GalleriesController(ApplicationDbContext db, IFileStorage storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<IActionResult> Index(string? q)
        {
            q = q?.Trim();
            var qLower = q?.ToLower();

            var query = from g in _db.Galleries
                    join u in _db.Users on g.OwnerId equals u.Id into gj
                    from u in gj.DefaultIfEmpty()
                orderby g.CreatedUtc descending
                    select new{
                            g.Id,
                            g.Title,
                            g.Description,
                            g.CreatedUtc,
                            OwnerName = (((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim() != ""
                                            ? ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim()
                                            : (u.Email ?? "Unknown")),
                            PhotoCount = _db.Photos.Count(p => p.GalleryId == g.Id)
                        };

            // ADDED: simple case-insensitive search across title/description/owner
            if (!string.IsNullOrEmpty(qLower))
            {
                query = query.Where(x =>
                    (x.Title ?? "").ToLower().Contains(qLower) ||
                    (x.Description ?? "").ToLower().Contains(qLower) ||
                    (x.OwnerName ?? "").ToLower().Contains(qLower));
            }

            var items = await query
                .Select(x => new PublicGalleryListItemVM
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    CreatedUtc = x.CreatedUtc,
                    OwnerName = x.OwnerName,
                    PhotoCount = x.PhotoCount
                })
                .ToListAsync();

            // ADDED: pass query to view for echo/clear link
            ViewBag.Query = q ?? string.Empty; // ADDED

            return View(items);
        }
        public async Task<IActionResult> Show(int id)
        {
            var g = await _db.Galleries.FirstOrDefaultAsync(x => x.Id == id);
            if (g == null) return NotFound();

            var owner = await _db.Users.Where(u => u.Id == g.OwnerId)
                .Select(u => new { u.FirstName, u.LastName, u.Email })
                .FirstOrDefaultAsync();

            var ownerName = owner is null
                ? "Unknown"
                : (string.IsNullOrWhiteSpace($"{owner.FirstName} {owner.LastName}".Trim())
                    ? owner.Email
                    : $"{owner.FirstName} {owner.LastName}".Trim());

            var photos = await _db.Photos
                .Where(p => p.GalleryId == id)
                .OrderByDescending(p => p.CreatedUtc)
                .ToListAsync();

            var photoVMs = photos.Select(p => new PublicPhotoVM
            {
                Id = p.Id,
                ThumbUrl =
                    !string.IsNullOrWhiteSpace(p.ThumbStorageKey) ? _storage.GetReadUrl(p.ThumbStorageKey, TimeSpan.FromHours(1)) :
                    !string.IsNullOrWhiteSpace(p.ThumbPath) ? p.ThumbPath :
                    !string.IsNullOrWhiteSpace(p.OriginalPath) ? p.OriginalPath :
                    "/img/placeholder-photo.svg",
                FullUrl =
                    !string.IsNullOrWhiteSpace(p.StorageKey) ? _storage.GetReadUrl(p.StorageKey, TimeSpan.FromHours(1)) :
                    !string.IsNullOrWhiteSpace(p.OriginalPath) ? p.OriginalPath :
                    "/img/placeholder-photo.svg"
            }).ToList();

            var vm = new PublicGalleryVM
            {
                Id = g.Id,
                Title = g.Title,
                Description = g.Description,
                OwnerName = ownerName,
                CreatedDateText = g.CreatedUtc.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture),
                Photos = photoVMs
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id, int photoId)
        {
            var photo = await _db.Photos
                .Where(p => p.Id == photoId && p.GalleryId == id)
                .FirstOrDefaultAsync();

            if (photo == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(photo.StorageKey))
            {
                var (stream, contentType) = await _storage.OpenReadAsync(photo.StorageKey, HttpContext.RequestAborted);

                var ext = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png"  => ".png",
                    "image/gif"  => ".gif",
                    "image/webp" => ".webp",
                    _ => ""
                };
                var fileName = $"photo-{photo.Id}{ext}";
                return File(stream, contentType, fileName);
            }

            if (!string.IsNullOrWhiteSpace(photo.OriginalPath))
            {
                return Redirect(photo.OriginalPath);
            }

            return NotFound();
        }
    }

    public class PublicGalleryListItemVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string OwnerName { get; set; } = "Unknown";
        public int PhotoCount { get; set; }
    }

    public class PublicGalleryVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string OwnerName { get; set; } = "Unknown";
        public string CreatedDateText { get; set; } = "";
        public List<PublicPhotoVM> Photos { get; set; } = new();
    }

    public class PublicPhotoVM
    {
        public int Id { get; set; }
        public string ThumbUrl { get; set; } = "/img/placeholder-photo.svg";
        public string FullUrl { get; set; } = "/img/placeholder-photo.svg";
    }
    
}