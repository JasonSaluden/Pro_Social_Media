using Microsoft.AspNetCore.Mvc;

namespace ProSocialApi.Controllers;

public class AuthViewController : Controller
{
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }
}
