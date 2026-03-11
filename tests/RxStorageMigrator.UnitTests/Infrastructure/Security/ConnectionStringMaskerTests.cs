using FluentAssertions;
using RxStorageMigrator.Infrastructure.Security;

namespace RxStorageMigrator.UnitTests.Infrastructure.Security;

public class ConnectionStringMaskerTests
{
  [Fact]
  public void Mask_WhenUserIdAndPasswordPresent_ShouldMaskThem()
  {
    const string input = "Data Source=.;Initial Catalog=Test;User ID=admin;Password=secret;";

    var result = ConnectionStringMasker.Mask(input);

    result.Should().NotContain("admin");
    result.Should().NotContain("secret");
    result.Should().Contain("User ID=***");
    result.Should().Contain("Password=***");
  }

  [Fact]
  public void Mask_WhenOnlyUserIdPresent_ShouldMaskOnlyUserId()
  {
    const string input = "Server=.;User ID=admin;";

    var result = ConnectionStringMasker.Mask(input);

    result.Should().NotContain("admin");
  }

  [Fact]
  public void Mask_WhenOnlyPasswordPresent_ShouldMaskOnlyPassword()
  {
    const string input = "Server=.;Password=secret;";

    var result = ConnectionStringMasker.Mask(input);

    result.Should().NotContain("secret");
  }

  [Fact]
  public void Mask_WhenEmpty_ShouldReturnUnchanged()
  {
    var result = ConnectionStringMasker.Mask(string.Empty);

    result.Should().BeEmpty();
  }

  [Fact]
  public void Mask_WhenNull_ShouldReturnNull()
  {
    string? input = null;

    var result = ConnectionStringMasker.Mask(input!);

    result.Should().BeNull();
  }

  [Fact]
  public void Mask_WhenWhitespace_ShouldReturnUnchanged()
  {
    const string input = "   ";

    var result = ConnectionStringMasker.Mask(input);

    result.Should().Be(input);
  }

  [Fact]
  public void Mask_WhenIntegratedSecurity_ShouldNotMask()
  {
    const string input = "Server=.;Database=Test;Integrated Security=true;";

    var result = ConnectionStringMasker.Mask(input);

    result.Should().Contain("Integrated Security=True");
    result.Should().NotContain("****");
  }

  [Fact]
  public void Mask_WhenInvalidConnectionString_ShouldReturnOriginal()
  {
    const string input = "This is not a valid connection string";

    var result = ConnectionStringMasker.Mask(input);

    result.Should().Be(input);
  }
}
