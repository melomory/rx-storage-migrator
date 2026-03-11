namespace RxStorageMigrator.Application.Interfaces;

/// <summary>
/// Контракт для вывода информационных сообщений пользователю в консоли.
/// </summary>
public interface IConsoleNotifier
{
  /// <summary>
  /// Вывести информационное сообщение.
  /// </summary>
  /// <param name="message">Текст сообщения.</param>
  void WriteInfo(string message);
}
