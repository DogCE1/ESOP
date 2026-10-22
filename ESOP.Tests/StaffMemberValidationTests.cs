using System.ComponentModel.DataAnnotations;
using ESOP.Models;

namespace ESOP.Tests;

//Checks the [Required], [EmailAddress] and [StringLength] rules on StaffMember, the same rules MVC uses to fill ModelState
public class StaffMemberValidationTests
{
    private static List<ValidationResult> Validate(StaffMember staff)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(staff, new ValidationContext(staff), results, validateAllProperties: true);
        return results;
    }

    private static StaffMember ValidStaff() =>
        new() { FullName = "Alice Smith", Email = "alice@x.com", Position = "Developer", Unit = "IT" };

    [Fact]
    public void ValidStaffMember_HasNoErrors()
    {
        Assert.Empty(Validate(ValidStaff()));
    }

    [Fact]
    public void ShortButValidEmail_IsAccepted()
    {
        var staff = ValidStaff();
        staff.Email = "a@b.co";

        Assert.Empty(Validate(staff));
    }

    [Fact]
    public void InvalidEmail_IsRejected()
    {
        var staff = ValidStaff();
        staff.Email = "not-an-email";

        Assert.Contains(Validate(staff), r => r.MemberNames.Contains(nameof(StaffMember.Email)));
    }

    [Theory]
    [InlineData(nameof(StaffMember.FullName))]
    [InlineData(nameof(StaffMember.Email))]
    [InlineData(nameof(StaffMember.Position))]
    [InlineData(nameof(StaffMember.Unit))]
    public void EmptyRequiredField_IsRejected(string field)
    {
        var staff = ValidStaff();
        typeof(StaffMember).GetProperty(field)!.SetValue(staff, "");

        Assert.Contains(Validate(staff), r => r.MemberNames.Contains(field));
    }

    [Fact]
    public void FullNameOver100Characters_IsRejected()
    {
        var staff = ValidStaff();
        staff.FullName = new string('a', 101);

        Assert.Contains(Validate(staff), r => r.MemberNames.Contains(nameof(StaffMember.FullName)));
    }
}
