using System.Linq;
using Microsoft.Extensions.Configuration;
using NovaShop.Api.Extensions;

// Fix: Disable configuration file reload (reloadOnChange) to prevent
// FileSystemWatcher / inotify instance creation. Render's default inotify
// limit (128) is exhausted by the default JSON file change tokens,
// causing a startup crash:
//   System.IO.IOException: The configured user limit (128) on the number of
//   inotify instances has been reached...
//
// ASP.NET Core's default JSON file providers set reloadOnChange: true,
// which creates a FileSystemWatcher per file.  In Production/Render we
// don't need live-reload — all config is static.
//
// .NET 8+ WebApplicationBuilder uses ConfigurationManager (which implements
// both IConfigurationRoot and IConfigurationBuilder) and builds configuration
// providers lazily.  By setting ReloadOnChange=false on the JSON file sources
// BEFORE any configuration value is read (i.e. before ConfigureLogging), the
// FileConfigurationProvider will not create a FileSystemWatcher when it
// eventually builds.  Environment variables and command-line args are
// unaffected — they have no ReloadOnChange property and are left as-is.
var builder = WebApplication.CreateBuilder(args);

// ConfigurationManager implements IConfigurationBuilder, so we can cast and
// modify the sources before providers are lazily built.
if (builder.Configuration is IConfigurationBuilder configBuilder)
{
    foreach (var source in configBuilder.Sources)
    {
        var prop = source.GetType().GetProperty("ReloadOnChange");
        if (prop != null && prop.CanWrite)
        {
            var val = prop.GetValue(source);
            if (val is bool && (bool)val)
                prop.SetValue(source, false);
        }
    }
}

ProgramHelpers.ConfigureLogging(builder);
ProgramHelpers.ConfigureServices(builder);

WebApplication app = builder.Build();

ProgramHelpers.ConfigurePipeline(app);

app.Run();
