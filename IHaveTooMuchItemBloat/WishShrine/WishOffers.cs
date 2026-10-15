using System.Collections.Generic;
using System.Linq;
using IHaveTooMuchItemBloat.Pools;
using RoR2;

namespace IHaveTooMuchItemBloat.WishShrine;

internal static class WishOffers
{
    public static Xoroshiro128Plus NewShrineRng() => new(StageRng.ShrineSeeds?.nextUlong ?? 0);

    public static bool CanOfferAnything(Settings settings, Run run) => run && DropPools.Wishable.Any(pool => settings.OfferWeight(pool) > 0f && pool.AvailableIn(run).Count > 0);

    public static PickupIndex[] Roll(Settings settings, Run run, Xoroshiro128Plus shrineRng, int playerIndex)
    {
        var rng = PlayerStream(shrineRng, playerIndex);
        var remaining = DropPools.Wishable.Where(pool => settings.OfferWeight(pool) > 0f).ToDictionary(pool => pool, pool => AvailableInOrder(pool, run));

        var offer = new List<PickupIndex>();

        while (offer.Count < settings.WishShrineOfferSize.Value)
        {
            var tier = PickTier(settings, remaining, rng);
            if (tier == null)
                break;

            var pickups = remaining[tier];
            var pick = rng.RangeInt(0, pickups.Count);

            offer.Add(pickups[pick]);
            pickups.RemoveAt(pick);
        }

        return [.. offer.OrderBy(DropPools.SortOrderOf).ThenBy(PickupNames.Display)];
    }

    public static PickupPickerController.Option[] AsOptions(IEnumerable<PickupIndex> offer)
        => offer.Select(pickup => new PickupPickerController.Option { pickup = new UniquePickup(pickup), available = true }).ToArray();

    private static List<PickupIndex> AvailableInOrder(DropPool pool, Run run) => pool.AvailableIn(run).OrderBy(pickup => pickup.value).ToList();

    private static Xoroshiro128Plus PlayerStream(Xoroshiro128Plus shrineRng, int playerIndex)
    {
        var rng = new Xoroshiro128Plus(shrineRng);
        for (var i = 0; i < playerIndex; i++)
            rng.Jump();

        return rng;
    }

    private static DropPool PickTier(Settings settings, Dictionary<DropPool, List<PickupIndex>> remaining, Xoroshiro128Plus rng)
    {
        var tiers = new WeightedSelection<DropPool>();

        foreach (var tier in remaining.Where(tier => tier.Value.Count > 0))
            tiers.AddChoice(tier.Key, settings.OfferWeight(tier.Key));

        return tiers.Count > 0
            ? tiers.Evaluate(rng.nextNormalizedFloat)
            : null;
    }
}
