//Deliverable 2: Whole file
using ESOP.Models;

namespace ESOP.Services;

public class StaffService
{
  private readonly List<StaffMember> _staff = new();

  //The service is a singleton, so several requests can use the list at the same time.
  //List<T> is not thread-safe, so every read/write is wrapped in a lock on this object
  private readonly object _lock = new();

  public void AddStaff(StaffMember staff)
  {
    lock (_lock)
    {
      // Gets the current highest ID nr and then adding 1 to it. Instead of using list count, so ID's stay unique after delete
      // DefaultIfEmpty(0) makes the first staff member get ID 1
      staff.StaffId = _staff.Select(sta => sta.StaffId).DefaultIfEmpty(0).Max() + 1;
      _staff.Add(staff);
    }
  }

  public List<StaffMember> GetAllStaff()
  {
    lock (_lock)
    {
      return new List<StaffMember>(_staff); //Returns a copy so callers can't change the list outside the lock
    }
  }

  public StaffMember GetStaffById(int id)
  {
    lock (_lock)
    {
      var check = _staff.FirstOrDefault(x => x.StaffId == id);
      if (check == null)
      {
        throw new ArgumentException("StaffId not found");
      }
      return check;
    }
  }

  public void UpdateStaff(StaffMember staff)
  {
    lock (_lock)
    {
      var index = _staff.FindIndex(x => x.StaffId == staff.StaffId); //Finds the list position of the staff member being updated
      if (index >= 0)
      {
        _staff[index] = staff; // Overwrites the list slot with new incoming object
      }
      else
      {
        throw new ArgumentException("StaffId not found");
      }
    }
  }

  public StaffMember DeleteStaff(int id)
  {
    lock (_lock)
    {
      var staff = GetStaffById(id); //Finds the staff member being removed (throws if the ID doesn't exist)
      _staff.Remove(staff); //Remove staff member from the list
      return staff; //Returns the deleted staff member so the controller can report who was removed
    }
  }
}
