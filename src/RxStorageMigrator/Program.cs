using Microsoft.Extensions.Hosting;
using RxStorageMigrator.Bootstrap;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
  .AddApplication()
  .AddInfrastructure(builder.Configuration);

StartupValidation.Validate(builder.Services);

var app = CliSetup.Create(builder.Services);

return await app.RunAsync(args);
