using System.Collections.Generic;
using System.Linq;
using IHaveTooMuchItemBloat.Pools;
using RoR2;

namespace IHaveTooMuchItemBloat.Wishes;

internal sealed class WishGranter(Settings settings, WishRegistry wishes)
{
    private readonly HashSet<string> _unreadableTableTypes = [];

    public GenericPickupController.CreatePickupInfo ApplyWishes(CharacterMaster recipient, PickupDropTable table, GenericPickupController.CreatePickupInfo pickupInfo)
    {
        var run = Run.instance;
        var held = wishes.Of(recipient);
        if (StageRng.WishRolls == null || !run || !table || held.Count == 0)
            return pickupInfo;

        var source = ReadWeights(table);
        if (source == null)
            return pickupInfo;

        var rolled = RolledPickups(pickupInfo);
        var droppedNaturally = held.Where(wish => rolled.Contains(wish.Pickup) && source.CanDrop(wish.Pickup)).ToList();
        if (droppedNaturally.Count > 0)
        {
            ResolveNaturalDrops(run, recipient, source, held, rolled, droppedNaturally);
            return pickupInfo;
        }

        for (var slot = 0; slot < rolled.Count; slot++)
        {
            var tier = DropPools.Containing(run, rolled[slot]);
            if (tier == null)
                continue;

            var candidates = CandidatesFor(run, tier, source, held, rolled);
            if (candidates.Count > 0)
                return RollSwaps(recipient, table, pickupInfo, slot, rolled[slot], candidates);
        }

        return pickupInfo;
    }

    private List<(Wish Wish, WishOdds Odds)> CandidatesFor(Run run, DropPool tier, DropTableWeights source, IReadOnlyList<Wish> held, List<PickupIndex> rolled)
    {
        var sourceTier = source.Within(tier);
        var eligible = held.Where(wish => tier.Matches(wish.Pickup) && sourceTier.CanDrop(wish.Pickup) && !rolled.Contains(wish.Pickup)).OrderByDescending(wish => wish.Desperation).ToList();
        if (eligible.Count == 0)
            return [];

        var wholeTier = DropTableWeights.OfPool(run, tier);

        return eligible.Select(wish => (wish, new WishOdds(settings, tier, sourceTier, wholeTier, wish.Pickup))).ToList();
    }

    private GenericPickupController.CreatePickupInfo RollSwaps(
        CharacterMaster recipient,
        PickupDropTable table,
        GenericPickupController.CreatePickupInfo pickupInfo,
        int slot,
        PickupIndex rolled,
        List<(Wish Wish, WishOdds Odds)> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!RollSwap(recipient, table, rolled, candidate.Wish, candidate.Odds))
                continue;

