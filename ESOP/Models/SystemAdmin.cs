//Deliverable 1: Whole file
using System.ComponentModel.DataAnnotations;

namespace ESOP.Models;

public class SystemAdmin
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [Display(Name = "Username")]
    public string Username { get; set; } =  string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
}