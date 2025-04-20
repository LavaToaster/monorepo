using DBot.Bot.Configuration;
using DBot.Bot.Embeds;
using DBot.Bot.Hosting;
using DBot.Bot.Interfaces;
using DBot.Bot.Services;
using DBot.Core.Data.Context;
using DBot.Integrations.Assetto.Extensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

#if DEBUG
builder.Configuration.AddJsonFile("appsettings.local.json", true, true);
#endif

builder.Services.Configure<DiscordConfiguration>(builder.Configuration.GetSection("Discord"));

// Configure Discord client
builder.Services.AddSingleton(_ =>
{
    var config = new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.Guilds | GatewayIntents.GuildMessages,
        AlwaysDownloadUsers = true
    };

    return new DiscordSocketClient(config);
});

// Configure Discord interactions
builder.Services.AddSingleton(serviceProvider =>
{
    var config = new InteractionServiceConfig
    {
        DefaultRunMode = RunMode.Async,
        LogLevel = LogSeverity.Info
    };
    
    return new InteractionService(
        serviceProvider.GetRequiredService<DiscordSocketClient>(),
        config
    );
});

builder.Services.AddScoped<IAssettoServerService, AssettoServerService>();
builder.Services.AddAssettoServerClient();
builder.Services.AddSingleton<AssettoStatusMessageGenerator>();
builder.Services.AddSingleton<AssettoServerEmbedFactory>();
builder.Services.AddSingleton<RoleMirrorService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=dbot.db";

builder.Services.AddDbContext<ApplicationDbContext>(options => { options.UseSqlite(connectionString); });

// Add background services
builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddHostedService<AssettoServerMonitorService>();

// Add database migration service
builder.Services.AddHostedService<DatabaseMigrationService>();

var host = builder.Build();
await host.RunAsync();

// Database migration service to ensure DB is created and up to date
internal class DatabaseMigrationService(
    IServiceProvider services,
    ILogger<DatabaseMigrationService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Running database migrations");

            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying migrations");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}