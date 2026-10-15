using System;
using IHaveTooMuchItemBloat.WishShrine;
using RoR2;

namespace IHaveTooMuchItemBloat.Debugging;

internal static class WishShrineSpawnOdds
{
    private static Settings _settings;

    public static void Install(Settings settings)
    {
        _settings = settings;

        Hooks.Install(typeof(SceneDirector), nameof(SceneDirector.GenerateInteractableCardSelection), [], LogShrineOdds);
    }

    private static WeightedSelection<DirectorCard> LogShrineOdds(Func<SceneDirector, WeightedSelection<DirectorCard>> orig, SceneDirector self)
    {
        var selection = orig(self);

        if (_settings.LogWishShrineSpawnOdds.Value)
            Failsafe.Run("logging Wish Shrine spawn odds", () => IHaveTooMuchItemBloatPlugin.Log.LogInfo(Describe(selection, self.interactableCredit)));

        return selection;
    }

    private static string Describe(WeightedSelection<DirectorCard> selection, int credits)
    {
        var scene = SceneCatalog.mostRecentSceneDef ? SceneCatalog.mostRecentSceneDef.cachedName : "?";
        var totalWeight = selection.GetTotalWeight();
        var shrineWeight = 0f;
        var weightedCost = 0f;

        for (var i = 0; i < selection.Count; i++)
        {
            var choice = selection.choices[i];
            weightedCost += choice.weight * choice.value.cost;

            if (choice.value.spawnCard == WishShrineAssets.SpawnCard)
                shrineWeight += choice.weight;
        }

        if (shrineWeight <= 0f)
            return $"Wish Shrine is not among {scene}'s {selection.Count} interactable choices.";

        var pickChance = shrineWeight / totalWeight;
        var averageCost = weightedCost / totalWeight;
        var picks = averageCost > 0f
            ? credits / averageCost
            : 0f;
        var atLeastOne = 1f - Math.Pow(1f - pickChance, picks);

        return $"Wish Shrine on {scene}: {pickChance:P2} of each interactable pick ({selection.Count} choices). "
            + $"With {credits} credits at about {averageCost:F1} per pick, that is about {picks:F0} picks and a {atLeastOne:P0} chance of at least one.";
    }
}
