using Microsoft.EntityFrameworkCore;
using RadarrAPI.Database.Models;

namespace RadarrAPI.Database
{
    public class DatabaseContext : DbContext
    {

        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<UpdatePackage> UpdatePackages { get; set; }

    }
}
