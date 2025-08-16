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
    public class GalleryModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public GalleryModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public Gallery? Gallery { get; private set; }

        public IList<Photo> Photos { get; private set; } = new List<Photo>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var uid = _userManager.GetUserId(User)!;

            Gallery = await _db.Galleries
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id && g.OwnerId == uid);

            if (Gallery is null)
                return NotFound();

            Photos = await _db.Photos
                .AsNoTracking()
                .Where(p => p.GalleryId == id)
                .OrderByDescending(p => p.CreatedUtc)
                .ToListAsync();

            return Page();
        }
    }
}
