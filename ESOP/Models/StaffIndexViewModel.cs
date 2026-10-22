namespace ESOP.Models;

//Everything the staff page needs: the filtered + sorted staff list, and the current search/sort so the page can show them
public class StaffIndexViewModel
{
    public List<StaffMember> Staff { get; set; } = new();

    public string? Search { get; set; } //Text in the search box, null when not searching

    public string Sort { get; set; } = "id"; //Column being sorted by, one of StaffService.SortColumns

    public bool Descending { get; set; }

    public int TotalCount { get; set; } //All staff in the database, so the page can show "2 of 5" while searching
}
