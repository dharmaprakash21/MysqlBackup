using Cronos;
using Microsoft.Extensions.Options;
using MysqlBackup.Models;

namespace MySqlBackupApi.Services
{
    public class BackupHostedService : BackgroundService
    {
        private readonly MySqlBackupService _backupService;

        private readonly CronExpression _cron;

        private readonly TimeZoneInfo _timeZone = TimeZoneInfo.Local;

        public BackupHostedService(MySqlBackupService backupService, IOptions<BackupConfig> backupConfig)
        {
            _backupService = backupService;
            _cron = CronExpression.Parse(backupConfig.Value.CronExpression);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var next = _cron.GetNextOccurrence(DateTimeOffset.Now, _timeZone);
                if (!next.HasValue) continue;

                var delay = next.Value - DateTimeOffset.Now;
                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay, stoppingToken);

                Console.WriteLine($"[CRON] Backup started at {DateTime.Now}");

                try
                {
                    await _backupService.BackupAllDatabasesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CRON ERROR] {ex.Message}");
                }

                Console.WriteLine($"[CRON] Backup finished at {DateTime.Now}");
            }
        }
    }
}