            wishes.Grant(recipient, candidate.Wish.Pickup);
            return WithPickupAt(pickupInfo, slot, candidate.Wish.Pickup);
        }

        SpreadDesperation(recipient, candidates);
        return pickupInfo;
    }

    private bool RollSwap(CharacterMaster recipient, PickupDropTable table, PickupIndex rolled, Wish wish, WishOdds odds)
    {
        var chance = settings.AlwaysGrantWishes.Value
            ? 1f
            : odds.SwapChanceAt(wish.Desperation);
        var granted = StageRng.WishRolls.nextNormalizedFloat < chance;

        LogRoll(recipient, table, rolled, wish, odds, chance, granted);
        return granted;
    }

    private void ResolveNaturalDrops(Run run, CharacterMaster recipient, DropTableWeights source, IReadOnlyList<Wish> held, List<PickupIndex> rolled, List<Wish> droppedNaturally)
    {
        var behavior = settings.NaturalDrops.Value;

        foreach (var dropsInTier in droppedNaturally.GroupBy(wish => DropPools.TierOf(wish.Pickup)))
        {
            var tier = dropsInTier.Key;
            if (tier == null)
                continue;

            if (behavior == NaturalDropBehavior.Ignore)
                SpreadDesperation(recipient, CandidatesFor(run, tier, source, held, rolled));
            else
                GrantNaturalDrops(recipient, tier, [.. dropsInTier], behavior);
        }
    }

    private void GrantNaturalDrops(CharacterMaster recipient, DropPool tier, List<Wish> drops, NaturalDropBehavior behavior)
    {
        foreach (var wish in drops)
        {
            LogNaturalGrant(recipient, wish, behavior);

            if (behavior == NaturalDropBehavior.GrantAndWishAgain)
            {
                wishes.GrantAndRenew(recipient, wish.Pickup);
                continue;
            }

            if (behavior == NaturalDropBehavior.GrantAndPassOn)
                PassOnDesperation(recipient, tier, wish, drops);

            wishes.Grant(recipient, wish.Pickup);
        }
    }

    private void PassOnDesperation(CharacterMaster recipient, DropPool tier, Wish granted, List<Wish> drops)
    {
        var heirs = wishes.Of(recipient).Where(wish => IsHeir(wish, tier, drops)).ToList();
        if (heirs.Count == 0)
            return;

        var share = granted.Desperation * settings.PassOnPercent.Value / 100f / heirs.Count;
        foreach (var heir in heirs)
            wishes.AddDesperation(recipient, heir.Pickup, share);
    }

    private static bool IsHeir(Wish wish, DropPool tier, List<Wish> drops) => tier.Matches(wish.Pickup) && drops.All(dropped => dropped.Pickup != wish.Pickup);

    private void SpreadDesperation(CharacterMaster recipient, List<(Wish Wish, WishOdds Odds)> candidates)
    {
        foreach (var candidate in candidates)
            wishes.AddDesperation(recipient, candidate.Wish.Pickup, candidate.Odds.DesperationPerMiss / candidates.Count);
    }

    private DropTableWeights ReadWeights(PickupDropTable table)
    {
        var weights = DropTableWeights.Of(table);
        if (weights == null)
            ReportUnreadable(table);

        return weights;
    }

    private void ReportUnreadable(PickupDropTable table)
    {
        var typeName = table.GetType().FullName;
        if (_unreadableTableTypes.Add(typeName))
            IHaveTooMuchItemBloatPlugin.Log.LogInfo($"Unreadable drop table type {typeName}; its drops ignore wishes.");
    }

    private static List<PickupIndex> RolledPickups(GenericPickupController.CreatePickupInfo pickupInfo)
    {
        if (pickupInfo.pickerOptions is { Length: > 0 } options)
            return options.Select(option => option.pickup.pickupIndex).ToList();

        return [pickupInfo.pickup.pickupIndex];
    }

    private static GenericPickupController.CreatePickupInfo WithPickupAt(GenericPickupController.CreatePickupInfo pickupInfo, int slot, PickupIndex pickup)
    {
        if (pickupInfo.pickerOptions is { Length: > 0 } options)
            options[slot].pickup = options[slot].pickup.WithPickupIndex(pickup);
        else
            pickupInfo.pickup = pickupInfo.pickup.WithPickupIndex(pickup);

        return pickupInfo;
    }

    private void LogRoll(CharacterMaster recipient, PickupDropTable table, PickupIndex rolled, Wish wish, WishOdds odds, float chance, bool granted)
    {
        if (!settings.LogWishRolls.Value)
            return;

        IHaveTooMuchItemBloatPlugin.Log.LogInfo(
            $"Wish roll for {Players.NameOf(recipient)} from {table.name}: rolled {PickupNames.Display(rolled)}; "
                + $"wish for {PickupNames.Display(wish.Pickup)} at {wish.Desperation:0.##} desperation of {odds.GuaranteedAt:0.##} (natural {odds.NaturalChance:P2}, vanilla {odds.VanillaChance:P2}, "
                + $"wish {odds.ChanceAt(wish.Desperation):P2}, {odds.DesperationPerMiss:0.##} desperation per miss) had a {chance:P2} swap chance and {(granted ? "was granted" : "missed")}.");
    }

    private void LogNaturalGrant(CharacterMaster recipient, Wish wish, NaturalDropBehavior behavior)
    {
        if (settings.LogWishRolls.Value)
            IHaveTooMuchItemBloatPlugin.Log.LogInfo($"{PickupNames.Display(wish.Pickup)} dropped naturally for {Players.NameOf(recipient)} at {wish.Desperation:0.##} desperation: {behavior}.");
    }
}
