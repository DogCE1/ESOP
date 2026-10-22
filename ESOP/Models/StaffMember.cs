//Deliverable 1: Whole file
using System.ComponentModel.DataAnnotations;

namespace ESOP.Models;

public class StaffMember
{
    public int StaffId { get; set; } //Gets no required, because gets no user input, the service assigns it

    [Required]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(150)]
    [Display(Name = "Email Address")]
    public string Email { get; set; }  = string.Empty;
    
    [Required]
    [Display(Name = "Position")]
    [StringLength(50)]
    public string Position {get; set;} = string.Empty;
    
    [Required]
    [Display(Name = "Unit")]
    [StringLength(50)]
    public string Unit { get; set; }  = string.Empty;
}