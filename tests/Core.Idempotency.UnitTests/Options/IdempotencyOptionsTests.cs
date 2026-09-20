using Core.Idempotency.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Core.Idempotency.UnitTests.Options;

public sealed class IdempotencyOptionsTests
{
    [Fact]
    public void AddCacheableStatusCodes_Should_Add_StatusCodes()
    {
        // Arrange
        var options = new IdempotencyOptions();

        // Act
        options.AddCacheableStatusCodes(
            StatusCodes.Status404NotFound,
            StatusCodes.Status409Conflict);

        // Assert
        options.CacheableStatusCodes.Should().Contain(
            StatusCodes.Status404NotFound);

        options.CacheableStatusCodes.Should().Contain(
            StatusCodes.Status409Conflict);
    }

    [Fact]
    public void AddCacheableStatusCodes_Should_Not_Add_Duplicates()
    {
        // Arrange
        var options = new IdempotencyOptions();

        // Act
        options.AddCacheableStatusCodes(
            StatusCodes.Status200OK,
            StatusCodes.Status200OK);

        // Assert
        options.CacheableStatusCodes.Count(x =>
            x == StatusCodes.Status200OK)
            .Should().Be(1);
    }

    [Fact]
    public void RemoveCacheableStatusCodes_Should_Remove_StatusCodes()
    {
        // Arrange
        var options = new IdempotencyOptions();

        // Act
        options.RemoveCacheableStatusCodes(
            StatusCodes.Status200OK,
            StatusCodes.Status201Created);

        // Assert
        options.CacheableStatusCodes.Should().NotContain(
            StatusCodes.Status200OK);

        options.CacheableStatusCodes.Should().NotContain(
            StatusCodes.Status201Created);
    }

    [Fact]
    public void RemoveCacheableStatusCodes_Should_Ignore_StatusCodes_Not_Present()
    {
        // Arrange
        var options = new IdempotencyOptions();

        // Act
        var action = () =>
            options.RemoveCacheableStatusCodes(999);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void AddAllowedMethods_Should_Add_Methods()
    {
        // Arrange
        var options = new IdempotencyOptions();

        // Act
        options.AddAllowedMethods("PATCH", "DELETE");

        // Assert
        options.AllowedMethods.Should().Contain("PATCH");
        options.AllowedMethods.Should().Contain("DELETE");
    }

    [Fact]
    public void AddAllowedMethods_Should_Convert_Methods_To_Uppercase()
    {
        // Arrange
        var options = new IdempotencyOptions();

        // Act
        options.AddAllowedMethods("patch", "delete");

        // Assert
        options.AllowedMethods.Should().Contain("PATCH");
        options.AllowedMethods.Should().Contain("DELETE");
        options.AllowedMethods.Should().NotContain("patch");
        options.AllowedMethods.Should().NotContain("delete");
    }

    [Fact]
    public void RemoveAllowedMethods_Should_Remove_Methods()
    {
        // Arrange
        var options = new IdempotencyOptions();

        // Act
        options.RemoveAllowedMethods("POST", "PUT");

        // Assert
        options.AllowedMethods.Should().NotContain("POST");
        options.AllowedMethods.Should().NotContain("PUT");
    }
}