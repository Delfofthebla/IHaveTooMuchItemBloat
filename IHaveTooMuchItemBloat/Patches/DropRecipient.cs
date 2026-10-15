using System;
using BepInEx.Configuration;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;
using UnityEngine;

namespace IHaveTooMuchItemBloat.Patches;

internal static class DropRecipient
{
    private static Func<PickupDropTable> _currentTable;

    public static CharacterMaster Current { get; private set; }

    public static PickupDropTable CurrentTable => _currentTable?.Invoke();

    public static void During(ConfigEntry<bool> sourceEnabled, Func<CharacterMaster> recipient, Func<PickupDropTable> table, Action drop)
    {
        if (!sourceEnabled.Value)
        {
            drop();
            return;
        }

        var previousRecipient = Current;
        var previousTable = _currentTable;

        Current = Failsafe.Run("finding a drop's recipient", recipient, null);
        _currentTable = table;

        try
        {
            drop();
        }
        finally
        {
            Current = previousRecipient;
            _currentTable = previousTable;
        }
    }

    public static CharacterMaster LastPurchaserOf(Component container) => container.TryGetComponent(out PurchaseInteraction purchase)
        ? Players.MasterOf(purchase.lastActivator)
        : null;
}
