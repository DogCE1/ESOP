//Deliverable 3: Whole file
using ESOP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace ESOP.Controllers;

public class AccessController : Controller
{
    //There will be only one system admin, so a user-management system is not needed
    //The credentials come from the "Admin" section in appsettings.json, only a salted hash of the password is stored, never the password itself
    private readonly string _adminUsername;
    private readonly string _adminPasswordHash;
    private readonly PasswordHasher<SystemAdmin> _hasher = new();

    public AccessController(IConfiguration config)
    {
        _adminUsername = config["Admin:Username"] ?? throw new InvalidOperationException("Admin:Username is missing from appsettings.json");
        _adminPasswordHash = config["Admin:PasswordHash"] ?? throw new InvalidOperationException("Admin:PasswordHash is missing from appsettings.json");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(); //Display empty login form
    }

    [HttpPost]
    [ValidateAntiForgeryToken] //Rejects posts that did not come from our own form (CSRF protection)
    public async Task<IActionResult> Login(SystemAdmin login)
    {
        //Check if user is admin: hash the entered password and compare it to the stored hash
        var passwordOk = !string.IsNullOrEmpty(login.Password)
            && _hasher.VerifyHashedPassword(login, _adminPasswordHash, login.Password) != PasswordVerificationResult.Failed;

        if (login.Username != _adminUsername || !passwordOk)
        {
            ModelState.AddModelError("","Invalid username or password");
            return View(login); //Show the form again with the error
        }

        //Claims to be admin
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, login.Username),
        };
        //Builds the auth identity and signs in via a cookie
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        
        return RedirectToAction("Index", "Staff");
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        //Signs the user out and clears the cookie auth
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    //Target of UseExceptionHandler in Program.cs, shows a generic error page outside Development
    public IActionResult Error()
    {
        return View();
    }
}