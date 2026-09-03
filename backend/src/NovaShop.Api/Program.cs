using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using NovaShop.Api.Extensions;

// Fix: Use WebApplication.CreateEmptyBuilder instead of WebApplication.CreateBuilder.
//
// WebApplication.CreateBuilder internally calls Host.CreateApplicationBuilder which
// adds default JSON configuration sources (appsettings.json, appsettings.{ENV}.json)
// with reloadOnChange: true. These sources are THEN materialized into
// FileConfigurationProvider instances during CreateBuilder's construction, creating
// FileSystemWatcher instances (inotify on Linux). On Render the default inotify
// user limit (128) is exhausted, causing:
//   System.IO.IOException: The configured user limit (128) on the number of
//   inotify instances has been reached...
//
// A post-hoc fix that mutates ReloadOnChange on source definitions AFTER
// CreateBuilder returns is TOO LATE — the providers (and their watchers)
// are already built during CreateBuilder.
//
// Solution: CreateEmptyBuilder does NOT add default JSON config sources. We add
// them ourselves with reloadOnChange: false, guaranteeing FileConfigurationProvider
// will NOT create a FileSystemWatcher (verified: _changeTokenRegistration is NULL).
//
// All configuration sources from the original CreateBuilder are preserved:
//   - appsettings.json
//   - appsettings.{Environment}.json
//   - Environment variables
//   - Command-line arguments
var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
{
    Args = args,
});

// Set the base path for JSON file resolution.
// CreateEmptyBuilder does not set this automatically, so we set it
// to the content root path (defaults to AppContext.BaseDirectory).
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath);

// Add JSON configuration sources with reloadOnChange: false to prevent
// FileSystemWatcher / inotify instance creation. All other config sources
// (environment variables, command-line args) are unaffected — they have
// no ReloadOnChange property and cannot create watchers.
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
var envName = builder.Environment.EnvironmentName;
builder.Configuration.AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
if (args != null && args.Length > 0)
    builder.Configuration.AddCommandLine(args);

ProgramHelpers.ConfigureLogging(builder);
ProgramHelpers.ConfigureServices(builder);

var app = builder.Build();

ProgramHelpers.ConfigurePipeline(app);

app.Run();
