using NovaShop.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ProgramHelpers.ConfigureLogging(builder);
ProgramHelpers.ConfigureServices(builder);

WebApplication app = builder.Build();

ProgramHelpers.ConfigurePipeline(app);

app.Run();
