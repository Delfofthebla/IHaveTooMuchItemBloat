using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.ContentManagement;

namespace IHaveTooMuchItemBloat.Pools;

internal static class VanillaContent
{
    private static readonly string LegacyModContentPack = "RoR2.LegacyModContent";
    private static readonly string VanillaPackPrefix = "RoR2.";

    private static Dictionary<PickupIndex, string> _contentPackOf;

    public static IEnumerable<string> ContentPacks => ContentPackOfPickup.Values.Distinct();
    public static bool Contains(PickupIndex pickup) => ContentPackOf(pickup) != null;
    public static string ContentPackOf(PickupIndex pickup) => ContentPackOfPickup.GetValueOrDefault(pickup);
    private static Dictionary<PickupIndex, string> ContentPackOfPickup => _contentPackOf ??= MapVanillaPickups();

    private static Dictionary<PickupIndex, string> MapVanillaPickups()
    {
        var contentPackOf = new Dictionary<PickupIndex, string>();

        foreach (var contentPack in ContentManager.allLoadedContentPacks.Where(IsVanilla))
        {
            foreach (var item in contentPack.itemDefs)
                contentPackOf[PickupCatalog.FindPickupIndex(item.itemIndex)] = contentPack.identifier;

            foreach (var equipment in contentPack.equipmentDefs)
                contentPackOf[PickupCatalog.FindPickupIndex(equipment.equipmentIndex)] = contentPack.identifier;
        }

        return contentPackOf;
    }

    private static bool IsVanilla(ReadOnlyContentPack contentPack) => contentPack.identifier.StartsWith(VanillaPackPrefix) && contentPack.identifier != LegacyModContentPack;
}
