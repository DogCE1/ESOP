//Deliverable 2: Whole file
using System.Linq.Expressions;
using ESOP.Data;
using ESOP.Models;
using Microsoft.EntityFrameworkCore;

namespace ESOP.Services;

//Reads and writes staff members in the SQLite database through EF Core.
//Registered as scoped (one instance per request), the same lifetime as the DbContext it uses
public class StaffService
{
  private readonly EsopDbContext _db;

  public StaffService(EsopDbContext db)
  {
    _db = db;
  }

  public void AddStaff(StaffMember staff)
  {
    staff.Email = staff.Email.Trim();
    EnsureEmailIsUnique(staff.Email, 0); //New staff member has no ID yet, so compare against everyone

    staff.StaffId = 0; //0 tells EF Core to let the database generate the ID (AUTOINCREMENT, so IDs are never reused after a delete)
    _db.Staff.Add(staff);
    _db.SaveChanges(); //Writes to the database and fills in staff.StaffId
  }

  //Columns the staff table can be sorted by, as used in the URL (?sort=name)
  public static readonly string[] SortColumns = { "id", "name", "email", "position", "unit" };

  //Returns staff matching the search (if any), sorted by the given column. Runs as one SQL query in the database
  public List<StaffMember> GetAllStaff(string? search = null, string? sortBy = null, bool descending = false)
  {
    IQueryable<StaffMember> query = _db.Staff.AsNoTracking(); //Read-only query, so no change tracking needed

    if (!string.IsNullOrWhiteSpace(search))
    {
      search = search.Trim();
      var pattern = "%" + EscapeLike(search) + "%"; //"%dev%" = contains "dev" anywhere
      var isNumber = int.TryParse(search, out var searchId);

      //SQL LIKE ignores upper/lower case, so "dev" also finds "Developer"
      query = query.Where(s =>
        EF.Functions.Like(s.FullName, pattern, "\\") ||
        EF.Functions.Like(s.Email, pattern, "\\") ||
        EF.Functions.Like(s.Position, pattern, "\\") ||
        EF.Functions.Like(s.Unit, pattern, "\\") ||
        (isNumber && s.StaffId == searchId)); //Typing a number also finds that staff ID
    }

    if (sortBy == null || sortBy == "id" || !SortColumns.Contains(sortBy))
    {
      return (descending ? query.OrderByDescending(s => s.StaffId) : query.OrderBy(s => s.StaffId)).ToList();
    }

    //NOCASE so "alice" and "Bob" sort alphabetically, instead of all capitals first
    Expression<Func<StaffMember, string>> sortKey = sortBy switch
    {
      "name" => s => EF.Functions.Collate(s.FullName, "NOCASE"),
      "email" => s => EF.Functions.Collate(s.Email, "NOCASE"),
      "position" => s => EF.Functions.Collate(s.Position, "NOCASE"),
      _ => s => EF.Functions.Collate(s.Unit, "NOCASE"),
    };

    var sorted = descending ? query.OrderByDescending(sortKey) : query.OrderBy(sortKey);
    return sorted.ThenBy(s => s.StaffId).ToList(); //Ties (e.g. same unit) stay in ID order
  }

  public int CountStaff()
  {
    return _db.Staff.Count();
  }

  public StaffMember GetStaffById(int id)
  {
    var check = _db.Staff.Find(id);
    if (check == null)
    {
      throw new ArgumentException("StaffId not found");
    }
    return check;
  }

  public void UpdateStaff(StaffMember staff)
  {
    var existing = GetStaffById(staff.StaffId); //Loads the current row (throws if the ID doesn't exist)

    staff.Email = staff.Email.Trim();
    EnsureEmailIsUnique(staff.Email, staff.StaffId); //Keeping your own email is fine, taking someone else's is not

    _db.Entry(existing).CurrentValues.SetValues(staff); //Copies the incoming values onto it
    _db.SaveChanges();
  }

  public StaffMember DeleteStaff(int id)
  {
    var staff = GetStaffById(id); //Finds the staff member being removed (throws if the ID doesn't exist)
    _db.Staff.Remove(staff);
    _db.SaveChanges();
    return staff; //Returns the deleted staff member so the controller can report who was removed
  }

  //% and _ are wildcards in LIKE, escape them so searching "50%" means the literal text "50%"
  private static string EscapeLike(string text)
  {
    return text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
  }

  //Throws if any other staff member already uses this email (case-insensitive, see EsopDbContext)
  private void EnsureEmailIsUnique(string email, int ownStaffId)
  {
    if (_db.Staff.Any(s => s.Email == email && s.StaffId != ownStaffId))
    {
      throw new ArgumentException($"A staff member with email {email} already exists");
    }
  }
}
