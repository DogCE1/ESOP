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

        var view = Assert.IsType<ViewResult>(NewController().Index(null, null, null));

        var model = Assert.IsType<StaffIndexViewModel>(view.Model);
        Assert.Equal("Alice", Assert.Single(model.Staff).FullName);
        Assert.Null(model.Search);
        Assert.Equal("id", model.Sort);
        Assert.False(model.Descending);
        Assert.Equal(1, model.TotalCount);
    }

    [Fact]
    public void Index_WithSearchAndSort_PassesThemToTheView()
    {
        NewController().Create(NewStaff("Alice", "alice@x.com"));
        NewController().Create(NewStaff("Bob", "bob@x.com"));

        var view = Assert.IsType<ViewResult>(NewController().Index("  bob ", "name", "desc"));

        var model = Assert.IsType<StaffIndexViewModel>(view.Model);
        Assert.Equal("Bob", Assert.Single(model.Staff).FullName);
        Assert.Equal("bob", model.Search); //Trimmed
        Assert.Equal("name", model.Sort);
        Assert.True(model.Descending);
        Assert.Equal(2, model.TotalCount); //Total is everyone, not just the matches
    }

    [Theory]
    [InlineData("password")] //Not a column
    [InlineData("")]
    public void Index_UnknownSort_FallsBackToId(string sort)
    {
        var view = Assert.IsType<ViewResult>(NewController().Index(null, sort, null));

        Assert.Equal("id", Assert.IsType<StaffIndexViewModel>(view.Model).Sort);
    }

    [Fact]
    public void Index_SortIsCaseInsensitive()
    {
        var view = Assert.IsType<ViewResult>(NewController().Index(null, "NAME", null));

        Assert.Equal("name", Assert.IsType<StaffIndexViewModel>(view.Model).Sort);
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

    // ---------- Remembering search and sort after Create/Edit/Delete ----------

    //Checks the redirect goes back to Index with the same search/sort the user had
    private static void AssertBackToSameView(IActionResult result, string? search, string? sort, string? dir)
    {
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(search, redirect.RouteValues?["search"]);
        Assert.Equal(sort, redirect.RouteValues?["sort"]);
        Assert.Equal(dir, redirect.RouteValues?["dir"]);
    }

    [Fact]
    public void Create_KeepsSearchAndSort()
    {
        var result = NewController().Create(NewStaff("Alice", "alice@x.com"), "dev", "name", "desc");

        AssertBackToSameView(result, "dev", "name", "desc");
    }

    [Fact]
    public void Create_Failure_KeepsSearchAndSort()
    {
        NewController().Create(NewStaff("Bob", "bob@x.com"));

        var result = NewController().Create(NewStaff("Robert", "bob@x.com"), "dev", "unit", "asc"); //Duplicate email

        AssertBackToSameView(result, "dev", "unit", "asc");
    }

    [Fact]
    public void Create_InvalidModel_KeepsSearchAndSort()
    {
        var controller = NewController();
        controller.ModelState.AddModelError("Email", "Invalid");

        var result = controller.Create(NewStaff("Alice", "bad"), "dev", "email", "desc");

        AssertBackToSameView(result, "dev", "email", "desc");
    }

    [Fact]
    public void Edit_KeepsSearchAndSort()
    {
        NewController().Create(NewStaff("Alice", "alice@x.com"));
        var edited = NewStaff("Alicia", "alice@x.com");
        edited.StaffId = 1;

        var result = NewController().Edit(edited, "ali", "position", "asc");

        AssertBackToSameView(result, "ali", "position", "asc");
    }

    [Fact]
    public void Edit_Failure_KeepsSearchAndSort()
    {
        var unknown = NewStaff("Nobody", "no@x.com");
        unknown.StaffId = 42;

        var result = NewController().Edit(unknown, "ali", "name", "desc");

        AssertBackToSameView(result, "ali", "name", "desc");
    }

    [Fact]
    public void Delete_KeepsSearchAndSort()
    {
        NewController().Create(NewStaff("Alice", "alice@x.com"));

        var result = NewController().Delete(1, "ali", "id", "desc");

        AssertBackToSameView(result, "ali", "id", "desc");
    }

    [Fact]
    public void Delete_Failure_KeepsSearchAndSort()
    {
        var result = NewController().Delete(42, "ali", "unit", "asc");

        AssertBackToSameView(result, "ali", "unit", "asc");
    }

    [Fact]
    public void Create_WithoutSearchOrSort_RedirectsToPlainIndex()
    {
        var result = NewController().Create(NewStaff("Alice", "alice@x.com"));

        AssertBackToSameView(result, null, null, null);
    }

    private class InMemoryTempDataProvider : ITempDataProvider
    {
        private IDictionary<string, object> _data = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _data = values;
    }
}
