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
    public class EditGalleryModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;

        public EditGalleryModel(ApplicationDbContext db, UserManager<ApplicationUser> um)
        { _db = db; _um = um; }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public int GalleryId { get; private set; }
        public class InputModel
        {
            [Required, StringLength(100)]
            public string Title { get; set; } = string.Empty;
            [StringLength(500)]
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var uid = _um.GetUserId(User)!;
            var g = await _db.Galleries.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == uid);
            if (g is null) return NotFound();

            GalleryId = g.Id;
            Input = new InputModel { Title = g.Title, Description = g.Description };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                GalleryId = id;
                return Page();
            }

            var uid = _um.GetUserId(User)!;
            var g = await _db.Galleries.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == uid);
            if (g is null) return NotFound();

            g.Title = Input.Title.Trim();
            g.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();

            await _db.SaveChangesAsync();
            TempData["StatusMessage"] = "Gallery updated.";
            return RedirectToPage("./Gallery", new { id });
        }
    }
}
