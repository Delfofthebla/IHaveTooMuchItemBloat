using System;
using System.Collections.Generic;
using System.Linq;
using R2API;
using RoR2;
using UnityEngine.Networking;

namespace IHaveTooMuchItemBloat.Wishes;

internal static class WishStorage
{
    public static readonly int Capacity = 16;

    private static readonly float DesperationScale = 100f;

    private static readonly ItemTag[] EntryTags =
    [
        ItemTag.CannotSteal,
        ItemTag.CannotCopy,
        ItemTag.CannotDuplicate,
        ItemTag.AIBlacklist,
        ItemTag.BrotherBlacklist,
        ItemTag.WorldUnique,
        ItemTag.IgnoreForDropList,
    ];

    private static readonly ItemDef[] _pickupEntries = new ItemDef[Capacity];
    private static readonly ItemDef[] _desperationEntries = new ItemDef[Capacity];

    public static void Register()
    {
        for (var i = 0; i < Capacity; i++)
        {
            _pickupEntries[i] = RegisterEntry($"IHaveTooMuchItemBloatWishEntry{i + 1}");
            _desperationEntries[i] = RegisterEntry($"IHaveTooMuchItemBloatWishDesperation{i + 1}");
        }

        RuleCatalog.availability.CallWhenAvailable(() => Failsafe.Run("hiding wish storage from the rulebook", HideFromRulebook));
    }

    private static void HideFromRulebook()
    {
        foreach (var entry in _pickupEntries.Concat(_desperationEntries))
        {
            var rule = RuleCatalog.FindRuleDef("Items." + entry.name);
            if (rule != null)
                rule.hideLobbyDisplay = true;
        }
    }

    public static List<Wish> Read(Inventory inventory)
    {
        var wishes = new List<Wish>();

        for (var i = 0; i < Capacity; i++)
        {
            var pickup = new PickupIndex(inventory.GetItemCountPermanent(_pickupEntries[i]) - 1);
            if (PickupCatalog.GetPickupDef(pickup) != null)
                wishes.Add(new Wish(pickup, inventory.GetItemCountPermanent(_desperationEntries[i]) / DesperationScale));
        }

        return wishes;
    }

    public static void Write(Inventory inventory, IReadOnlyList<Wish> wishes)
    {
        if (!NetworkServer.active)
            return;

        for (var i = 0; i < Capacity; i++)
        {
            var wish = i < wishes.Count
                ? wishes[i]
                : new Wish(PickupIndex.none, 0f);

            SetCount(inventory, _pickupEntries[i], wish.Pickup.value + 1);
            SetCount(inventory, _desperationEntries[i], (int)Math.Round(wish.Desperation * DesperationScale));
        }
    }

    private static ItemDef RegisterEntry(string name)
    {
        var entry = new CustomItem(name, Tokens.WishEntryName, "", "", "", null!, null!, EntryTags, ItemTier.NoTier, hidden: true, canRemove: false, null!, []);

        // R2API's CustomItem has no tier definition for NoTier and leaves the item at Tier1.
#pragma warning disable CS0618
        entry.ItemDef.deprecatedTier = ItemTier.NoTier;
#pragma warning restore CS0618

        ItemAPI.Add(entry);
        return entry.ItemDef;
    }

    private static void SetCount(Inventory inventory, ItemDef entry, int wanted)
    {
        var stored = inventory.GetItemCountPermanent(entry);

        if (wanted > stored)
            inventory.GiveItemPermanent(entry, wanted - stored);
        else if (wanted < stored)
            inventory.RemoveItemPermanent(entry, stored - wanted);
    }
}
