using System;
using RoR2;

namespace IHaveTooMuchItemBloat.Pools;

internal readonly struct WishOdds
{
    private readonly float _vanillaOddsMultiplier;
    
    public float NaturalChance { get; }
    public float VanillaChance { get; }
    public float GuaranteedAt { get; }
    public float DesperationPerMiss { get; }
    public float FreshChance => NaturalChance > 0f ? Math.Max(NaturalChance, Math.Min(1f, VanillaChance * _vanillaOddsMultiplier)) : 0f;

    public WishOdds(Settings settings, DropPool tier, DropTableWeights sourceTier, DropTableWeights wholeTier, PickupIndex pickup)
    {
        NaturalChance = sourceTier.NaturalChance(pickup);
        VanillaChance = sourceTier.VanillaChance(pickup);
        GuaranteedAt = settings.GuaranteedAt(tier);
        _vanillaOddsMultiplier = settings.VanillaOddsPercent.Value / 100f;

        var wholeTierChance = wholeTier.NaturalChance(pickup);
        DesperationPerMiss = wholeTierChance > 0f ? NaturalChance / wholeTierChance : 1f;
    }

    public static WishOdds OnRegularDrop(Settings settings, Run run, DropPool tier, PickupIndex pickup)
    {
        var wholeTier = DropTableWeights.OfPool(run, tier);

        return new WishOdds(settings, tier, wholeTier, wholeTier, pickup);
    }

    public float ChanceAt(float desperation)
    {
        var fresh = FreshChance;
        if (fresh <= 0f)
            return 0f;

        var progress = GuaranteedAt > 0f ? Math.Min(1f, desperation / GuaranteedAt) : 0f;

        return (float)Math.Pow(fresh, 1f - progress);
    }

    public float SwapChanceAt(float desperation)
        => NaturalChance < 1f ? Math.Max(0f, (ChanceAt(desperation) - NaturalChance) / (1f - NaturalChance)) : 0f;
}
