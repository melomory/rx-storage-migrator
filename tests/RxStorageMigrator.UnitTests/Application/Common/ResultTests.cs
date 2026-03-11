using FluentAssertions;
using RxStorageMigrator.Application.Common;
using Xunit;

namespace RxStorageMigrator.UnitTests.Application.Common;

public class ResultTests
{
  [Fact]
  public void Success_ShouldSetIsSuccessTrue_AndContainValue()
  {
    const int value = 42;

    var result = Result<int>.Success(value);

    result.IsSuccess.Should().BeTrue();
    result.Value.Should().Be(value);
    result.Error.Should().BeNull();
  }

  [Fact]
  public void Failure_ShouldSetsSuccessFalse_AndContainValueAndContainError()
  {
    const int value = 42;
    const string error = "error";

    var result = Result<int>.Failure(value, error);

    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Be(error);
    result.Value.Should().Be(value);
  }
}
