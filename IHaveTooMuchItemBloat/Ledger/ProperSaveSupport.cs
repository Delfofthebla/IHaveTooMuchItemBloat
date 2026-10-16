using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using ProperSave;
using RoR2;

namespace IHaveTooMuchItemBloat.Ledger;

internal static class ProperSaveSupport
{
    public const string GUID = "com.KingEnderBrine.ProperSave";

    private static readonly string PlayersKey = "IHaveTooMuchItemBloat.Players";

    public static void Install()
    {
        if (Chainloader.PluginInfos.ContainsKey(GUID))
            Subscribe();
    }

    // ProperSave's types are resolved when this method is compiled, which only happens when ProperSave is loaded.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Subscribe()
    {
        SaveFile.OnGatherSaveData += Failsafe.Handler<Dictionary<string, object>>("saving the run ledger", Save);
        Loading.OnLoadingEnded += Failsafe.Handler<SaveFile>("loading the run ledger", Load);
    }

    private static void Save(Dictionary<string, object> saveData)
    {
        var run = Run.instance;
        var ledger = RunLedger.Current;
        if (!run || !ledger)
            return;

        saveData[PlayersKey] = run.userMasters.Select(user => SavedPlayer.Of(user.Key, ledger.WishesOf(user.Value))).ToList();
    }

    private static void Load(SaveFile saveFile)
    {
        var ledger = RunLedger.Current;
        var players = saveFile.TryGetModdedData<List<SavedPlayer>>(PlayersKey);
        if (!ledger || players == null)
            return;

        var users = NetworkUser.readOnlyInstancesList;

        // ProperSave gives a lone user the save's first player whatever its user ID; the ledger follows it to the same character.
        if (users.Count == 1)
        {
            Restore(ledger, users[0].master, players.FirstOrDefault());
            return;
        }

        foreach (var user in users)
            Restore(ledger, user.master, players.FirstOrDefault(player => player.User.ToNetworkUserId().Equals(user.id)));
    }

    private static void Restore(RunLedger ledger, CharacterMaster master, SavedPlayer player)
    {
        if (player == null || !master)
            return;

        var missing = player.Wishes.Where(wish => !wish.IsInGame()).Select(wish => wish.Pickup).ToList();
        if (missing.Count > 0)
            IHaveTooMuchItemBloatPlugin.Log.LogWarning($"Dropped saved wishes for pickups this game doesn't have: {string.Join(", ", missing)}.");

        ledger.SetWishes(master, player.Wishes.Where(wish => wish.IsInGame()).Select(wish => wish.ToWish()));
    }
}
