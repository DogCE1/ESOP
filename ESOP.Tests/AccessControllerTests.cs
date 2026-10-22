using ESOP.Controllers;
using ESOP.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ESOP.Tests;

public class AccessControllerTests
{
    private const string TestPassword = "correct-horse";

    //Builds the controller with test credentials (not the real ones from appsettings.json)
    //and just enough of the web pipeline for cookie sign-in to work
    private static AccessController NewController()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Username"] = "testadmin",
                ["Admin:PasswordHash"] = new PasswordHasher<SystemAdmin>().HashPassword(new SystemAdmin(), TestPassword),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllersWithViews(); //View() and RedirectToAction() look up TempData and URL services
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

        return new AccessController(config)
        {
            ControllerContext = new ControllerContext(new ActionContext(
                new DefaultHttpContext { RequestServices = services.BuildServiceProvider() },
                new RouteData(),
                new ControllerActionDescriptor())),
        };
    }

    [Fact]
    public async Task Login_CorrectCredentials_SignsInAndRedirectsToStaff()
    {
        var controller = NewController();

        var result = await controller.Login(new SystemAdmin { Username = "testadmin", Password = TestPassword });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Staff", redirect.ControllerName);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Contains(".AspNetCore.Cookies", controller.Response.Headers.SetCookie.ToString()); //The login cookie was issued
    }

    [Theory]
    [InlineData("testadmin", "wrong-password")]
    [InlineData("someoneelse", TestPassword)]
    [InlineData("testadmin", "")]
    [InlineData("TESTADMIN", TestPassword)] //Username is case-sensitive
    public async Task Login_WrongCredentials_ShowsErrorAndDoesNotSignIn(string username, string password)
    {
        var controller = NewController();

        var result = await controller.Login(new SystemAdmin { Username = username, Password = password });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal("Invalid username or password", controller.ModelState[""]!.Errors.Single().ErrorMessage);
        Assert.Equal(0, controller.Response.Headers.SetCookie.Count); //No login cookie issued
    }

    [Fact]
    public void Constructor_MissingAdminConfig_ThrowsClearError()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new AccessController(emptyConfig));

        Assert.Contains("Admin:Username", ex.Message);
    }
}
