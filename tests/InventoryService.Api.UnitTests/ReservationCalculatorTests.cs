using InventoryService.Api.Dtos;
using InventoryService.Api.Entities;
using InventoryService.Api.Errors;
using InventoryService.Api.Services;

namespace InventoryService.Api.UnitTests;

public sealed class ReservationCalculatorTests
{
    private static readonly Guid ItemA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid ItemB = Guid.Parse("00000000-0000-0000-0000-00000000000b");

    [Fact]
    public void Merge_SameItemTwice_AddsQuantities()
    {
        var merged = ReservationCalculator.Merge([new(ItemA, 3), new(ItemB, 7), new(ItemA, 2)]);

        Assert.Equal(new[] { new RequestedLine(ItemA, 5), new RequestedLine(ItemB, 7) }, merged);
    }

    [Fact]
    public void Merge_UnsortedLines_ReturnsLinesSortedByItemId()
    {
        var merged = ReservationCalculator.Merge([new(ItemB, 1), new(ItemA, 1)]);

        Assert.Equal(new[] { ItemA, ItemB }, merged.Select(l => l.ItemId));
    }

    [Fact]
    public void Merge_TotalAboveIntMax_ThrowsBadRequest()
    {
        Assert.Throws<BadRequestException>(() =>
            ReservationCalculator.Merge([new(ItemA, int.MaxValue), new(ItemA, 1)]));
    }

    [Fact]
    public void HasSameLines_SameLinesInDifferentOrder_ReturnsTrue()
    {
        var existing = new[] { new ReservationLineResponse(ItemB, 7), new ReservationLineResponse(ItemA, 5) };
        var requested = ReservationCalculator.Merge([new(ItemA, 5), new(ItemB, 7)]);

        Assert.True(ReservationCalculator.HasSameLines(existing, requested));
    }

    [Fact]
    public void HasSameLines_DifferentQuantity_ReturnsFalse()
    {
        var existing = new[] { new ReservationLineResponse(ItemA, 5) };
        var requested = ReservationCalculator.Merge([new(ItemA, 6)]);

        Assert.False(ReservationCalculator.HasSameLines(existing, requested));
    }

    [Fact]
    public void HasSameLines_DifferentItem_ReturnsFalse()
    {
        var existing = new[] { new ReservationLineResponse(ItemA, 5) };
        var requested = ReservationCalculator.Merge([new(ItemB, 5)]);

        Assert.False(ReservationCalculator.HasSameLines(existing, requested));
    }

    [Fact]
    public void HasSameLines_ExtraLine_ReturnsFalse()
    {
        var existing = new[] { new ReservationLineResponse(ItemA, 5) };
        var requested = ReservationCalculator.Merge([new(ItemA, 5), new(ItemB, 1)]);

        Assert.False(ReservationCalculator.HasSameLines(existing, requested));
    }

    [Theory]
    [InlineData(ReservationStatus.Pending, 0, 4)]
    [InlineData(ReservationStatus.Confirmed, -4, -4)]
    [InlineData(ReservationStatus.Released, 0, -4)]
    [InlineData(ReservationStatus.Expired, 0, -4)]
    public void StockChangeFor_EachStatus_ReturnsCorrectSigns(ReservationStatus status, int expectedQuantityChange, int expectedReservedChange)
    {
        var change = ReservationCalculator.StockChangeFor(status, ItemA, 4);

        Assert.Equal(new StockChange(ItemA, expectedQuantityChange, expectedReservedChange), change);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending, StockMovementType.Reserved)]
    [InlineData(ReservationStatus.Confirmed, StockMovementType.Confirmed)]
    [InlineData(ReservationStatus.Released, StockMovementType.Released)]
    [InlineData(ReservationStatus.Expired, StockMovementType.Expired)]
    public void MovementTypeFor_EachStatus_ReturnsMatchingType(ReservationStatus status, StockMovementType expected)
    {
        Assert.Equal(expected, ReservationCalculator.MovementTypeFor(status));
    }

    [Fact]
    public void ReserveThenConfirm_NetChange_TakesPiecesOffTheShelfAndLeavesNothingReserved()
    {
        var reserved = ReservationCalculator.StockChangeFor(ReservationStatus.Pending, ItemA, 4);
        var confirmed = ReservationCalculator.StockChangeFor(ReservationStatus.Confirmed, ItemA, 4);

        Assert.Equal(-4, reserved.QuantityChange + confirmed.QuantityChange);
        Assert.Equal(0, reserved.ReservedChange + confirmed.ReservedChange);
    }

    [Theory]
    [InlineData(ReservationStatus.Released)]
    [InlineData(ReservationStatus.Expired)]
    public void ReserveThenCancel_NetChange_IsZero(ReservationStatus finalStatus)
    {
        var reserved = ReservationCalculator.StockChangeFor(ReservationStatus.Pending, ItemA, 4);
        var cancelled = ReservationCalculator.StockChangeFor(finalStatus, ItemA, 4);

        Assert.Equal(0, reserved.QuantityChange + cancelled.QuantityChange);
        Assert.Equal(0, reserved.ReservedChange + cancelled.ReservedChange);
    }
}
