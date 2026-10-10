using System.Collections.Generic;
using BepInEx.Configuration;
using IHaveTooMuchItemBloat.Pools;
using IHaveTooMuchItemBloat.Wishes;

namespace IHaveTooMuchItemBloat;

internal sealed class Settings
{
    private const string WishShrineSection = "Wish Shrine";
    private const string WishShrineOfferWeightsSection = "Wish Shrine Offer Weights";
    private const string WishesSection = "Wishes";
    private const string GuaranteedAtSection = "Guaranteed At";
    private const string DropSourcesSection = "Drop Sources";
    private const string DebugSection = "Debug";

    private static readonly Dictionary<DropPool, float> DefaultGuaranteedAt = new()
    {
        [DropPools.Common] = 16f,
        [DropPools.Uncommon] = 10f,
        [DropPools.Legendary] = 8f,
        [DropPools.Lunar] = 10f,
        [DropPools.VoidCommon] = 10f,
        [DropPools.VoidUncommon] = 8f,
        [DropPools.VoidLegendary] = 4f,
        [DropPools.Equipment] = 9f,
        [DropPools.LunarEquipment] = 6f,
    };

    private readonly Dictionary<DropPool, ConfigEntry<float>> _offerWeights = [];
    private readonly Dictionary<DropPool, ConfigEntry<float>> _guaranteedAt = [];

    public ConfigEntry<bool> WishShrineEnabled { get; private set; }
    public ConfigEntry<int> WishShrineSpawnWeight { get; private set; }
    public ConfigEntry<int> WishShrineMaxPerStage { get; private set; }
    public ConfigEntry<int> WishShrineRepeatChance { get; private set; }
    public ConfigEntry<int> WishShrineEarliestStage { get; private set; }
    public ConfigEntry<bool> WishShrineAlwaysOnGildedCoast { get; private set; }
    public ConfigEntry<int> WishShrineBaseCost { get; private set; }
    public ConfigEntry<int> WishShrineCostIncreasePercent { get; private set; }
    public ConfigEntry<int> WishShrineUses { get; private set; }
    public ConfigEntry<bool> WishShrinePerPlayerUses { get; private set; }
    public ConfigEntry<int> WishShrineOfferSize { get; private set; }
    public ConfigEntry<int> MaxWishes { get; private set; }
    public ConfigEntry<int> VanillaOddsPercent { get; private set; }
    public ConfigEntry<int> StrengthenPercent { get; private set; }
    public ConfigEntry<NaturalDropBehavior> NaturalDrops { get; private set; }
    public ConfigEntry<int> PassOnPercent { get; private set; }
    public ConfigEntry<bool> ChestsGrantWishes { get; private set; }
    public ConfigEntry<bool> ChoiceContainersGrantWishes { get; private set; }
    public ConfigEntry<bool> ShrinesOfChanceGrantWishes { get; private set; }
    public ConfigEntry<bool> HiddenShopItemsGrantWishes { get; private set; }
    public ConfigEntry<bool> ConsoleCommandsEnabled { get; private set; }
    public ConfigEntry<bool> AlwaysGrantWishes { get; private set; }
    public ConfigEntry<bool> LogWishRolls { get; private set; }
    public ConfigEntry<bool> LogWishShrineSpawnOdds { get; private set; }

    public Settings(ConfigFile config)
    {
        BindWishShrine(config);
        BindWishShrineOfferWeights(config);
        BindWishes(config);
        BindGuaranteedAt(config);
        BindDropSources(config);
        BindDebug(config);
    }

    public float OfferWeight(DropPool pool)
        => pool != null && _offerWeights.TryGetValue(pool, out var weight) ? weight.Value : 0f;

    public float GuaranteedAt(DropPool pool)
        => pool != null && _guaranteedAt.TryGetValue(pool, out var desperation) ? desperation.Value : 0f;

