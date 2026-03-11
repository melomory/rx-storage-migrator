using Microsoft.Extensions.Logging;
using RxStorageMigrator.Application.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RxStorageMigrator.Commands;

/// <summary>
/// CLI‑команда, запускающая процесс миграции.
/// </summary>
public class RunCommand(
  ILogger<RunCommand> logger,
  IConsoleNotifier consoleNotifier,
  IMigrationService migrationService)
  : AsyncCommand<RunCommandSettings>
{
  /// <inheritdoc/>
  public override async Task<int> ExecuteAsync(
    CommandContext context,
    RunCommandSettings settings,
    CancellationToken cancellationToken)
  {
    consoleNotifier.WriteInfo("Starting migration...");

    try
    {
      logger.LogInformation("Migration started");

      if (settings.ClearTables)
      {
        logger.LogInformation("Clearing tables before migration...");
        consoleNotifier.WriteInfo("Clearing tables before migration...");
        await migrationService.ClearAsync(cancellationToken);
      }

      await AnsiConsole
        .Progress()
        .Columns(
          new TaskDescriptionColumn(),
          new ProgressBarColumn(),
          new PercentageColumn(),
          new SpinnerColumn(),
          new ElapsedTimeColumn(),
          new RemainingTimeColumn())
        .AutoClear(false)
        .StartAsync(
          async ctx =>
          {
            var task = ctx.AddTask("[yellow]Processing data[/]...");

            var progress =
              new Progress<(int Processed, int Total)>(
                p =>
                {
                  task.MaxValue = p.Total > 0 ? p.Total : 1;
                  task.Value = p.Total > 0 ? p.Processed : 1;
                });

            await migrationService.RunAsync(progress, cancellationToken);
          });

      consoleNotifier.WriteInfo($"Migration completed!");

      return 0;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Command failed");

      return 1;
    }
    finally
    {
      logger.LogInformation("Migration finished");
    }
  }
}
