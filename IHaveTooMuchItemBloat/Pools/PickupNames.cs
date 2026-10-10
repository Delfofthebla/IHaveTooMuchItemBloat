using System.Linq;
using RoR2;

namespace IHaveTooMuchItemBloat.Pools;

internal static class PickupNames
{
    public static string Display(PickupIndex pickup)
    {
        var pickupDef = PickupCatalog.GetPickupDef(pickup);

        return pickupDef != null ? Language.GetString(pickupDef.nameToken) : pickup.ToString();
    }

    public static PickupIndex Find(string name)
    {
        var wanted = Compact(name);
        var match = PickupCatalog.allPickups.FirstOrDefault(pickup => Compact(InternalName(pickup)) == wanted || Compact(Language.GetString(pickup.nameToken)) == wanted);

        return match?.pickupIndex ?? PickupIndex.none;
    }

    private static string InternalName(PickupDef pickup)
    {
        if (pickup.itemIndex != ItemIndex.None)
            return ItemCatalog.GetItemDef(pickup.itemIndex).name;

        if (pickup.equipmentIndex != EquipmentIndex.None)
            return EquipmentCatalog.GetEquipmentDef(pickup.equipmentIndex).name;

        return null;
    }

    private static string Compact(string name)
        => name == null ? null : new string(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
