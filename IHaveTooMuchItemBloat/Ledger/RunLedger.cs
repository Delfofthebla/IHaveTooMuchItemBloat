using System.Collections.Generic;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;
using UnityEngine.Networking;

namespace IHaveTooMuchItemBloat.Ledger;

internal sealed class RunLedger : NetworkBehaviour
{
    private static readonly uint WishesDirtyBit = 1u;
    private static readonly uint AllDirtyBits = WishesDirtyBit;

    private readonly Dictionary<NetworkInstanceId, Wish[]> _wishes = [];

    public static RunLedger Current => Run.instance
        ? Run.instance.GetComponent<RunLedger>()
        : null;

    public static void Install() => GameModeCatalog.availability.CallWhenAvailable(() => Failsafe.Run("adding the run ledger to game modes", AddToGameModes));

    public IReadOnlyList<Wish> WishesOf(CharacterMaster master) => master && _wishes.TryGetValue(master.netId, out var wishes)
        ? wishes
        : [];

    public void SetWishes(CharacterMaster master, IEnumerable<Wish> wishes)
    {
        if (!NetworkServer.active || !master)
            return;

        _wishes[master.netId] = [.. wishes];
        SetDirtyBit(WishesDirtyBit);
    }

    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        var dirtyBits = initialState ? AllDirtyBits : syncVarDirtyBits;
        writer.WritePackedUInt32(dirtyBits);

        if ((dirtyBits & WishesDirtyBit) != 0)
            WriteWishes(writer);

        return dirtyBits != 0;
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        var dirtyBits = reader.ReadPackedUInt32();

        if ((dirtyBits & WishesDirtyBit) != 0)
            ReadWishes(reader);
    }

    private void WriteWishes(NetworkWriter writer)
    {
        writer.WritePackedUInt32((uint)_wishes.Count);

        foreach (var playerWishes in _wishes)
        {
            writer.Write(playerWishes.Key);
            writer.WritePackedUInt32((uint)playerWishes.Value.Length);

            foreach (var wish in playerWishes.Value)
            {
                writer.Write(wish.Pickup.value);
                writer.Write(wish.Desperation);
            }
        }
    }

    private void ReadWishes(NetworkReader reader)
    {
        _wishes.Clear();

        var playerCount = reader.ReadPackedUInt32();
        for (var player = 0; player < playerCount; player++)
        {
            var master = reader.ReadNetworkId();
            var wishes = new Wish[reader.ReadPackedUInt32()];

            for (var i = 0; i < wishes.Length; i++)
                wishes[i] = new Wish(new PickupIndex(reader.ReadInt32()), reader.ReadSingle());

            _wishes[master] = wishes;
        }
    }

    private static void AddToGameModes()
    {
        for (var index = 0; index < GameModeCatalog.gameModeCount; index++)
        {
            var run = GameModeCatalog.GetGameModePrefabComponent((GameModeIndex)index);
            if (run && !run.GetComponent<RunLedger>())
                run.gameObject.AddComponent<RunLedger>();
        }
    }
}
