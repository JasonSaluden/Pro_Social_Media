using Microsoft.AspNetCore.Mvc;

namespace ProSocialApi.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Network()
    {
        return View();
    }

    public IActionResult Profile()
    {
        return View();
    }
}
