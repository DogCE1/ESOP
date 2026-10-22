using ESOP.Controllers;
using ESOP.Models;
using ESOP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ESOP.Tests;

public class StaffControllerTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private StaffController NewController()
    {
        return new StaffController(new StaffService(_db.CreateContext()))
        {
            //TempData normally comes from the web pipeline, so the test supplies one that just keeps values in memory
            TempData = new TempDataDictionary(new DefaultHttpContext(), new InMemoryTempDataProvider()),
        };
    }

    private static StaffMember NewStaff(string name, string email) =>
        new() { FullName = name, Email = email, Position = "Developer", Unit = "IT" };

    [Fact]
    public void Index_ReturnsViewWithAllStaff()
    {
        NewController().Create(NewStaff("Alice", "alice@x.com"));

        var view = Assert.IsType<ViewResult>(NewController().Index());

        var model = Assert.IsType<List<StaffMember>>(view.Model);
        Assert.Equal("Alice", Assert.Single(model).FullName);
    }

    [Fact]
    public void Create_ValidStaffMember_SetsSuccessMessageAndRedirects()
    {
        var controller = NewController();

        var result = controller.Create(NewStaff("Alice", "alice@x.com"));

        Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.Contains("Successfully added", controller.TempData["SMessage"] as string);
        Assert.Null(controller.TempData["EMessage"]);
    }

    [Fact]
    public void Create_InvalidModel_SetsErrorMessageAndSavesNothing()
    {
        var controller = NewController();
        controller.ModelState.AddModelError("Email", "The Email Address field is not a valid e-mail address.");

        controller.Create(NewStaff("Alice", "not-an-email"));

        Assert.Contains("not a valid e-mail address", controller.TempData["EMessage"] as string);
        Assert.Empty(new StaffService(_db.CreateContext()).GetAllStaff());
    }

    [Fact]
    public void Create_DuplicateEmail_SetsErrorMessage()
    {
        NewController().Create(NewStaff("Bob", "bob@x.com"));
        var controller = NewController();

        controller.Create(NewStaff("Robert", "bob@x.com"));

        Assert.Contains("already exists", controller.TempData["EMessage"] as string);
    }

    [Fact]
    public void Edit_UnknownId_SetsErrorMessage()
    {
        var controller = NewController();
        var staff = NewStaff("Nobody", "no@x.com");
        staff.StaffId = 42;

        controller.Edit(staff);

        Assert.Equal("StaffId not found", controller.TempData["EMessage"]);
    }

    [Fact]
    public void Delete_ExistingStaffMember_SetsSuccessMessage()
    {
        NewController().Create(NewStaff("Alice", "alice@x.com"));
        var controller = NewController();

        controller.Delete(1);

        Assert.Equal("Successfully deleted staff member: 1, Alice", controller.TempData["SMessage"]);
    }

    [Fact]
    public void Delete_UnknownId_SetsErrorMessage()
    {
        var controller = NewController();

        controller.Delete(42);

        Assert.Equal("StaffId not found", controller.TempData["EMessage"]);
    }

    private class InMemoryTempDataProvider : ITempDataProvider
    {
        private IDictionary<string, object> _data = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _data = values;
    }
}
