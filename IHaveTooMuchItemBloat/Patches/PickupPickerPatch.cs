using System;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;

namespace IHaveTooMuchItemBloat.Patches;

internal static class PickupPickerPatch
{
    private static WishChoices _choices;

    public static void Install(WishChoices choices)
    {
        _choices = choices;

        Hooks.Install(typeof(PickupPickerController), nameof(PickupPickerController.SetOptionsServer), [typeof(PickupPickerController.Option[])], AttachWishChoice);
    }

    private static void AttachWishChoice(Action<PickupPickerController, PickupPickerController.Option[]> orig, PickupPickerController self, PickupPickerController.Option[] newOptions)
    {
        orig(self, newOptions);

        Failsafe.Run("attaching a wish choice", () => _choices.Attach(self, newOptions));
    }
}
