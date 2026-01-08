using Microsoft.AspNetCore.Mvc;

namespace ProSocialApi.Controllers;

[Route("auth")]
public class AuthViewController : Controller
{
    [HttpGet("login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet("register")]
    public IActionResult Register()
    {
        return View();
    }
}
