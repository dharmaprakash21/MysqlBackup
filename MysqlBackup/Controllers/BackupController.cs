using Microsoft.AspNetCore.Mvc;
using MySqlBackupApi.Services;

[ApiController]
[Route("[controller]")]
public class BackupController : ControllerBase
{
    private readonly MySqlBackupService _backupService;

    public BackupController(MySqlBackupService backupService)
    {
        _backupService = backupService;
    }

    [HttpPost("now")]
    public async Task<IActionResult> BackupNow()
    {
        await _backupService.BackupAllDatabasesAsync();
        return Ok("Backup completed successfully!");
    }
}
