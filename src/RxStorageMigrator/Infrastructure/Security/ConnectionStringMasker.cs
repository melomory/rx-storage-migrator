using Microsoft.Data.SqlClient;

namespace RxStorageMigrator.Infrastructure.Security;

/// <summary>
/// Предоставляет методы для маскирования конфиденциальных данных в строках подключения.
/// </summary>
public static class ConnectionStringMasker
{
  /// <summary>
  /// Значение для маскировки.
  /// </summary>
  private const string MaskValue = "***";

  /// <summary>
  /// Вернуть строку подключения с замаскированными чувствительными параметрами.
  /// </summary>
  /// <param name="connectionString">Исходная строка подключения.</param>
  /// <returns>
  /// Строка подключения, безопасная для логирования.
  /// Если строка не может быть разобрана, возвращается исходное значение.
  /// </returns>
  /// <remarks>
  /// Маскируются:
  /// <list type="bullet">
  /// <item><description>Password / Pwd</description></item>
  /// <item><description>User ID / UID</description></item>
  /// </list>
  /// Если используется Integrated Security, учётные данные не выводятся.
  /// Persist Security Info принудительно устанавливается в false.
  /// </remarks>
  public static string Mask(string connectionString)
  {
    if (string.IsNullOrWhiteSpace(connectionString))
      return connectionString;

    try
    {
      var builder = new SqlConnectionStringBuilder(connectionString) { PersistSecurityInfo = false };

      if (builder.IntegratedSecurity)
        return builder.ToString();

      if (!string.IsNullOrEmpty(builder.UserID))
        builder.UserID = MaskValue;

      if (!string.IsNullOrEmpty(builder.Password))
        builder.Password = MaskValue;

      return builder.ToString();
    }
    catch
    {
      return connectionString;
    }
  }
}
