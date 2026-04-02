using Argus.EvidencePlatform.Application;
using Argus.EvidencePlatform.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.AddInfrastructure();

var host = builder.Build();
await host.RunAsync();
