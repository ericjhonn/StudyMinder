using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.Sqlite;
using StudyMinder.Data;

namespace StudyMinder.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StudyMinderContext>
    {
        public StudyMinderContext CreateDbContext(string[] args)
        {
            var connectionString = "Data Source=StudyMinder.db";
            var optionsBuilder = new DbContextOptionsBuilder<StudyMinderContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new StudyMinderContext(optionsBuilder.Options);
        }
    }
}
