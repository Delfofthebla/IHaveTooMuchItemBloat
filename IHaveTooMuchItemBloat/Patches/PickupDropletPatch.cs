using System;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;
using RoR2.Artifacts;
using UnityEngine;

namespace IHaveTooMuchItemBloat.Patches;

internal static class PickupDropletPatch
{
    private static WishGranter _granter;

    public static void Install(WishGranter granter)
    {
        _granter = granter;

        Hooks.Install(
            typeof(PickupDropletController),
            nameof(PickupDropletController.CreatePickupDroplet),
            [typeof(GenericPickupController.CreatePickupInfo), typeof(Vector3), typeof(Vector3)],
            ApplyRecipientsWishes);
    }

    private static void ApplyRecipientsWishes(
        Action<GenericPickupController.CreatePickupInfo, Vector3, Vector3> orig,
        GenericPickupController.CreatePickupInfo pickupInfo,
        Vector3 position,
        Vector3 velocity)
    {
        if (DropRecipient.Current)
        {
            var info = pickupInfo;
            pickupInfo = Failsafe.Run("applying wishes to a drop", () => WithRecipientsWishes(info), pickupInfo);
        }

        orig(pickupInfo, position, velocity);
    }

    private static GenericPickupController.CreatePickupInfo WithRecipientsWishes(GenericPickupController.CreatePickupInfo pickupInfo)
    {
        if (CommandArtifactManager.IsCommandArtifactEnabled)
            return pickupInfo;

        return _granter.ApplyWishes(DropRecipient.Current, DropRecipient.CurrentTable, pickupInfo);
    }
}
