using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using MysqlBackup.Models;
using System.Diagnostics;

namespace MySqlBackupApi.Services
{
    public class MySqlBackupServiceAws
    {
        private readonly MySqlSettings _mysql;
        private readonly AWSSettings _aws;
        private readonly BackupConfig _config;
        private readonly IAmazonS3 _s3Client;

        public MySqlBackupServiceAws( IOptions<MySqlSettings> mysqlOptions, IOptions<AWSSettings> awsOptions, IOptions<BackupConfig> backupConfig)
        {
            _mysql = mysqlOptions.Value;
            _aws = awsOptions.Value;
            _config = backupConfig.Value;
            _s3Client = new AmazonS3Client( _aws.AccessKey, _aws.SecretKey, RegionEndpoint.GetBySystemName(_aws.Region));
        }

        public async Task BackupAllDatabasesAsync()
        {
            foreach (var db in _mysql.Databases)
            {
                var tempFile = Path.Combine( Path.GetTempPath(), $"{db}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql" );

                try
                {
                    await DumpDatabaseAsync(db, tempFile);
                    await UploadToS3Async(tempFile, db);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Backup failed for DB '{db}': {ex.Message}");
                }
                finally
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
        }

        private async Task DumpDatabaseAsync(string database, string filePath)
        {
            var args = $"-h {_mysql.Server} -P {_mysql.Port} -u {_mysql.Username} " + $"-p{_mysql.Password} {database}";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _config.MySqlDumpPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await process.StandardOutput.BaseStream.CopyToAsync(fileStream);
            var error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception(error);
        }

        private async Task UploadToS3Async(string filePath, string database)
        {
            var key = $"mysql-backups/{database}/{Path.GetFileName(filePath)}";

            var transferUtility = new TransferUtility(_s3Client);

            await transferUtility.UploadAsync(filePath, _aws.BucketName, key);

            Console.WriteLine($"[S3] Uploaded {key}");
        }
    }
}
