using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Models;

namespace PhotoGallery.Web.Controllers
{
    [AllowAnonymous]
    public class GalleriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public GalleriesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var model = await _db.Galleries
                .OrderByDescending(g => g.CreatedUtc)
                .Take(50) //let me test small for now
                .ToListAsync();

            return View(model);
        }
    }
}