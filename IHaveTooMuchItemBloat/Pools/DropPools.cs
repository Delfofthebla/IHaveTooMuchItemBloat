using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;

namespace IHaveTooMuchItemBloat.Pools;

internal static class DropPools
{
    public static readonly DropPool Common = Items("Common", ColorCatalog.ColorIndex.Tier1Item, ItemTier.Tier1, run => run.availableTier1DropList);
    public static readonly DropPool Uncommon = Items("Uncommon", ColorCatalog.ColorIndex.Tier2Item, ItemTier.Tier2, run => run.availableTier2DropList);
    public static readonly DropPool Legendary = Items("Legendary", ColorCatalog.ColorIndex.Tier3Item, ItemTier.Tier3, run => run.availableTier3DropList);
    public static readonly DropPool Boss = Items("Boss", ColorCatalog.ColorIndex.BossItem, ItemTier.Boss, run => run.availableBossDropList);
    public static readonly DropPool Lunar = Items("Lunar", ColorCatalog.ColorIndex.LunarItem, ItemTier.Lunar, run => run.availableLunarItemDropList);
    public static readonly DropPool VoidCommon = Items("Void Common", ColorCatalog.ColorIndex.VoidItem, ItemTier.VoidTier1, run => run.availableVoidTier1DropList);
    public static readonly DropPool VoidUncommon = Items("Void Uncommon", ColorCatalog.ColorIndex.VoidItem, ItemTier.VoidTier2, run => run.availableVoidTier2DropList);
    public static readonly DropPool VoidLegendary = Items("Void Legendary", ColorCatalog.ColorIndex.VoidItem, ItemTier.VoidTier3, run => run.availableVoidTier3DropList);
    public static readonly DropPool VoidBoss = Items("Void Boss", ColorCatalog.ColorIndex.VoidItem, ItemTier.VoidBoss, run => run.availableVoidBossDropList);

    public static readonly DropPool Equipment = new(
        "Equipment",
        ColorCatalog.ColorIndex.Equipment,
        run => run.availableEquipmentDropList,
        pickup => pickup.equipmentIndex != EquipmentIndex.None && !pickup.isLunar);

    public static readonly DropPool LunarEquipment = new(
        "Lunar Equipment",
        ColorCatalog.ColorIndex.LunarItem,
        run => run.availableLunarEquipmentDropList,
        pickup => pickup.equipmentIndex != EquipmentIndex.None && pickup.isLunar);

    public static IReadOnlyList<DropPool> All { get; } = [Common, Uncommon, Legendary, Boss, Lunar, VoidCommon, VoidUncommon, VoidLegendary, VoidBoss, Equipment, LunarEquipment];

    public static IReadOnlyList<DropPool> Wishable { get; } = [Common, Uncommon, Legendary, Lunar, VoidCommon, VoidUncommon, VoidLegendary, Equipment, LunarEquipment];

    public static DropPool Containing(Run run, PickupIndex pickup) => All.FirstOrDefault(pool => pool.AvailableIn(run).Contains(pickup));

    public static DropPool TierOf(PickupIndex pickup) => All.FirstOrDefault(pool => pool.Matches(pickup));

    public static int SortOrderOf(PickupIndex pickup) => All.TakeWhile(pool => !pool.Matches(pickup)).Count();

    private static DropPool Items(string name, ColorCatalog.ColorIndex color, ItemTier tier, Func<Run, List<PickupIndex>> availablePickups)
        => new(name, color, availablePickups, pickup => pickup.itemIndex != ItemIndex.None && pickup.itemTier == tier);
}
