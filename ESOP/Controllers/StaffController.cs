//Deliverable 3: Whole file
using ESOP.Models;
using ESOP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ESOP.Controllers;

//Auth on a class: it is required to be logged in for every action here, not just some
[Authorize]
public class StaffController : Controller
{
    private readonly StaffService _staffService;

    public StaffController(StaffService staffService)
    {
        _staffService = staffService;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        //Loads all staff and displays them
        var staffInfo = _staffService.GetAllStaff();
        return View(staffInfo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken] //Rejects posts that did not come from our own form (CSRF protection)
    public IActionResult Create(StaffMember staffMember)
    {
        if (ModelState.IsValid) //Checks if the submitted data passed all validation rules
        {
            try
            {
                //Adds a new staff member if the model is valid
                _staffService.AddStaff(staffMember);
                TempData["SMessage"] = $"Successfully added staff member: {staffMember.StaffId}, {staffMember.FullName}, {staffMember.Email}, {staffMember.Position}, {staffMember.Unit}";
            }
            catch (ArgumentException ex)
            {
                TempData["EMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
        else
        {
            TempData["EMessage"] = ValidationErrors();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(StaffMember staffMember)
    {
        if (ModelState.IsValid)
        {
            try
            {
                //Update a staff member if the model is valid
                _staffService.UpdateStaff(staffMember);
                TempData["SMessage"] = $"Successfully updated staff member: {staffMember.StaffId}, {staffMember.FullName}";
            }
            catch (ArgumentException ex)
            {
                TempData["EMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
        else
        {
            TempData["EMessage"] = ValidationErrors();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        try
        {
            //Removes a staff member by ID and reports who was deleted
            var staff = _staffService.DeleteStaff(id);
            TempData["SMessage"] = $"Successfully deleted staff member: {staff.StaffId}, {staff.FullName}";
        }
        catch (ArgumentException ex)
        {
            TempData["EMessage"] = ex.Message;
            return RedirectToAction("Index");
        }
        return RedirectToAction("Index");
    }

    //Joins all validation messages into one line so the user sees why their input was rejected
    private string ValidationErrors()
    {
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        return "Could not save staff member: " + string.Join(" ", errors);
    }
}