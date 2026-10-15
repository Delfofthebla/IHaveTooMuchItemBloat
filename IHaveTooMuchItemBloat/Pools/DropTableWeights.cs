using System.Collections.Generic;
using RoR2;

namespace IHaveTooMuchItemBloat.Pools;

internal sealed class DropTableWeights
{
    private readonly Dictionary<PickupIndex, float> _weights = [];
    private float _totalWeight;
    private float _vanillaWeight;

    private DropTableWeights() { }

    public static DropTableWeights Of(PickupDropTable table)
    {
        var selection = SelectionOf(table);
        if (selection == null)
            return null;

        var weights = new DropTableWeights();
        for (var i = 0; i < selection.Count; i++)
            weights.Add(selection.choices[i].value.pickupIndex, selection.choices[i].weight);

        return weights;
    }

    public static DropTableWeights OfPool(Run run, DropPool pool)
    {
        var weights = new DropTableWeights();
        foreach (var pickup in pool.AvailableIn(run))
            weights.Add(pickup, 1f);

        return weights;
    }

    public DropTableWeights Within(DropPool pool)
    {
        var tier = new DropTableWeights();
        foreach (var (pickup, weight) in _weights)
        {
            if (pool.Matches(pickup))
                tier.Add(pickup, weight);
        }

        return tier;
    }

    public bool CanDrop(PickupIndex pickup) => WeightOf(pickup) > 0f;

    public float NaturalChance(PickupIndex pickup) => _totalWeight > 0f
        ? WeightOf(pickup) / _totalWeight
        : 0f;

    public float VanillaChance(PickupIndex pickup)
    {
        var weight = WeightOf(pickup);
        var vanillaWeight = VanillaContent.Contains(pickup)
            ? _vanillaWeight
            : _vanillaWeight + weight;

        return vanillaWeight > 0f
            ? weight / vanillaWeight
            : 0f;
    }

    private static WeightedSelection<UniquePickup> SelectionOf(PickupDropTable table) => table switch
    {
        BasicPickupDropTable basic => basic.selector,
        ExplicitPickupDropTable explicitTable => explicitTable.weightedSelection,
        FreeChestDropTable freeChest => freeChest.selector,
        _ => null,
    };

    private float WeightOf(PickupIndex pickup) => _weights.TryGetValue(pickup, out var weight)
        ? weight
        : 0f;

    private void Add(PickupIndex pickup, float weight)
    {
        if (weight <= 0f || !pickup.isValid)
            return;

        _weights[pickup] = WeightOf(pickup) + weight;
        _totalWeight += weight;

        if (VanillaContent.Contains(pickup))
            _vanillaWeight += weight;
    }
}
