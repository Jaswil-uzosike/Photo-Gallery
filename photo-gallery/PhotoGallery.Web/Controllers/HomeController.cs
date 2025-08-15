using System.Diagnostics;
using Microsoft.AspNetCore.Identity;                 
using Microsoft.AspNetCore.Mvc;
using PhotoGallery.Web.Models;                       

namespace PhotoGallery.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;


    public HomeController(
        ILogger<HomeController> logger,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> Index()
    {
        if (_signInManager.IsSignedIn(User))
        {
            var me = await _userManager.GetUserAsync(User);

            var fullName = (me is not null && (!string.IsNullOrWhiteSpace(me.FirstName) || !string.IsNullOrWhiteSpace(me.LastName)))
                ? $"{me!.FirstName} {me!.LastName}".Trim()
                : me?.Email ?? User.Identity?.Name ?? "User";

            ViewBag.FullName = fullName;
        }

        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}