/* Builds the vertical, single-photo “feed” for the homepage. We grab a
 recent sample, shuffle it, page it, and return a partial view for
infinite scroll. Image links prefer time-limited URLs when possible.*/

using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Web.Data;
using PhotoGallery.Web.Services;

namespace PhotoGallery.Web.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileStorage _storage;

        public HomeController(ApplicationDbContext db, IFileStorage storage)
        {
            _db = db;
            _storage = storage;
        }

        
        private const int PageSize = 24;
        private const int SampleSize = 500;   

        public async Task<IActionResult> Index()
        {
            var vm = await BuildFeedPageAsync(page: 1);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Feed(int page = 2)
        {
            if (page < 1) page = 1;
            var vm = await BuildFeedPageAsync(page);

            Response.Headers["X-HasMore"] = vm.HasMore ? "1" : "0";
            return PartialView("_FeedItems", vm.Photos);
        }

        //We pull a capped sample of recent photos, shuffle them server-side, and then paginate that list

        private async Task<HomeFeedVM> BuildFeedPageAsync(int page)
        {
            if (page < 1) page = 1;

            var sample = await (
                from p in _db.Photos
                join g in _db.Galleries on p.GalleryId equals g.Id
                join u in _db.Users on g.OwnerId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                orderby p.CreatedUtc descending
                select new
                {
                    PhotoId = p.Id,
                    p.GalleryId,
                    GalleryTitle = g.Title,
                    p.ThumbStorageKey,
                    p.ThumbPath,
                    p.StorageKey,
                    p.OriginalPath,
                    p.CreatedUtc,
                    OwnerName =
                        (((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim() != ""
                            ? ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim()
                            : (u.Email ?? "Unknown"))
                })
                .Take(SampleSize)
                .ToListAsync();

            var rng = new Random();
            var shuffled = sample.OrderBy(_ => rng.Next()).ToList();

            var skip = (page - 1) * PageSize;
            var pageItems = shuffled.Skip(skip).Take(PageSize).ToList();
            var hasMore = shuffled.Count > skip + PageSize;

            var photos = pageItems.Select(x => new HomeFeedItemVM
            {
                PhotoId = x.PhotoId,
                GalleryId = x.GalleryId,
                GalleryTitle = x.GalleryTitle ?? "Untitled gallery",
                ThumbUrl =
                    !string.IsNullOrWhiteSpace(x.ThumbStorageKey) ? _storage.GetReadUrl(x.ThumbStorageKey, TimeSpan.FromHours(1)) :
                    !string.IsNullOrWhiteSpace(x.ThumbPath) ? x.ThumbPath :
                    !string.IsNullOrWhiteSpace(x.OriginalPath) ? x.OriginalPath :
                    "/img/placeholder-photo.svg",
                FullUrl =
                    !string.IsNullOrWhiteSpace(x.StorageKey) ? _storage.GetReadUrl(x.StorageKey, TimeSpan.FromHours(1)) :
                    !string.IsNullOrWhiteSpace(x.OriginalPath) ? x.OriginalPath :
                    "/img/placeholder-photo.svg",
                Caption = $"by {x.OwnerName} • {x.CreatedUtc.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture)}"
            }).ToList();

            return new HomeFeedVM
            {
                Page = page,
                PageSize = PageSize,
                HasMore = hasMore,
                Photos = photos
            };
        }
    }

    public class HomeFeedVM
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
        public List<HomeFeedItemVM> Photos { get; set; } = new();
    }

    public class HomeFeedItemVM
    {
        public int PhotoId { get; set; }
        public int GalleryId { get; set; }
        public string GalleryTitle { get; set; } = "Untitled gallery"; 
        public string ThumbUrl { get; set; } = "/img/placeholder-photo.svg";
        public string FullUrl { get; set; } = "/img/placeholder-photo.svg";
        public string Caption { get; set; } = "Photo";
    }
}
