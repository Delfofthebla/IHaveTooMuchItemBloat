using System;
using System.Collections.Generic;
using System.Linq;
using IHaveTooMuchItemBloat.Pools;
using IHaveTooMuchItemBloat.WishShrine;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace IHaveTooMuchItemBloat.Debugging;

internal static class WishCommands
{
    [ConCommand(commandName = "itmb_wish", flags = ConVarFlags.ExecuteOnServer, helpText = "Replaces your wishes with fresh ones for the named items, by internal or display name (spaces and punctuation ignored). With no names, shows your wishes.")]
    private static void Wish(ConCommandArgs args)
    {
        var master = EnabledSenderMaster(args);
        if (!master)
            return;

        if (args.Count == 0)
        {
            Debug.Log(DescribeWishes(master));
            return;
        }

        if (args.Count > WishStorage.CAPACITY)
        {
            Debug.LogWarning($"At most {WishStorage.CAPACITY} wishes can be held.");
            return;
        }

        var pickups = new List<PickupIndex>();
        for (var i = 0; i < args.Count; i++)
        {
            var pickup = PickupNames.Find(args[i]);
            if (pickup == PickupIndex.none)
            {
                Debug.LogWarning($"No item or equipment is named \"{args[i]}\".");
                return;
            }

            pickups.Add(pickup);
        }

        IHaveTooMuchItemBloatPlugin.Wishes.Set(master, pickups.Select(pickup => new Wish(pickup, 0)));
        Debug.Log(DescribeWishes(master));
    }

    [ConCommand(commandName = "itmb_wish_clear", flags = ConVarFlags.ExecuteOnServer, helpText = "Clears your wishes.")]
    private static void ClearWish(ConCommandArgs args)
    {
        var master = EnabledSenderMaster(args);
        if (!master)
            return;

        IHaveTooMuchItemBloatPlugin.Wishes.Forget(master);
        Debug.Log("Your wishes were cleared.");
    }

    [ConCommand(commandName = "itmb_wish_odds", flags = ConVarFlags.ExecuteOnServer,
        helpText = "Shows each drop pool's size by content pack and a fresh wish's odds for one of its vanilla items on a regular drop.")]
    private static void ShowOdds(ConCommandArgs args)
    {
        if (!EnabledSenderMaster(args))
            return;

        var lines = DropPools.All.Select(pool => DescribeOdds(Run.instance, pool));
        Debug.Log(string.Join("\n", lines));
    }

    [ConCommand(commandName = "itmb_spawn_wish_shrine", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns a Shrine of Wishes in front of you.")]
    private static void SpawnWishShrine(ConCommandArgs args)
    {
        var master = EnabledSenderMaster(args);
        var body = master ? master.GetBody() : null;
        if (!body)
            return;

        if (!WishShrineAssets.ShrinePrefab)
        {
            Debug.LogWarning("The Shrine of Wishes is disabled; see the BepInEx log.");
            return;
        }

        var facing = Vector3.ProjectOnPlane(body.inputBank.aimDirection, Vector3.up).normalized;
        var position = body.footPosition + facing * 4f;
        var shrine = UnityEngine.Object.Instantiate(WishShrineAssets.ShrinePrefab, position, Quaternion.LookRotation(-facing));

        WishShrineAssets.ScaleCostWithDifficulty(shrine);
        NetworkServer.Spawn(shrine);
    }

    private static CharacterMaster EnabledSenderMaster(ConCommandArgs args)
    {
        if (!IHaveTooMuchItemBloatPlugin.Settings.ConsoleCommandsEnabled.Value)
        {
            Debug.LogWarning("Wish commands are disabled. Enable Debug > Console Commands in the host's config.");
            return null;
        }

        if (!Run.instance || !args.senderMaster)
        {
            Debug.LogWarning("Wish commands only work during a run.");
            return null;
        }

        return args.senderMaster;
    }

    private static string DescribeWishes(CharacterMaster master)
    {
        var wishes = IHaveTooMuchItemBloatPlugin.Wishes.Of(master);
        if (wishes.Count == 0)
            return "You have no wishes.";

        var run = Run.instance;

        return "Your wishes:\n" + string.Join("\n", wishes.Select(wish => DescribeWish(run, wish)));
    }

    private static string DescribeWish(Run run, Wish wish)
    {
        var name = PickupNames.Display(wish.Pickup);
        var pool = DropPools.Containing(run, wish.Pickup);
        if (pool == null)
            return $"{name}: not in this run's drop pools.";

        var odds = WishOdds.OnRegularDrop(IHaveTooMuchItemBloatPlugin.Settings, run, pool, wish.Pickup);

        return $"{name} ({pool.Name}): {wish.Desperation:0.##} of {odds.GuaranteedAt:0.##} desperation, " +
               $"{odds.ChanceAt(wish.Desperation):P2} on a regular drop of its tier (natural {odds.NaturalChance:P2}).";
    }

    private static string DescribeContentPacks(List<PickupIndex> available)
    {
        var counts = VanillaContent.ContentPacks
            .Select(contentPack => $"{contentPack.Replace("RoR2.", "")} {available.Count(pickup => VanillaContent.ContentPackOf(pickup) == contentPack)}");
        var modded = available.Count(pickup => !VanillaContent.Contains(pickup));

        return string.Join(", ", counts) + $", modded {modded}";
    }

    private static string DescribeOdds(Run run, DropPool pool)
    {
        var available = pool.AvailableIn(run);
        var header = $"{pool.Name}: {available.Count} available ({DescribeContentPacks(available)}).";
        if (available.Count == 0)
            return header;

        var vanillaIndex = available.FindIndex(VanillaContent.Contains);
        var example = available[Math.Max(0, vanillaIndex)];

        var odds = WishOdds.OnRegularDrop(IHaveTooMuchItemBloatPlugin.Settings, run, pool, example);

        return $"{header} One item on a regular drop: natural {odds.NaturalChance:P2}, vanilla {odds.VanillaChance:P2}, fresh wish {odds.FreshChance:P2}, guaranteed at {odds.GuaranteedAt:0.##} desperation.";
    }
}
