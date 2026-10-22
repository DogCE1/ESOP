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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Emails are compared case-insensitively (NOCASE), so "Bob@x.com" and "bob@x.com" count as the same email
        modelBuilder.Entity<StaffMember>().Property(s => s.Email).UseCollation("NOCASE");

        //Unique index: the database itself refuses a second staff member with the same email,
        //a backstop behind the friendly check in StaffService
        modelBuilder.Entity<StaffMember>().HasIndex(s => s.Email).IsUnique();
    }
}
