using InventoryService.Api.Errors;

namespace InventoryService.Api.Services;

public static class UnitConversion
{
    public static int ToPieces(int quantity, int quantityPerUnit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityPerUnit);

        var pieces = (long)quantity * quantityPerUnit;
        if (pieces > int.MaxValue)
            throw new BadRequestException("Quantity is too large.");

        return (int)pieces;
    }

    public static (int FullUnits, int RemainingPieces) Split(int pieces, int quantityPerUnit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pieces);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityPerUnit);

        return (pieces / quantityPerUnit, pieces % quantityPerUnit);
    }
}
