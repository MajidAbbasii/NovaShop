using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using NovaShop.Api.Extensions;

// Fix for inotify/FileSystemWatcher crash on Render:
//
// WebApplication.CreateBuilder(args) internally adds default JSON configuration
// sources (appsettings.json, appsettings.{ENV}.json) with reloadOnChange: true.
// These sources are materialized into FileConfigurationProvider instances that
// create FileSystemWatcher instances (inotify on Linux). On Render the default
// inotify user limit (128) is exhausted, causing:
//   System.IO.IOException: The configured user limit (128) on the number of
//   inotify instances has been reached...
//
// IMPORTANT: ConfigurationManager is LAZY. Providers are not permanently
// materialized during CreateBuilder construction. When we clear the sources
// and re-add them with reloadOnChange: false, the provider manager rebuilds
// the providers from the new sources, creating NO FileSystemWatchers.
//
// This approach preserves ALL ASP.NET Core hosting defaults (Kestrel, IServer,
// routing, authentication, etc.) that CreateBuilder sets up, while eliminating
// the JSON config FileSystemWatchers that cause the Render crash.
var builder = WebApplication.CreateBuilder(args);

// Clear all default configuration sources (JSON with reloadOnChange: true, etc.)
// and re-add them with reloadOnChange: false to prevent FileSystemWatcher creation.
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
if (args != null && args.Length > 0)
    builder.Configuration.AddCommandLine(args);

ProgramHelpers.ConfigureLogging(builder);
ProgramHelpers.ConfigureServices(builder);

var app = builder.Build();

ProgramHelpers.ConfigurePipeline(app);

app.Run();
