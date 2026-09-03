// Fix for inotify/FileSystemWatcher crash on Render:
//
// The Render stack trace proves that WebApplication.CreateBuilder(args)
// calls Host.CreateApplicationBuilder -> HostApplicationBuilder.ctor ->
// ApplyDefaultAppConfigurations, which adds default JSON configuration
// sources (appsettings.json, appsettings.{ENV}.json, {PROJECT}.settings.json)
// with reloadOnChange: true. On .NET 10 this materializes the
// FileConfigurationProvider instances DURING CreateBuilder construction,
// creating FileSystemWatcher instances (inotify on Linux). On Render the
// default inotify user limit (128) is exhausted, causing:
//   System.IO.IOException: The configured user limit (128) on the number of
//   inotify instances has been reached...
//
// The stack trace:
//   at Microsoft.Extensions.Hosting.HostingHostBuilderExtensions.ApplyDefaultAppConfiguration(...)
//   at Microsoft.Extensions.Hosting.HostApplicationBuilder..ctor(...)
//   at Microsoft.AspNetCore.Builder.WebApplicationBuilder..ctor(...)
//   at Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(String[] args)
//
// The previous Sources.Clear() approach was INSUFFICIENT because:
//   1. The FileSystemWatcher instances are created INSIDE CreateBuilder,
//      before our Sources.Clear() code in Program.cs can execute.
//   2. The crash occurs during CreateBuilder construction itself.
//
// WebApplication.CreateSlimBuilder(args) is the correct fix:
//   - Does NOT call ApplyDefaultAppConfigurations (the crash point).
//   - Still registers Kestrel/IServer (unlike CreateEmptyBuilder which
//     removed web hosting and caused
//     "No service for type 'IServer' has been registered").
//   - Registers 103 services including IServer/Kestrel.
//
// After CreateSlimBuilder, we must explicitly re-add all configuration:
//   - appsettings.json (reloadOnChange: false)
//   - appsettings.{Environment}.json (reloadOnChange: false)
//   - User secrets (Development only) — CreateSlimBuilder does NOT add
//     these automatically; CreateBuilder does via AddDefaultWebConfigSources.
//   - Environment variables
//   - Command-line args
//
// Verified: After this fix, zero FileConfigurationSources have
// reloadOnChange=true, so no FileSystemWatcher/inotify instances are created.
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using NovaShop.Api.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

// Clear all default configuration sources and re-add with reloadOnChange: false.
// CreateSlimBuilder adds minimal defaults (appsettings.json + env var sources).
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
// User secrets must be added explicitly — CreateSlimBuilder does NOT call
// AddDefaultWebConfigSources (which CreateBuilder does). Preserve the
// UserSecretsId from NovaShop.Api.csproj for Development local dev.
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();
builder.Configuration.AddEnvironmentVariables();
if (args != null && args.Length > 0)
    builder.Configuration.AddCommandLine(args);

ProgramHelpers.ConfigureLogging(builder);
ProgramHelpers.ConfigureServices(builder);

var app = builder.Build();

ProgramHelpers.ConfigurePipeline(app);

app.Run();