    public float StrengthenDesperation(DropPool pool) => GuaranteedAt(pool) * StrengthenPercent.Value / 100f;

    private void BindWishShrine(ConfigFile config)
    {
        WishShrineEnabled = config.Bind(WishShrineSection, "Enabled", true, "Wish Shrines spawn on stages. Applies from the next stage.");
        WishShrineSpawnWeight = config.Bind(WishShrineSection, "Spawn Weight", 6,
            new ConfigDescription(
                "Selection weight of the Wish Shrine's stage director category. For scale, on Titanic Plains: chests 45, drones 14, shrines 10, barrels 10, printers and scrappers 8. Applies from the next stage.",
                new AcceptableValueRange<int>(0, 50)));
        WishShrineMaxPerStage = config.Bind(WishShrineSection, "Maximum Per Stage", 2,
            new ConfigDescription(
                "Most Wish Shrines on one stage.",
                new AcceptableValueRange<int>(1, 5)));
        WishShrineRepeatChance = config.Bind(WishShrineSection, "Repeat Chance", 25,
            new ConfigDescription(
                "Percent chance that the stage director places additional Wish Shrines after already placing one. Rolls for each additional shrine attempt by the director.",
                new AcceptableValueRange<int>(0, 100)));
        WishShrineEarliestStage = config.Bind(WishShrineSection, "Earliest Stage", 1,
            new ConfigDescription(
                "First stage on which a Wish Shrine can spawn.",
                new AcceptableValueRange<int>(1, 10)));
        WishShrineAlwaysOnGildedCoast = config.Bind(WishShrineSection, "Always On Gilded Coast", true, "Every visit to Gilded Coast has a Wish Shrine.");
        WishShrineBaseCost = config.Bind(WishShrineSection, "Base Cost", 40,
            new ConfigDescription(
                "Gold cost of a wish before difficulty scaling. For scale: small chest 25, large chest 50. Applies to Wish Shrines spawned after the change.",
                new AcceptableValueRange<int>(0, 400)));
        WishShrineCostIncreasePercent = config.Bind(WishShrineSection, "Cost Increase Percent", 30,
            new ConfigDescription(
                "Compounding price increase per use of a Wish Shrine (Shrine of Chance: 40). With Per Player Uses on, each player's price rises only with their own uses.",
                new AcceptableValueRange<int>(0, 300)));
        WishShrineUses = config.Bind(WishShrineSection, "Uses", 2,
            new ConfigDescription(
                "Uses before a Wish Shrine is spent. A new, strengthened or replaced wish is one use.",
                new AcceptableValueRange<int>(1, 10)));
        WishShrinePerPlayerUses = config.Bind(WishShrineSection, "Per Player Uses", true, "On: each player gets the Wish Shrine's uses. Off: all players share them.");
        WishShrineOfferSize = config.Bind(WishShrineSection, "Offer Size", 10,
            new ConfigDescription(
                "Items a Wish Shrine offers each player. Each player gets their own offer per shrine, excluding items they already wish for.",
                new AcceptableValueRange<int>(1, 16)));
    }

    private void BindWishShrineOfferWeights(ConfigFile config)
    {
        foreach (var pool in DropPools.Wishable)
        {
            _offerWeights[pool] = config.Bind(WishShrineOfferWeightsSection, pool.Name, DefaultOfferWeight(pool),
                new ConfigDescription(
                    $"Weight of the {pool.Name} tier when picking the tier of each item a Wish Shrine offers. 0 never offers it.",
                    new AcceptableValueRange<float>(0f, 100f)));
        }
    }

