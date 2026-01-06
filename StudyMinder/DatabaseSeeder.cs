using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;

namespace StudyMinder
{
    public class DatabaseSeeder
    {
        public static void SeedDatabase(StudyMinderContext context)
        {
            // Garante que o banco foi criado
            context.Database.EnsureCreated();
            
            // Marca migrações como aplicadas se necessário
            try
            {
                var pendingMigrations = context.Database.GetPendingMigrations();
                if (pendingMigrations.Any())
                {
                    context.Database.Migrate();
                }
            }
            catch
            {
                // Se falhar, assume que o banco já está no estado correto 
            }
        }
    }
}
