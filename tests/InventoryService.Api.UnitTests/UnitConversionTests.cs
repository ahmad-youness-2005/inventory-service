using InventoryService.Api.Errors;
using InventoryService.Api.Services;

namespace InventoryService.Api.UnitTests;

public sealed class UnitConversionTests
{
    [Theory]
    [InlineData(2, 24, 48)]
    [InlineData(1, 24, 24)]
    [InlineData(5, 1, 5)]
    [InlineData(int.MaxValue, 1, int.MaxValue)]
    public void ToPieces_ValidQuantity_ReturnsQuantityTimesUnitSize(int quantity, int quantityPerUnit, int expected)
    {
        Assert.Equal(expected, UnitConversion.ToPieces(quantity, quantityPerUnit));
    }

    [Theory]
    [InlineData(int.MaxValue, 2)]
    [InlineData(100_000_000, 24)]
    public void ToPieces_ResultAboveIntMax_ThrowsBadRequest(int quantity, int quantityPerUnit)
    {
        var exception = Assert.Throws<BadRequestException>(() => UnitConversion.ToPieces(quantity, quantityPerUnit));

        Assert.Equal("Quantity is too large.", exception.Message);
    }

    [Theory]
    [InlineData(0, 24)]
    [InlineData(-1, 24)]
    [InlineData(2, 0)]
    [InlineData(2, -24)]
    public void ToPieces_ZeroOrNegativeInput_Throws(int quantity, int quantityPerUnit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnitConversion.ToPieces(quantity, quantityPerUnit));
    }

    [Theory]
    [InlineData(245, 24, 10, 5)]
    [InlineData(240, 24, 10, 0)]
    [InlineData(5, 24, 0, 5)]
    [InlineData(0, 24, 0, 0)]
    [InlineData(7, 1, 7, 0)]
    public void Split_Pieces_ReturnsFullUnitsAndRemainder(int pieces, int quantityPerUnit, int expectedUnits, int expectedRemainder)
    {
        var (fullUnits, remainingPieces) = UnitConversion.Split(pieces, quantityPerUnit);

        Assert.Equal(expectedUnits, fullUnits);
        Assert.Equal(expectedRemainder, remainingPieces);
    }

    [Fact]
    public void Split_ZeroUnitSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnitConversion.Split(10, 0));
    }
}
