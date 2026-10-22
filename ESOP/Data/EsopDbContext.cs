using ESOP.Models;
using Microsoft.EntityFrameworkCore;

namespace ESOP.Data;

//EF Core's link to the SQLite database: each DbSet is a table
public class EsopDbContext : DbContext
{
    public EsopDbContext(DbContextOptions<EsopDbContext> options) : base(options)
    {
    }

    public DbSet<StaffMember> Staff => Set<StaffMember>(); //The "Staff" table, one row per StaffMember
}
