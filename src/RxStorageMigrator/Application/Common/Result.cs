namespace RxStorageMigrator.Application.Common;

/// <summary>
/// Результат выполнения операции.
/// </summary>
/// <typeparam name="T">Тип значения, возвращаемого при успешном выполнении.</typeparam>
public sealed record Result<T>
{
  /// <summary>
  /// Инициализирует успешный результат выполнения операции.
  /// </summary>
  /// <param name="value">Значение, полученное в результате успешного выполнения.</param>
  private Result(T value)
  {
    IsSuccess = true;
    Value = value;
  }

  /// <summary>
  /// Инициализирует результат выполнения операции с ошибкой.
  /// </summary>
  /// <param name="value">Значение по умолчанию или частичный результат.</param>
  /// <param name="error">Сообщение об ошибке, описывающее причину неудачи.</param>
  private Result(T value, string error)
  {
    IsSuccess = false;
    Value = value;
    Error = error;
  }

  /// <summary>
  /// Признак, что операция завершилась успешно.
  /// </summary>
  /// <value>
  /// <c>true</c>, если операция выполнена успешно; иначе — <c>false</c>.
  /// </value>
  public bool IsSuccess { get; }

  /// <summary>
  /// Возвращаемое значение при результате.
  /// </summary>
  /// <value>
  /// Значение результата, если операция выполнена успешно; иначе — <c>null</c>.
  /// </value>
  public T? Value { get; }

  /// <summary>
  /// Сообщение об ошибке при неуспешном результате.
  /// Равно <c>null</c>, если операция выполнена успешно.
  /// </summary>
  /// <value>
  /// Текст ошибки или <c>null</c>, если операция выполнена успешно.
  /// </value>
  public string? Error { get; }

  /// <summary>
  /// Создать успешный результат с указанным значением.
  /// </summary>
  /// <param name="value">Значение результата.</param>
  /// <returns>Экземпляр <see cref="Result{T}"/> со статусом успеха.</returns>
  public static Result<T> Success(T value) => new(value);

  /// <summary>
  /// Создать неуспешный результат с указанным описанием ошибки.
  /// </summary>
  /// <param name="value">Значение результата.</param>
  /// <param name="error">Сообщение об ошибке.</param>
  /// <returns>Экземпляр <see cref="Result{T}"/> со статусом ошибки.</returns>
  public static Result<T> Failure(T value, string error) => new(value, error);
}
