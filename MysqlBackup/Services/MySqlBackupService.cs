using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using MysqlBackup.Models;
using System.Diagnostics;

namespace MySqlBackupApi.Services
{
    public class MySqlBackupService
    {
        private readonly MySqlSettings _mysql;
        private readonly BackupConfig _config;

        public MySqlBackupService( IOptions<MySqlSettings> mysqlOptions, IOptions<BackupConfig> backupConfig)
        {
            _mysql = mysqlOptions.Value;
            _config = backupConfig.Value;

            if (!Directory.Exists(_config.BackupFolder))
                Directory.CreateDirectory(_config.BackupFolder);
        }

        public async Task BackupAllDatabasesAsync()
        {
            foreach (var db in _mysql.Databases)
            {
                if (!await DatabaseExistsAsync(db))
                {
                    Console.WriteLine($"[SKIP] Database '{db}' not found");
                    continue;
                }

                string dbFolder = Path.Combine(_config.BackupFolder, db);
                if (!Directory.Exists(dbFolder))
                    Directory.CreateDirectory(dbFolder);

                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{db}_{timestamp}.sql";
                string localPath = Path.Combine(dbFolder, fileName);

                await DumpDatabaseAsync(db, localPath);

                Console.WriteLine($"[OK] Backup created: {localPath}");

                DeleteOldBackupsForDb(dbFolder);
            }
        }

        private async Task<bool> DatabaseExistsAsync(string dbName)
        {
            string conn = $"server={_mysql.Server};user={_mysql.Username};password={_mysql.Password};port={_mysql.Port};";

            using var connection = new MySqlConnection(conn);
            await connection.OpenAsync();

            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME=@db", connection);

            cmd.Parameters.AddWithValue("@db", dbName);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }

        private async Task DumpDatabaseAsync(string dbName, string outputPath)
        {

            var psi = new ProcessStartInfo
            {
                FileName = _config.MySqlDumpPath,
                Arguments = $"-h {_mysql.Server} -P {_mysql.Port} -u {_mysql.Username} -p{_mysql.Password} {dbName} -r \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"mysqldump failed for {dbName}");
        }

        private void DeleteOldBackupsForDb(string dbFolder)
        {
            var files = Directory.GetFiles(dbFolder, "*.sql") .Select(f => new FileInfo(f)).OrderByDescending(f => f.CreationTimeUtc).ToList();

            foreach (var file in files.Skip(_config.RetentionDays))
            {
                file.Delete();
                Console.WriteLine($"[DELETE] {file.FullName}");
            }
        }
    }
}
