using System;
using R2API;
using RoR2;

namespace IHaveTooMuchItemBloat.WishShrine;

internal static class WishShrineSpawning
{
    // R2API matches categories by substring, so this name must not contain or be contained in another category's name (such as "Shrines").
    private const string CategoryName = "Wishes";

    private const string GildedCoastScene = "goldshores";

    private static Settings _settings;
    private static DirectorCard _card;
    private static DirectorAPI.DirectorCardHolder _cardHolder;
    private static SceneDirector _populatingDirector;
    private static bool _repeatsEnded;

    public static void Install(Settings settings)
    {
        _settings = settings;
        _card = new DirectorCard { spawnCard = WishShrineAssets.SpawnCard, selectionWeight = 1 };
        _cardHolder = new DirectorAPI.DirectorCardHolder
        {
            Card = _card,
            InteractableCategory = DirectorAPI.InteractableCategory.Custom,
            CustomInteractableCategory = CategoryName,
        };

        Hooks.Install(typeof(DirectorCard), nameof(DirectorCard.IsAvailable), [], GateRepeatSpawns);

        ApplySettings();
        settings.WishShrineEnabled.SettingChanged += ApplySettings;
        settings.WishShrineSpawnWeight.SettingChanged += ApplySettings;
        settings.WishShrineMaxPerStage.SettingChanged += ApplySettings;
        settings.WishShrineEarliestStage.SettingChanged += ApplySettings;

        DirectorAPI.Helpers.AddNewInteractable(_cardHolder);

        SceneDirector.onPrePopulateSceneServer += BeginPopulating;
        SceneDirector.onPostPopulateSceneServer += _ => _populatingDirector = null;
        SceneDirector.onPostPopulateSceneServer += Failsafe.Handler<SceneDirector>("spawning a Shrine of Wishes on Gilded Coast", SpawnOnGildedCoast);
    }

    private static void BeginPopulating(SceneDirector sceneDirector)
    {
        _populatingDirector = sceneDirector;
        _repeatsEnded = false;
    }

    private static void ApplySettings(object sender, EventArgs args) => ApplySettings();

    private static void ApplySettings()
    {
        _cardHolder.InteractableCategorySelectionWeight = _settings.WishShrineEnabled.Value ? _settings.WishShrineSpawnWeight.Value : 0;
        _card.minimumStageCompletions = _settings.WishShrineEarliestStage.Value - 1;
        WishShrineAssets.SpawnCard.maxSpawnsPerStage = _settings.WishShrineMaxPerStage.Value;
    }

    private static bool GateRepeatSpawns(Func<DirectorCard, bool> orig, DirectorCard self)
    {
        var available = orig(self);
        if (!available || !_populatingDirector || self.spawnCard != WishShrineAssets.SpawnCard)
            return available;

        return Failsafe.Run("rolling a repeat Shrine of Wishes", RollRepeatSpawn, true);
    }

    private static bool RollRepeatSpawn()
    {
        var placed = WishShrineState.PlacedCount;
        if (placed == 0 || placed >= WishShrineAssets.SpawnCard.maxSpawnsPerStage)
            return true;

        if (_repeatsEnded)
            return false;

        _repeatsEnded = _populatingDirector.rng.nextNormalizedFloat * 100f >= _settings.WishShrineRepeatChance.Value;

        return !_repeatsEnded;
    }

    private static void SpawnOnGildedCoast(SceneDirector sceneDirector)
    {
        var scene = SceneCatalog.mostRecentSceneDef;
        var run = Run.instance;

        if (!_settings.WishShrineEnabled.Value || !_settings.WishShrineAlwaysOnGildedCoast.Value)
            return;

        if (!run || !scene || scene.cachedName != GildedCoastScene)
            return;

        if (!DirectorCore.instance || WishShrineState.PlacedCount > 0)
            return;

        var placement = new DirectorPlacementRule { placementMode = DirectorPlacementRule.PlacementMode.Random };

        var shrine = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(WishShrineAssets.SpawnCard, placement, sceneDirector.rng));
        WishShrineAssets.ScaleCostWithDifficulty(shrine);
    }
}
