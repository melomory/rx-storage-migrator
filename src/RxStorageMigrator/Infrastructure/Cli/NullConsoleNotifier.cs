using RxStorageMigrator.Application.Interfaces;

namespace RxStorageMigrator.Infrastructure.Cli;

/// <summary>
/// Пустая реализация <see cref="IConsoleNotifier"/>, не выполняющая вывод сообщений.
/// </summary>
/// <remarks>
/// Используется в сценариях, где вывод в консоль отключён
/// (например, при тестировании, фоновом выполнении или включенном выводе лога в консоль).
/// </remarks>
public sealed class NullConsoleNotifier : IConsoleNotifier
{
  /// <inheritdoc />
  public void WriteInfo(string message)
  {
    // Intentionally empty.
  }
}
