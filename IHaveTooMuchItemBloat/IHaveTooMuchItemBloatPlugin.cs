using BepInEx;
using BepInEx.Logging;
using IHaveTooMuchItemBloat.Debugging;
using IHaveTooMuchItemBloat.Hud;
using IHaveTooMuchItemBloat.Patches;
using IHaveTooMuchItemBloat.WishShrine;
using IHaveTooMuchItemBloat.Wishes;
using R2API;
using R2API.Utils;
using RoR2;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace IHaveTooMuchItemBloat;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency(LanguageAPI.PluginGUID)]
[BepInDependency(PrefabAPI.PluginGUID)]
[BepInDependency(DirectorAPI.PluginGUID)]
[BepInDependency(ItemAPI.PluginGUID)]
[BepInDependency(RiskOfOptionsMenu.Guid, BepInDependency.DependencyFlags.SoftDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
// ReSharper disable once InconsistentNaming
public class IHaveTooMuchItemBloatPlugin : BaseUnityPlugin
{
    public const string Guid = "com.Delfofthebla.IHaveTooMuchItemBloat";
    public const string Name = "IHaveTooMuchItemBloat";
    public const string Version = "1.0.0";

    internal static ManualLogSource Log { get; private set; }
    internal static Settings Settings { get; private set; }
    internal static WishRegistry Wishes { get; private set; }
    internal static WishGranter Granter { get; private set; }

    private void Awake()
    {
        Log = Logger;
        Settings = new Settings(Config);
        Wishes = new WishRegistry();
        Granter = new WishGranter(Settings, Wishes);

        Tokens.Register();
        WishStorage.Register();
        Wishes.Granted += AnnounceGrantedWish;

        Failsafe.Run("hooking the stage RNG", StageRng.Install);
        Failsafe.Run("installing drop patches", InstallDropPatches);
        Failsafe.Run("building the Shrine of Wishes", InstallWishShrine);
        Failsafe.Run("installing the wish HUD", WishHud.Install);

        RiskOfOptionsMenu.Register(this, Log);
    }

    private static void InstallDropPatches()
    {
        DropSourcePatches.Install(Settings);
        PickupDropletPatch.Install(Granter);
    }

    private static void InstallWishShrine()
    {
        if (!WishShrinePurchase.Install())
        {
            Log.LogError("The Shrine of Wishes is disabled.");
            return;
        }

        WishShrineAssets.Build(Settings);
        WishShrineSpawning.Install(Settings);
        WishShrineSpawnOdds.Install(Settings);
    }

    private static void AnnounceGrantedWish(CharacterMaster master, PickupIndex pickup)
        => Failsafe.Run("announcing a granted wish", () => WishAnnouncements.Granted(master, pickup));
}
