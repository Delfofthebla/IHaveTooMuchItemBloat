using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine.Networking;

namespace IHaveTooMuchItemBloat.WishShrine;

internal sealed class WishShrineState : NetworkBehaviour
{
    private static readonly uint UsesDirtyBit = 1u;
    private static readonly uint TermsDirtyBit = 2u;
    private static readonly uint AllDirtyBits = UsesDirtyBit | TermsDirtyBit;

    private readonly Dictionary<NetworkInstanceId, int> _usesBy = [];
    private readonly Dictionary<NetworkInstanceId, PickupIndex[]> _offers = [];
    private Xoroshiro128Plus _offerRng;
    private bool _offersSomething;
    private int _wishSlots = 1;
    private int _totalUses;
    private int _usesPerShrine = 1;
    private bool _usesArePerPlayer = true;
    private float _costMultiplierPerUse = 1f;

    public bool OffersNothing => !_offersSomething;

    public static int PlacedCount => InstanceTracker.GetInstancesList<WishShrineState>().Count;

    public int WishSlots => _wishSlots;

    public bool UsesSharedPrice => !_usesArePerPlayer;

    private void Awake()
    {
        if (!NetworkServer.active)
            return;

        var settings = IHaveTooMuchItemBloatPlugin.Settings;
        var run = Run.instance;

        _offerRng = WishOffers.NewShrineRng();
        _offersSomething = WishOffers.CanOfferAnything(settings, run);
        _wishSlots = settings.MaxWishes.Value;
        _usesPerShrine = settings.WishShrineUses.Value;
        _usesArePerPlayer = settings.WishShrinePerPlayerUses.Value;
        _costMultiplierPerUse = 1f + settings.WishShrineCostIncreasePercent.Value / 100f;
        SetDirtyBit(TermsDirtyBit);
    }

    private void OnEnable() => InstanceTracker.Add(this);

    private void OnDisable() => InstanceTracker.Remove(this);

    public bool IsExhaustedFor(CharacterMaster master) => _usesArePerPlayer && UsesBy(master) >= _usesPerShrine;

    public bool IsSpent()
    {
        if (!_usesArePerPlayer)
            return _totalUses >= _usesPerShrine;

        return PlayerCharacterMasterController.instances.All(player => !player.master || IsExhaustedFor(player.master));
    }

    public int PriceFor(CharacterMaster master, int shrinePrice)
    {
        if (!_usesArePerPlayer || !master)
            return shrinePrice;

        var price = shrinePrice;
        for (var i = 0; i < UsesBy(master); i++)
            price = RaisedPrice(price);

        return price;
    }

    public int RaisedPrice(int price) => (int)(price * _costMultiplierPerUse);

    public void RecordUseBy(CharacterMaster master)
    {
        if (!NetworkServer.active)
            return;

        _usesBy[master.netId] = UsesBy(master) + 1;
        _totalUses++;
        SetDirtyBit(UsesDirtyBit);
    }

    public IReadOnlyList<PickupIndex> OfferFor(CharacterMaster master)
    {
        if (!_offers.TryGetValue(master.netId, out var offer))
        {
            var settings = IHaveTooMuchItemBloatPlugin.Settings;
            offer = WishOffers.Roll(settings, Run.instance, _offerRng, PlayerIndexOf(master));
            _offers[master.netId] = offer;
        }

        return offer;
    }

    private static int PlayerIndexOf(CharacterMaster master)
    {
        var players = PlayerCharacterMasterController.instances;
        for (var i = 0; i < players.Count; i++)
        {
            if (players[i].master == master)
                return i;
        }

        return players.Count;
    }

    private int UsesBy(CharacterMaster master) => _usesBy.TryGetValue(master.netId, out var uses) ? uses : 0;

    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        var dirtyBits = initialState ? AllDirtyBits : syncVarDirtyBits;
        writer.WritePackedUInt32(dirtyBits);

        if ((dirtyBits & UsesDirtyBit) != 0)
            WriteUses(writer);

        if ((dirtyBits & TermsDirtyBit) != 0)
            WriteTerms(writer);

        return dirtyBits != 0;
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        var dirtyBits = reader.ReadPackedUInt32();

        if ((dirtyBits & UsesDirtyBit) != 0)
            ReadUses(reader);

        if ((dirtyBits & TermsDirtyBit) != 0)
            ReadTerms(reader);
    }

    private void WriteUses(NetworkWriter writer)
    {
        writer.WritePackedUInt32((uint)_usesBy.Count);

        foreach (var uses in _usesBy)
        {
            writer.Write(uses.Key);
            writer.WritePackedUInt32((uint)uses.Value);
        }
    }

    private void ReadUses(NetworkReader reader)
    {
        _usesBy.Clear();

        var count = reader.ReadPackedUInt32();
        for (var i = 0; i < count; i++)
            _usesBy[reader.ReadNetworkId()] = (int)reader.ReadPackedUInt32();
    }

    private void WriteTerms(NetworkWriter writer)
    {
        writer.WritePackedUInt32((uint)_usesPerShrine);
        writer.Write(_usesArePerPlayer);
        writer.Write(_costMultiplierPerUse);
        writer.Write(_offersSomething);
        writer.WritePackedUInt32((uint)_wishSlots);
    }

    private void ReadTerms(NetworkReader reader)
    {
        _usesPerShrine = (int)reader.ReadPackedUInt32();
        _usesArePerPlayer = reader.ReadBoolean();
        _costMultiplierPerUse = reader.ReadSingle();
        _offersSomething = reader.ReadBoolean();
        _wishSlots = (int)reader.ReadPackedUInt32();
    }
}
