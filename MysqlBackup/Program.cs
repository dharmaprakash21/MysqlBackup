using MysqlBackup.Models;
using MySqlBackupApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<MySqlSettings>(builder.Configuration.GetSection("MySql"));
builder.Services.Configure<AWSSettings>(builder.Configuration.GetSection("AWS"));
builder.Services.Configure<BackupConfig>(builder.Configuration.GetSection("BackupConfig"));

builder.Services.AddSingleton<MySqlBackupService>();
builder.Services.AddHostedService<BackupHostedService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
