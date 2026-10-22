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

    // ---------- Search ----------

    //A small team used by the search and sort tests
    private void AddTeam()
    {
        var service = NewService();
        service.AddStaff(new StaffMember { FullName = "Charlie Brown", Email = "charlie@acme.com", Position = "Developer", Unit = "IT" });     //ID 1
        service.AddStaff(new StaffMember { FullName = "alice Smith", Email = "alice@acme.com", Position = "Manager", Unit = "HR" });         //ID 2
        service.AddStaff(new StaffMember { FullName = "Bob Jones", Email = "bob@other.org", Position = "Senior Developer", Unit = "IT" });   //ID 3
        service.AddStaff(new StaffMember { FullName = "Dana 50% Off", Email = "dana@acme.com", Position = "Sales_Rep", Unit = "Sales" });   //ID 4
    }

    private List<string> SearchNames(string? search, string? sortBy = null, bool descending = false) =>
        NewService().GetAllStaff(search, sortBy, descending).Select(s => s.FullName).ToList();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Search_Empty_ReturnsEveryone(string? search)
    {
        AddTeam();

        Assert.Equal(4, SearchNames(search).Count);
    }

    [Theory]
    [InlineData("bob", new[] { "Bob Jones" })]                                   //Name
    [InlineData("other.org", new[] { "Bob Jones" })]                             //Email
    [InlineData("developer", new[] { "Charlie Brown", "Bob Jones" })]            //Position, different case
    [InlineData("hr", new[] { "alice Smith" })]                                  //Unit
    [InlineData("ACME", new[] { "Charlie Brown", "alice Smith", "Dana 50% Off" })] //Upper case
    [InlineData("  smith  ", new[] { "alice Smith" })]                           //Spaces trimmed
    [InlineData("rown", new[] { "Charlie Brown" })]                              //Middle of a word
    public void Search_MatchesAnyColumn_IgnoringCase(string search, string[] expected)
    {
        AddTeam();

        Assert.Equal(expected, SearchNames(search));
    }

    [Fact]
    public void Search_Number_FindsStaffId()
    {
        AddTeam();

        Assert.Equal(new[] { "Bob Jones" }, SearchNames("3"));
    }

    [Theory]
    [InlineData("50%", new[] { "Dana 50% Off" })]  //% is a LIKE wildcard, must be searched literally
    [InlineData("%", new[] { "Dana 50% Off" })]
    [InlineData("s_r", new[] { "Dana 50% Off" })]  //_ is a LIKE wildcard: unescaped it would also match "Senior"
    public void Search_WildcardCharacters_AreMatchedLiterally(string search, string[] expected)
    {
        AddTeam();

        Assert.Equal(expected, SearchNames(search));
    }

    [Fact]
    public void Search_NoMatches_ReturnsEmptyList()
    {
        AddTeam();

        Assert.Empty(SearchNames("zzz"));
    }

    // ---------- Sort ----------

    [Fact]
    public void Sort_Default_IsByIdAscending()
    {
        AddTeam();

        Assert.Equal(new[] { 1, 2, 3, 4 }, NewService().GetAllStaff().Select(s => s.StaffId));
    }

    [Fact]
    public void Sort_ByIdDescending()
    {
        AddTeam();

        Assert.Equal(new[] { 4, 3, 2, 1 }, NewService().GetAllStaff(null, "id", descending: true).Select(s => s.StaffId));
    }

    [Fact]
    public void Sort_ByName_IgnoresCase()
    {
        AddTeam();

        //"alice" is lower case but still comes first, a plain sort would put it after the capitals
        Assert.Equal(new[] { "alice Smith", "Bob Jones", "Charlie Brown", "Dana 50% Off" }, SearchNames(null, "name"));
    }

    [Fact]
    public void Sort_ByNameDescending()
    {
        AddTeam();

        Assert.Equal(new[] { "Dana 50% Off", "Charlie Brown", "Bob Jones", "alice Smith" }, SearchNames(null, "name", descending: true));
    }

    [Theory]
    [InlineData("email", new[] { 2, 3, 1, 4 })]     //alice@, bob@, charlie@, dana@
    [InlineData("position", new[] { 1, 2, 4, 3 })]  //Developer, Manager, Sales_Rep, Senior Developer
    [InlineData("unit", new[] { 2, 1, 3, 4 })]      //HR, IT, IT, Sales (the two IT staff stay in ID order)
    public void Sort_ByOtherColumns(string sortBy, int[] expectedIds)
    {
        AddTeam();

        Assert.Equal(expectedIds, NewService().GetAllStaff(null, sortBy).Select(s => s.StaffId));
    }

    [Fact]
    public void Sort_UnknownColumn_FallsBackToId()
    {
        AddTeam();

        Assert.Equal(new[] { 1, 2, 3, 4 }, NewService().GetAllStaff(null, "salary").Select(s => s.StaffId));
    }

    [Fact]
    public void SearchAndSort_Together()
    {
        AddTeam();

        Assert.Equal(new[] { "Charlie Brown", "Bob Jones" }, SearchNames("developer", "name", descending: true));
    }

    [Fact]
    public void CountStaff_CountsEveryone()
    {
        AddTeam();

        Assert.Equal(4, NewService().CountStaff());
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
