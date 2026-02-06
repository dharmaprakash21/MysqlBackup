namespace MysqlBackup.Models
{
    public class MySqlSettings
    {
        public string Server { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<string> Databases { get; set; }
    }
    public class AWSSettings
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; }
        public string BucketName { get; set; }
    }

    public class BackupConfig
    {
        public string BackupFolder { get; set; }
        public int RetentionDays { get; set; }
        public string MySqlDumpPath { get; set; }
        public string CronExpression { get; set; }
    }
}
