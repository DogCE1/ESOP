//Deliverable 2: Whole file
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

  public List<StaffMember> GetAllStaff()
  {
    return _db.Staff.AsNoTracking().OrderBy(s => s.StaffId).ToList(); //Read-only query, so no change tracking needed
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

  //Throws if any other staff member already uses this email (case-insensitive, see EsopDbContext)
  private void EnsureEmailIsUnique(string email, int ownStaffId)
  {
    if (_db.Staff.Any(s => s.Email == email && s.StaffId != ownStaffId))
    {
      throw new ArgumentException($"A staff member with email {email} already exists");
    }
  }
}
