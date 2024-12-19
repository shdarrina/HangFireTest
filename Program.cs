using Hangfire;
using Hangfire.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Use Memory storage
builder.Services.AddHangfire(config => config.UseMemoryStorage());
// Use MsSql storage
// builder.Services.AddHangfire(config => config
//    .UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
// {
//     CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
//     SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
//     QueuePollInterval = TimeSpan.Zero,
//     UseRecommendedIsolationLevel = true,
//     UsePageLocksOnDequeue = true,
//     DisableGlobalLocks = true
// }));
// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

builder.Services.AddSingleton<BackgroundJobs>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowAllOrigins");

// Before running set environment variable to enable OpenAPI UI 
//   PowerShell - $Env:ASPNETCORE_ENVIRONMENT="Development"
//   Bash - export ASPNETCORE_ENVIRONMENT=Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.UseHangfireDashboard();


// Enqueue a fire-and-forget job
app.MapGet("/fire", (BackgroundJobs backgroundJobs, ILogger<Program> logger) =>
{
    logger.LogInformation("/fire called");

    BackgroundJob.Enqueue(() => backgroundJobs.FireAndForgetJob());

    return new { Message = "Fire-and-forget job enqueued" };
});

const string RecurringMessageStarted = "Recurring job started";
const string RecurringMessageStopped = "Recurring job stopped";

// Schedule a recurring job.  Fires by default every 5 seconds
app.MapGet("/timer/start", (BackgroundJobs backgroundJobs, IConfiguration config, ILogger<Program> logger) =>
{
    logger.LogInformation("/timer/start called");

    RecurringJob.AddOrUpdate("recurring-event", () => backgroundJobs.RecurringJob(), config.GetValue("RecurringJobCron", "*/5 * * * * *"));

    logger.LogInformation(RecurringMessageStarted);

    return new { Message = RecurringMessageStarted};
});

// Stop the recurring job
app.MapGet("/timer/stop", (IRecurringJobManager recurringJobManager, ILogger<Program> logger) =>
{
    logger.LogInformation("/timer/stop called");

    recurringJobManager.RemoveIfExists("recurring-event");

    logger.LogInformation(RecurringMessageStopped);
    return new { Message = RecurringMessageStopped};
});

app.Run();

public class BackgroundJobs(ILogger<BackgroundJobs> logger)
{
    public void FireAndForgetJob()
    {
        logger.LogInformation("Fire-and-forget event fired");
    }

    public void RecurringJob()
    {
        logger.LogInformation("Recurring 5s event fired");
    }
}