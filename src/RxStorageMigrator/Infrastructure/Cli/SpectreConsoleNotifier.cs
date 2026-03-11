using RxStorageMigrator.Application.Interfaces;
using Spectre.Console;

namespace RxStorageMigrator.Infrastructure.Cli;

/// <summary>
/// Реализация <see cref="IConsoleNotifier"/> с использованием
/// библиотеки Spectre.Console для форматированного вывода.
/// </summary>
public sealed class SpectreConsoleNotifier : IConsoleNotifier
{
  /// <inheritdoc />
  public void WriteInfo(string message) => AnsiConsole.MarkupLine($"[green]{message}[/]");
}
