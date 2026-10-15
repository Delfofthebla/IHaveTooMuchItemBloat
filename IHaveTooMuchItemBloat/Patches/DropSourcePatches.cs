using System;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;

namespace IHaveTooMuchItemBloat.Patches;

internal static class DropSourcePatches
{
    private static Settings _settings;

    public static void Install(Settings settings)
    {
        _settings = settings;

        Hooks.Install(typeof(ChestBehavior), nameof(ChestBehavior.ItemDrop), [], DropFromChest);
        Hooks.Install(typeof(OptionChestBehavior), nameof(OptionChestBehavior.ItemDrop), [], DropFromChoiceContainer);
        Hooks.Install(typeof(ShrineChanceBehavior), nameof(ShrineChanceBehavior.AddShrineStack), [typeof(Interactor)], DropFromShrineOfChance);
        Hooks.Install(typeof(ShopTerminalBehavior), nameof(ShopTerminalBehavior.DropPickup), [typeof(bool)], DropFromShopTerminal);
    }

    private static void DropFromChest(Action<ChestBehavior> orig, ChestBehavior self)
        => DropRecipient.During(_settings.ChestsGrantWishes, () => ChestOpener(self), () => self.dropTable, () => orig(self));

    private static void DropFromChoiceContainer(Action<OptionChestBehavior> orig, OptionChestBehavior self)
        => DropRecipient.During(_settings.ChoiceContainersGrantWishes, () => DropRecipient.LastPurchaserOf(self), () => self.dropTable, () => orig(self));

    private static void DropFromShrineOfChance(Action<ShrineChanceBehavior, Interactor> orig, ShrineChanceBehavior self, Interactor activator)
        => DropRecipient.During(_settings.ShrinesOfChanceGrantWishes, () => Players.MasterOf(activator), () => ShrineOfChanceTable(self), () => orig(self, activator));

    private static void DropFromShopTerminal(Action<ShopTerminalBehavior, bool> orig, ShopTerminalBehavior self, bool isDuplicated)
        => DropRecipient.During(_settings.HiddenShopItemsGrantWishes, () => HiddenItemBuyer(self, isDuplicated), () => self.dropTable, () => orig(self, isDuplicated));

    private static CharacterMaster ChestOpener(ChestBehavior chest) => IsDelusionGuess(chest)
        ? null
        : DropRecipient.LastPurchaserOf(chest);

    private static bool IsDelusionGuess(ChestBehavior chest)
        => DelusionChestController.isDelusionEnable && chest.TryGetComponent(out DelusionChestController delusion) && delusion.SelectedPickupIndex != PickupIndex.none;

    private static PickupDropTable ShrineOfChanceTable(ShrineChanceBehavior shrine) => shrine.chanceDollWin ? shrine.chanceDollDropTable : shrine.dropTable;

    private static CharacterMaster HiddenItemBuyer(ShopTerminalBehavior terminal, bool isDuplicated) => !isDuplicated && terminal.pickupIndexIsHidden
        ? DropRecipient.LastPurchaserOf(terminal)
        : null;
}
