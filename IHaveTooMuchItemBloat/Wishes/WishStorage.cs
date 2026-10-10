using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine.Networking;

namespace IHaveTooMuchItemBloat.Wishes;

internal static class WishStorage
{
    public const int CAPACITY = 16;

    private const float DesperationScale = 100f;

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

    private static readonly ItemDef[] PickupEntries = new ItemDef[CAPACITY];
    private static readonly ItemDef[] DesperationEntries = new ItemDef[CAPACITY];

    public static void Register()
    {
        for (var i = 0; i < CAPACITY; i++)
        {
            PickupEntries[i] = RegisterEntry($"IHaveTooMuchItemBloatWishEntry{i + 1}");
            DesperationEntries[i] = RegisterEntry($"IHaveTooMuchItemBloatWishDesperation{i + 1}");
        }
    }

    public static List<Wish> Read(Inventory inventory)
    {
        var wishes = new List<Wish>();

        for (var i = 0; i < CAPACITY; i++)
        {
            var pickup = new PickupIndex(inventory.GetItemCountPermanent(PickupEntries[i]) - 1);
            if (PickupCatalog.GetPickupDef(pickup) != null)
                wishes.Add(new Wish(pickup, inventory.GetItemCountPermanent(DesperationEntries[i]) / DesperationScale));
        }

        return wishes;
    }

    public static void Write(Inventory inventory, IReadOnlyList<Wish> wishes)
    {
        if (!NetworkServer.active)
            return;

        for (var i = 0; i < CAPACITY; i++)
        {
            var wish = i < wishes.Count ? wishes[i] : new Wish(PickupIndex.none, 0f);

            SetCount(inventory, PickupEntries[i], wish.Pickup.value + 1);
            SetCount(inventory, DesperationEntries[i], (int)Math.Round(wish.Desperation * DesperationScale));
        }
    }

    private static ItemDef RegisterEntry(string name)
    {
        var entry = new CustomItem(name, Tokens.WISH_ENTRY_NAME, "", "", "", null!, null!, EntryTags, ItemTier.NoTier, hidden: true, canRemove: false, null!, []);

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
