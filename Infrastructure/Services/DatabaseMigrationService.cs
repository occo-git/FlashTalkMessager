using Infrastructure.Data;
using Infrastructure.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class DatabaseMigrationService : IDatabaseMigrationService
    {
        private readonly DataContext _dbContext;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(DataContext dbContext, ILogger<DatabaseMigrationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task MigrateDatabaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Migration STARTED");
                await _dbContext.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Migration COMPLETED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration ERROR");
                throw;
            }
        }
    }
}
