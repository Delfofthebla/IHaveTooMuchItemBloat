using System;
using System.Linq;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace IHaveTooMuchItemBloat.WishShrine;

internal static class WishShrinePurchase
{
    private static bool _paying;

    public static bool Install()
    {
        bool[] installed =
        [
            Hooks.Install(typeof(PurchaseInteraction), nameof(PurchaseInteraction.GetInteractability), [typeof(Interactor)], GetInteractability),
            Hooks.Install(typeof(PurchaseInteraction), nameof(PurchaseInteraction.OnInteractionBegin), [typeof(Interactor)], OpenPanelInsteadOfPaying),
            Hooks.Install(typeof(PickupPickerController), nameof(PickupPickerController.SubmitChoice), [typeof(int)], StageWishOption),
            Hooks.Install(typeof(PurchaseInteraction), nameof(PurchaseInteraction.ShouldDisplayHologram), [typeof(GameObject)], HidePriceFromSpentPlayers),
            Hooks.Install(typeof(PurchaseInteraction), nameof(PurchaseInteraction.UpdateHologramContent), [typeof(GameObject), typeof(Transform)], ShowViewersPrice),
            Hooks.Install(typeof(PingIndicator), nameof(PingIndicator.RebuildPing), [], PingWithPingersPrice),
            Hooks.Install(typeof(GlobalEventManager), nameof(GlobalEventManager.OnInteractionBegin), [typeof(Interactor), typeof(IInteractable), typeof(GameObject)], ProcOnlyOnWishes),
        ];

        return installed.All(hooked => hooked);
    }

    public static bool TryPay(PurchaseInteraction purchase, Interactor interactor, CharacterMaster master) => WithPriceFor(purchase, master, () => Pay(purchase, interactor));

    private static bool Pay(PurchaseInteraction purchase, Interactor interactor)
    {
        if (!purchase.CanBeAffordedByInteractor(interactor))
            return false;

        _paying = true;

        try
        {
            purchase.OnInteractionBegin(interactor);
            Failsafe.Run("triggering on-interaction items for a wish", () => ProcInteractionItems(purchase, interactor));
        }
        finally
        {
            _paying = false;
        }

        return true;
    }

    private static void ProcInteractionItems(PurchaseInteraction purchase, Interactor interactor)
    {
        if (GlobalEventManager.instance)
            GlobalEventManager.instance.OnInteractionBegin(interactor, purchase, purchase.gameObject);
    }

    private static Interactability GetInteractability(Func<PurchaseInteraction, Interactor, Interactability> orig, PurchaseInteraction self, Interactor activator)
    {
        if (!IsWishShrine(self))
            return orig(self, activator);

        return Failsafe.Run("checking a Shrine of Wishes' interactability", () => ShrineInteractability(orig, self, activator), Interactability.Disabled);
    }

    private static Interactability ShrineInteractability(Func<PurchaseInteraction, Interactor, Interactability> orig, PurchaseInteraction self, Interactor activator)
    {
        var shrine = self.GetComponent<WishShrineController>();
        var master = Players.MasterOf(activator);
        if (!shrine || !master || shrine.OffersNothing || shrine.IsExhaustedFor(master))
            return Interactability.Disabled;

        if (shrine.IsInUseByAnyoneBut(master))
            return Interactability.ConditionsNotMet;

        return WithPriceFor(self, master, () => orig(self, activator));
    }

    private static void OpenPanelInsteadOfPaying(Action<PurchaseInteraction, Interactor> orig, PurchaseInteraction self, Interactor activator)
    {
        if (_paying || !IsWishShrine(self))
        {
            orig(self, activator);
            return;
        }

        Failsafe.Run("opening a Shrine of Wishes", () => OpenPanel(self, activator));
    }

    private static void OpenPanel(PurchaseInteraction self, Interactor activator)
    {
        if (self.TryGetComponent(out WishShrineController shrine))
            shrine.OpenFor(activator);
    }

    private static void StageWishOption(Action<PickupPickerController, int> orig, PickupPickerController self, int choiceIndex)
    {
        if (!IsWishShrine(self))
        {
            orig(self, choiceIndex);
            return;
        }

        Failsafe.Run("staging a wish", () => StageOption(self, choiceIndex));
    }

    private static void StageOption(PickupPickerController picker, int choiceIndex)
    {
        if (picker is WishShrineController shrine)
            shrine.StageOption(choiceIndex);
    }

    private static bool HidePriceFromSpentPlayers(Func<PurchaseInteraction, GameObject, bool> orig, PurchaseInteraction self, GameObject viewer)
    {
        var visible = orig(self, viewer);
        if (!visible || !self.TryGetComponent(out WishShrineState state))
            return visible;

        var viewerMaster = Players.MasterOfBody(viewer);

        return !(viewerMaster && state.IsExhaustedFor(viewerMaster));
    }

    private static void ShowViewersPrice(Action<PurchaseInteraction, GameObject, Transform> orig, PurchaseInteraction self, GameObject hologramContent, Transform viewer)
    {
        if (!IsWishShrine(self))
        {
            orig(self, hologramContent, viewer);
            return;
        }

        var viewerMaster = Players.MasterOfBody(viewer ? viewer.gameObject : null);

        WithPriceFor(self, viewerMaster, () => orig(self, hologramContent, viewer));
    }

    private static void ProcOnlyOnWishes(
        Action<GlobalEventManager, Interactor, IInteractable, GameObject> orig,
        GlobalEventManager self,
        Interactor interactor,
        IInteractable interactable,
        GameObject interactableObject)
    {
        var openingWishShrine = !_paying && interactableObject && interactableObject.TryGetComponent(out WishShrineState _);
        if (openingWishShrine)
            return;

        orig(self, interactor, interactable, interactableObject);
    }

    private static void PingWithPingersPrice(Action<PingIndicator> orig, PingIndicator self)
    {
        var shrine = Failsafe.Run("finding a pinged Shrine of Wishes", () => PingedWishShrine(self), null);
        if (!shrine)
        {
            orig(self);
            return;
        }

        WithPriceFor(shrine, PingerOf(self), () => orig(self));
    }

    private static PurchaseInteraction PingedWishShrine(PingIndicator ping)
    {
        var purchase = ping.pingTarget
            ? ping.pingTarget.GetComponent<PurchaseInteraction>()
            : null;

        return purchase && IsWishShrine(purchase)
            ? purchase
            : null;
    }

    private static CharacterMaster PingerOf(PingIndicator ping) => ping.pingOwner
        ? ping.pingOwner.GetComponent<CharacterMaster>()
        : null;

    private static bool IsWishShrine(Component component) => component.TryGetComponent(out WishShrineState _);

    private static int PriceFor(PurchaseInteraction purchase, CharacterMaster master) => purchase.TryGetComponent(out WishShrineState state)
        ? state.PriceFor(master, purchase.cost)
        : purchase.cost;

    private static void WithPriceFor(PurchaseInteraction purchase, CharacterMaster master, Action action) => WithPriceFor(purchase, master, () =>
    {
        action();
        return true;
    });

    private static T WithPriceFor<T>(PurchaseInteraction purchase, CharacterMaster master, Func<T> action)
    {
        var shrinePrice = purchase.cost;
        purchase.cost = PriceFor(purchase, master);

        try
        {
            return action();
        }
        finally
        {
            purchase.cost = shrinePrice;
        }
    }
}
