
// Galleries page inside the Profile section.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Models;

namespace PhotoGallery.Web.Areas.Identity.Pages.Account.Manage
{
    [Authorize] 
    public class GalleriesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public GalleriesModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public IList<Gallery> MyGalleries { get; private set; } = new List<Gallery>();

        [BindProperty]
        public CreateGalleryInput Input { get; set; } = new();

        public class CreateGalleryInput
        {
            [Required, StringLength(100)]
            public string Title { get; set; } = string.Empty;

            [StringLength(500)]
            public string? Description { get; set; }
        }

        private async Task LoadAsync()
        {
            var uid = _userManager.GetUserId(User)!;
            MyGalleries = await _db.Galleries
                .Where(g => g.OwnerId == uid)
                .OrderByDescending(g => g.CreatedUtc)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var uid = _userManager.GetUserId(User)!;

            if (!ModelState.IsValid)
            {
                await LoadAsync();
                return Page();
            }

            var gallery = new Gallery
            {
                Title = Input.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
                OwnerId = uid,
                CreatedUtc = DateTime.UtcNow
            };

            _db.Galleries.Add(gallery);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Gallery created.";
            return RedirectToPage(); 
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var uid = _userManager.GetUserId(User)!;
            var gallery = await _db.Galleries.FindAsync(id);
            if (gallery is null) return NotFound();

            if (gallery.OwnerId != uid) return Forbid();

            _db.Galleries.Remove(gallery);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Gallery deleted.";
            return RedirectToPage();
        }
    }
}