    private void BindWishes(ConfigFile config)
    {
        MaxWishes = config.Bind(WishesSection, "Maximum Count", 3,
            new ConfigDescription(
                "Wishes a player can hold at once, of any tiers. When full, a new wish replaces one. Lowering this keeps existing wishes.",
                new AcceptableValueRange<int>(1, 6)));
        VanillaOddsPercent = config.Bind(WishesSection, "Vanilla Odds Percent", 120,
            new ConfigDescription(
                "A fresh wish's chance, as a percent of its item's odds from the same drop source in a vanilla game. Never below the item's natural odds.",
                new AcceptableValueRange<int>(0, 500)));
        StrengthenPercent = config.Bind(WishesSection, "Strengthen Percent", 25,
            new ConfigDescription(
                "Desperation added by strengthening a wish, as a percent of its tier's Guaranteed At value.",
                new AcceptableValueRange<int>(0, 100)));
        NaturalDrops = config.Bind(WishesSection, "Natural Drops", NaturalDropBehavior.GrantAndPassOn,
            "What happens when a wished item drops without a wish swapping it in. " +
            "Ignore: the drop counts as a miss for your other wishes in its tier. " +
            "Grant: the wish is granted and its desperation is lost. " +
            "GrantAndPassOn: the wish is granted and Pass On Percent of its desperation is split among your other wishes in its tier. " +
            "GrantAndWishAgain: the wish is granted and reset to 0 desperation.");
        PassOnPercent = config.Bind(WishesSection, "Pass On Percent", 40,
            new ConfigDescription(
                "With Natural Drops set to GrantAndPassOn, the percent of a granted wish's desperation split among your other wishes in its tier.",
                new AcceptableValueRange<int>(0, 100)));
    }

    private void BindGuaranteedAt(ConfigFile config)
    {
        foreach (var pool in DropPools.Wishable)
        {
            _guaranteedAt[pool] = config.Bind(GuaranteedAtSection, pool.Name, DefaultGuaranteedAt[pool],
                new ConfigDescription(
                    $"Desperation at which a {pool.Name} wish is guaranteed. " +
                    "Each drop of the tier that could have been a wished item but wasn't adds desperation, split among the wishes it could have been: " +
                    "1 for a regular drop, more from sources that narrow the tier, such as category chests.",
                    new AcceptableValueRange<float>(1f, 50f)));
        }
    }

    private void BindDropSources(ConfigFile config)
    {
        ChestsGrantWishes = config.Bind(DropSourcesSection, "Chests", true, "Single-item containers, such as chests and equipment barrels, can grant the opener's wishes.");
        ChoiceContainersGrantWishes = config.Bind(DropSourcesSection, "Choice Containers", false, "Containers offering a choice of items can grant the opener's wishes by adding a wished item to the choices. The wish is granted whether or not it is picked.");
        ShrinesOfChanceGrantWishes = config.Bind(DropSourcesSection, "Shrines of Chance", true, "Shrine of Chance rewards can grant the user's wishes.");
        HiddenShopItemsGrantWishes = config.Bind(DropSourcesSection, "Hidden Shop Items", true, "Shop terminals showing \"?\" can grant the buyer's wishes.");
    }

    private void BindDebug(ConfigFile config)
    {
        ConsoleCommandsEnabled = config.Bind(DebugSection, "Console Commands", false, "Enables the itmb_wish, itmb_wish_clear, itmb_wish_odds and itmb_spawn_wish_shrine console commands. The host's setting applies to everyone.");
        AlwaysGrantWishes = config.Bind(DebugSection, "Always Grant Wishes", false, "Every drop from a tier holding a wished item becomes that item.");
        LogWishRolls = config.Bind(DebugSection, "Log Wish Rolls", false, "Logs every wish roll.");
        LogWishShrineSpawnOdds = config.Bind(DebugSection, "Log Wish Shrine Spawn Odds", false, "Logs each stage's chance of spawning a Wish Shrine.");
    }

    private static float DefaultOfferWeight(DropPool pool)
    {
        if (pool == DropPools.Common)
            return 70f;

        if (pool == DropPools.Uncommon)
            return 30f;

        return pool == DropPools.Legendary ? 2f : 0f;
    }
}
