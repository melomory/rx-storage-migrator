namespace RxStorageMigrator.Application.Models;

/// <summary>
/// Результат переноса документов.
/// </summary>
/// <param name="PerformedCount">Количество успешно перенесенных документов.</param>
/// <param name="TotalCount">Общее количество перенесенных документов.</param>
public sealed record TransferredCountResult(
  int PerformedCount,
  int TotalCount);
