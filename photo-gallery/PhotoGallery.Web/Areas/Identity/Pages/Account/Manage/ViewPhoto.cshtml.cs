// Loads one photofrom your gallery, checks ownership, and picks
// the best URL to show (signed storage URL if we have a key,
// otherwise the original public path). Thenit renders the page.

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
    public class ViewPhotoModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;
        private readonly IFileStorage _storage;

        public ViewPhotoModel(ApplicationDbContext db, UserManager<ApplicationUser> um, IFileStorage storage)
        {
            _db = db; _um = um; _storage = storage;
        }

        public Photo? Photo { get; private set; }
        public string ImageUrl { get; private set; } = "/img/placeholder-photo.svg";

        public async Task<IActionResult> OnGetAsync(int id, int photoId)
        {
            var uid = _um.GetUserId(User)!;

            Photo = await _db.Photos
                .Include(p => p.Gallery!)
                .Where(p => p.Id == photoId && p.GalleryId == id && p.Gallery.OwnerId == uid)
                .FirstOrDefaultAsync();

            if (Photo == null) return NotFound();

            /* If we stored this photo with a StorageKey, we ask the storage
            service for a temporary (like 1 hour) signed URL. That way
            the file stays locked down, but the page can still display it. */


            if (!string.IsNullOrWhiteSpace(Photo.StorageKey))
            {
                ImageUrl = _storage.GetReadUrl(Photo.StorageKey, TimeSpan.FromHours(1));
            }
            else if (!string.IsNullOrWhiteSpace(Photo.OriginalPath))
            {
                ImageUrl = Photo.OriginalPath;
            }

            return Page();
        }
    }
}
