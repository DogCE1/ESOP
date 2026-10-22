using ESOP.Models;
using ESOP.Services;
using Microsoft.EntityFrameworkCore;

namespace ESOP.Tests;

public class StaffServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    //Each call gets its own DbContext, so every step reads what was really saved to the database
    private StaffService NewService() => new(_db.CreateContext());

    private static StaffMember NewStaff(string name, string email) =>
        new() { FullName = name, Email = email, Position = "Developer", Unit = "IT" };

    // ---------- AddStaff ----------

    [Fact]
    public void AddStaff_FirstStaffMember_GetsId1()
    {
        var alice = NewStaff("Alice", "alice@x.com");

        NewService().AddStaff(alice);

        Assert.Equal(1, alice.StaffId);
    }

    [Fact]
    public void AddStaff_AssignsIncreasingIds()
    {
        var service = NewService();
        var alice = NewStaff("Alice", "alice@x.com");
        var bob = NewStaff("Bob", "bob@x.com");

        service.AddStaff(alice);
        service.AddStaff(bob);

        Assert.Equal(1, alice.StaffId);
        Assert.Equal(2, bob.StaffId);
    }

    [Fact]
    public void AddStaff_IgnoresIdSentByUser()
    {
        var alice = NewStaff("Alice", "alice@x.com");
        alice.StaffId = 99;

        NewService().AddStaff(alice);

        Assert.Equal(1, alice.StaffId); //The database decides the ID, not the form
    }

    [Fact]
    public void AddStaff_AfterDeletingNewest_DoesNotReuseId()
    {
        NewService().AddStaff(NewStaff("Alice", "alice@x.com"));
        NewService().AddStaff(NewStaff("Bob", "bob@x.com"));
        NewService().DeleteStaff(2);

        var carol = NewStaff("Carol", "carol@x.com");
        NewService().AddStaff(carol);

        Assert.Equal(3, carol.StaffId);
    }

    [Fact]
    public void AddStaff_IsSavedToDatabase()
    {
        NewService().AddStaff(NewStaff("Alice", "alice@x.com"));

        var saved = NewService().GetStaffById(1);

        Assert.Equal("Alice", saved.FullName);
        Assert.Equal("alice@x.com", saved.Email);
    }

    [Fact]
    public void AddStaff_TrimsEmail()
    {
        NewService().AddStaff(NewStaff("Alice", "  alice@x.com  "));

        Assert.Equal("alice@x.com", NewService().GetStaffById(1).Email);
    }

    [Theory]
    [InlineData("bob@x.com")]       //Exact same email
    [InlineData("BOB@X.COM")]       //Different case
    [InlineData("  bob@x.com ")]    //Extra spaces
    public void AddStaff_DuplicateEmail_Throws(string duplicateEmail)
    {
        NewService().AddStaff(NewStaff("Bob", "bob@x.com"));

        var ex = Assert.Throws<ArgumentException>(() => NewService().AddStaff(NewStaff("Robert", duplicateEmail)));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void AddStaff_DuplicateEmail_DoesNotSave()
    {
        NewService().AddStaff(NewStaff("Bob", "bob@x.com"));

        Assert.Throws<ArgumentException>(() => NewService().AddStaff(NewStaff("Robert", "bob@x.com")));

        Assert.Single(NewService().GetAllStaff());
    }

    // ---------- GetAllStaff / GetStaffById ----------

    [Fact]
    public void GetAllStaff_EmptyDatabase_ReturnsEmptyList()
    {
        Assert.Empty(NewService().GetAllStaff());
    }

    [Fact]
    public void GetAllStaff_ReturnsEveryoneOrderedById()
    {
        NewService().AddStaff(NewStaff("Alice", "alice@x.com"));
        NewService().AddStaff(NewStaff("Bob", "bob@x.com"));
        NewService().AddStaff(NewStaff("Carol", "carol@x.com"));

        var names = NewService().GetAllStaff().Select(s => s.FullName);

        Assert.Equal(new[] { "Alice", "Bob", "Carol" }, names);
    }

    [Fact]
    public void GetStaffById_UnknownId_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewService().GetStaffById(42));
    }

    // ---------- UpdateStaff ----------

    [Fact]
    public void UpdateStaff_SavesNewValues()
    {
        NewService().AddStaff(NewStaff("Bob", "bob@x.com"));

        NewService().UpdateStaff(new StaffMember { StaffId = 1, FullName = "Bobby", Email = "bobby@x.com", Position = "Lead", Unit = "HR" });

        var saved = NewService().GetStaffById(1);
        Assert.Equal("Bobby", saved.FullName);
        Assert.Equal("bobby@x.com", saved.Email);
        Assert.Equal("Lead", saved.Position);
        Assert.Equal("HR", saved.Unit);
    }

    [Fact]
    public void UpdateStaff_KeepingOwnEmail_IsAllowed()
    {
        NewService().AddStaff(NewStaff("Bob", "bob@x.com"));

        NewService().UpdateStaff(new StaffMember { StaffId = 1, FullName = "Bobby", Email = "BOB@x.com", Position = "Lead", Unit = "HR" });

        Assert.Equal("Bobby", NewService().GetStaffById(1).FullName);
    }

    [Fact]
    public void UpdateStaff_TakingAnotherStaffMembersEmail_Throws()
    {
        NewService().AddStaff(NewStaff("Alice", "alice@x.com"));
        NewService().AddStaff(NewStaff("Bob", "bob@x.com"));

        Assert.Throws<ArgumentException>(() =>
            NewService().UpdateStaff(new StaffMember { StaffId = 2, FullName = "Bob", Email = "alice@x.com", Position = "Dev", Unit = "IT" }));

        Assert.Equal("bob@x.com", NewService().GetStaffById(2).Email); //Unchanged
    }

    [Fact]
    public void UpdateStaff_UnknownId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            NewService().UpdateStaff(new StaffMember { StaffId = 42, FullName = "Nobody", Email = "no@x.com", Position = "Dev", Unit = "IT" }));
    }

    // ---------- DeleteStaff ----------

    [Fact]
    public void DeleteStaff_RemovesAndReturnsDeletedStaffMember()
    {
        NewService().AddStaff(NewStaff("Alice", "alice@x.com"));
        NewService().AddStaff(NewStaff("Bob", "bob@x.com"));

        var deleted = NewService().DeleteStaff(1);

        Assert.Equal("Alice", deleted.FullName);
        Assert.Equal("Bob", Assert.Single(NewService().GetAllStaff()).FullName);
    }

    [Fact]
    public void DeleteStaff_UnknownId_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewService().DeleteStaff(42));
    }

    [Fact]
    public void DeleteStaff_FreesEmailForReuse()
    {
        NewService().AddStaff(NewStaff("Alice", "alice@x.com"));
        NewService().DeleteStaff(1);

        NewService().AddStaff(NewStaff("New Alice", "alice@x.com")); //Should not throw

        Assert.Single(NewService().GetAllStaff());
    }

    // ---------- Database backstop ----------

    [Fact]
    public void Database_UniqueIndex_RejectsDuplicateEmail_EvenWithoutService()
    {
        using var context = _db.CreateContext();
        context.Staff.Add(NewStaff("Bob", "bob@x.com"));
        context.Staff.Add(NewStaff("Robert", "BOB@X.COM"));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
