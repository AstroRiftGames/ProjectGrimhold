/// <summary>
/// Evaluates the numeric rules for aggregated temporary loot stacks without
/// owning inventory state or depending on Unity or Fusion.
/// </summary>
public static class LootInventoryRules
{
    /// <summary>
    /// Determines whether gameplay capacity fits within its technical representation.
    /// </summary>
    public static bool IsValidSlotCapacity(int slotCapacity, int maximumRepresentableSlots)
    {
        return slotCapacity > 0 && slotCapacity <= maximumRepresentableSlots;
    }

    /// <summary>
    /// Validates whether a complete quantity can be added to an aggregated stack.
    /// </summary>
    public static LootTransferFailureReason ValidateReceive(
        bool alreadyHeld,
        int currentAmount,
        int occupiedSlotCount,
        int slotCapacity,
        int requestedAmount)
    {
        if (requestedAmount <= 0)
        {
            return LootTransferFailureReason.InvalidAmount;
        }

        if (slotCapacity <= 0 || occupiedSlotCount < 0 || occupiedSlotCount > slotCapacity)
        {
            return LootTransferFailureReason.ContainerUnavailable;
        }

        if (alreadyHeld)
        {
            if (currentAmount <= 0)
            {
                return LootTransferFailureReason.ContainerUnavailable;
            }

            return currentAmount > int.MaxValue - requestedAmount
                ? LootTransferFailureReason.Overflow
                : LootTransferFailureReason.None;
        }

        if (currentAmount != 0)
        {
            return LootTransferFailureReason.ContainerUnavailable;
        }

        return occupiedSlotCount >= slotCapacity
            ? LootTransferFailureReason.InventoryFull
            : LootTransferFailureReason.None;
    }

    /// <summary>
    /// Validates whether a complete quantity can be removed from an aggregated stack.
    /// </summary>
    public static LootTransferFailureReason ValidateExtraction(
        bool alreadyHeld,
        int currentAmount,
        int requestedAmount)
    {
        if (requestedAmount <= 0)
        {
            return LootTransferFailureReason.InvalidAmount;
        }

        if (!alreadyHeld)
        {
            return LootTransferFailureReason.InsufficientAmount;
        }

        if (currentAmount <= 0)
        {
            return LootTransferFailureReason.ContainerUnavailable;
        }

        return requestedAmount > currentAmount
            ? LootTransferFailureReason.InsufficientAmount
            : LootTransferFailureReason.None;
    }

    /// <summary>
    /// Calculates the stored amount after a reception that has already been validated.
    /// </summary>
    public static int CalculateReceivedAmount(int currentAmount, int requestedAmount)
    {
        return checked(currentAmount + requestedAmount);
    }

    /// <summary>
    /// Calculates the stored amount after an extraction that has already been validated.
    /// Zero means that the caller must remove the stack.
    /// </summary>
    public static int CalculateRemainingAmount(int currentAmount, int requestedAmount)
    {
        return checked(currentAmount - requestedAmount);
    }
}
