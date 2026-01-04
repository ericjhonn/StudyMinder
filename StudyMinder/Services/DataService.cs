using Microsoft.EntityFrameworkCore;
using StudyMinder.Data;

namespace StudyMinder.Services
{
    public class DataService
    {
        private readonly StudyMinderContext _context;

        public DataService(StudyMinderContext context)
        {
            _context = context;
        }

        public async Task<bool> TestarConexaoAsync()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CriarBancoDadosAsync()
        {
            try
            {
                await _context.Database.EnsureCreatedAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AplicarMigracoesAsync()
        {
            try
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    await _context.Database.MigrateAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LimparBancoDadosAsync()
        {
            await _context.Database.EnsureDeletedAsync();
        }
    }
}
