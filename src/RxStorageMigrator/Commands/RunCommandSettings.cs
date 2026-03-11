using System.ComponentModel;
using RxStorageMigrator.Infrastructure.Cli.Attributes;
using RxStorageMigrator.Infrastructure.Cli.Resources;
using Spectre.Console.Cli;

namespace RxStorageMigrator.Commands;

/// <summary>
/// Параметры команды запуска миграции.
/// </summary>
/// <remarks>
/// Используется в CLI‑команде для управления поведением процесса миграции.
/// </remarks>
public sealed class RunCommandSettings : CommandSettings
{
  /// <summary>
  /// Указывает, необходимо ли очистить таблицы перед запуском миграции.
  /// </summary>
  /// <value>
  /// <c>true</c>, если перед запуском миграции требуется очистка таблиц; иначе <c>false</c>.
  /// </value>
  /// <remarks>
  /// Соответствует параметру командной строки <c>--clear</c>.
  /// </remarks>
  [CommandOption("-c|--clear")]
  [LocalizedDescription(nameof(CliResources.ClearTablesBeforeMigration))]
  public bool ClearTables { get; init; }
}
