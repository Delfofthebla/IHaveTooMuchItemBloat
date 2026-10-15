using System.Collections.Generic;
using System.Linq;
using IHaveTooMuchItemBloat.Pools;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace IHaveTooMuchItemBloat.WishShrine;

internal sealed class WishShrineController : PickupPickerController, IInteractable
{
    private const byte CANCEL_MESSAGE = 1;
    private const byte CONFIRM_MESSAGE = 2;

    private IReadOnlyList<Wish> _participantWishes = [];
    private PickupIndex _stagedPickup = PickupIndex.none;
    private WishShrineState _state;
    private PurchaseInteraction _purchase;
    private WishPanel _panel;

    public GameObject wishMadeEffectPrefab;

    public bool OffersNothing => State.OffersNothing;

    public int SlotCount => Mathf.Max(State.WishSlots, _participantWishes.Count);

    public int FocusedSlot { get; private set; }

    public CostTypeDef CostType => CostTypeCatalog.GetCostTypeDef(Purchase.costType);

    public int Price
    {
        get
        {
            var participant = networkUIPromptController.currentParticipantMaster;
            var price = State.PriceFor(participant, Purchase.cost);
            var participantBody = participant
                ? participant.GetBody()
                : null;

            return TeamManager.AdjustCostForLongstandingSolitude(Purchase.costType, price, participantBody);
        }
    }

    public WishAction StagedAction
    {
        get
        {
            if (_stagedPickup == PickupIndex.none)
                return WishAction.None;

            var current = WishedIn(FocusedSlot);
            if (current == PickupIndex.none)
                return WishAction.NewWish;

            return current == _stagedPickup
                ? WishAction.Strengthen
                : WishAction.Replace;
        }
    }

    private bool IsStagedFromOffer => _stagedPickup != PickupIndex.none && _participantWishes.All(wish => wish.Pickup != _stagedPickup);

    private WishShrineState State => _state ??= GetComponent<WishShrineState>();

    private PurchaseInteraction Purchase => _purchase ??= GetComponent<PurchaseInteraction>();

    public PickupIndex ShownIn(int slot) => slot == FocusedSlot && _stagedPickup != PickupIndex.none
        ? _stagedPickup
        : WishedIn(slot);

    public bool IsExhaustedFor(CharacterMaster master) => State.IsExhaustedFor(master);

    public bool IsInUseByAnyoneBut(CharacterMaster master)
    {
        var participant = networkUIPromptController.currentParticipantMaster;

        return participant && participant != master;
    }

    public void OpenFor(Interactor activator)
    {
        var master = Players.MasterOf(activator);
        if (!NetworkServer.active || OffersNothing || !master)
            return;

        SetOptionsServer(WishOffers.AsOptions(OpenOfferFor(master)));
        networkUIPromptController.SetParticipantMasterFromInteractor(activator);
    }

    public void FocusSlot(int slot)
    {
        if ((uint)slot >= SlotCount)
            return;

        if (IsStagedFromOffer)
        {
            FocusedSlot = slot;
            RefreshPanel();
            return;
        }

        var wished = WishedIn(slot);
        var strengtheningThisSlot = slot == FocusedSlot && _stagedPickup == wished;

        FocusedSlot = slot;
        _stagedPickup = strengtheningThisSlot ? PickupIndex.none : wished;

        RefreshPanel();
    }

    public void StageOption(int optionIndex)
    {
        if ((uint)optionIndex >= options.Length)
            return;

        var pickup = options[optionIndex].pickup.pickupIndex;
        _stagedPickup = _stagedPickup == pickup
            ? PickupIndex.none
            : pickup;

        RefreshPanel();
    }

    public void ConfirmSelection()
    {
        if (StagedAction == WishAction.None)
            return;

        if (NetworkServer.active)
        {
            MakeWish(networkUIPromptController.currentParticipantMaster, FocusedSlot, _stagedPickup);
            return;
        }

        var writer = networkUIPromptController.BeginMessageToServer();
        writer.Write(CONFIRM_MESSAGE);
        writer.WritePackedUInt32((uint)FocusedSlot);
        writer.Write(_stagedPickup.value);
        networkUIPromptController.FinishMessageToServer(writer);
    }

    public override void HandleClientMessage(NetworkReader reader)
    {
        switch (reader.ReadByte())
        {
            case CANCEL_MESSAGE:
                networkUIPromptController.SetParticipantMaster(null);
                break;
            case CONFIRM_MESSAGE:
                var slot = (int)reader.ReadPackedUInt32();
                var pickup = new PickupIndex(reader.ReadInt32());
                MakeWish(networkUIPromptController.currentParticipantMaster, slot, pickup);
                break;
        }
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        base.OnDeserialize(reader, initialState);

        RefreshPanel();
    }

    public override void OnDisplayBegin(NetworkUIPromptController promptController, LocalUser localUser, CameraRigController cameraRigController)
    {
        base.OnDisplayBegin(promptController, localUser, cameraRigController);

        _participantWishes = IHaveTooMuchItemBloatPlugin.Wishes.Of(promptController.currentParticipantMaster);
        _stagedPickup = PickupIndex.none;
        FocusedSlot = FirstEmptySlot();

        _panel = panelInstance.GetComponent<WishPanel>();
        _panel.Bind(this);
    }

    public override void OnDisplayEnd(NetworkUIPromptController promptController, LocalUser localUser, CameraRigController cameraRigController)
    {
        _panel = null;

        base.OnDisplayEnd(promptController, localUser, cameraRigController);
    }

    private IEnumerable<PickupIndex> OpenOfferFor(CharacterMaster master)
    {
        var wished = IHaveTooMuchItemBloatPlugin.Wishes.Of(master).Select(wish => wish.Pickup).ToList();

        return State.OfferFor(master).Where(pickup => !wished.Contains(pickup));
    }

    private int FirstEmptySlot()
    {
        for (var slot = 0; slot < SlotCount; slot++)
        {
            if (WishedIn(slot) == PickupIndex.none)
                return slot;
        }

        return 0;
    }

    private PickupIndex WishedIn(int slot) => (uint)slot < _participantWishes.Count
        ? _participantWishes[slot].Pickup
        : PickupIndex.none;

    private void RefreshPanel()
    {
        if (_panel)
            _panel.Refresh();
    }

    private void MakeWish(CharacterMaster master, int slot, PickupIndex pickup)
    {
        if (!master || !Purchase.available || State.IsExhaustedFor(master))
            return;

        var wishes = IHaveTooMuchItemBloatPlugin.Wishes;
        var held = wishes.Of(master);
        var strengthening = (uint)slot < held.Count && held[slot].Pickup == pickup;

        if (!strengthening && !CanWishFor(master, held, slot, pickup))
            return;

        if (!TryPay(master))
            return;

        if (strengthening)
            wishes.AddDesperation(master, pickup, IHaveTooMuchItemBloatPlugin.Settings.StrengthenDesperation(DropPools.TierOf(pickup)));
        else
            wishes.Place(master, slot, pickup);

        RecordUseBy(master);
        WishAnnouncements.Made(master, strengthening);
        PlayWishMadeEffect();
    }

    private bool TryPay(CharacterMaster master)
    {
        var body = master.GetBody();
        var interactor = body
            ? body.GetComponent<Interactor>()
            : null;

        return interactor && WishShrinePurchase.TryPay(Purchase, interactor, master);
    }

    private void RecordUseBy(CharacterMaster master)
    {
        State.RecordUseBy(master);

        if (State.IsSpent())
            Purchase.SetAvailable(false);
        else if (State.UsesSharedPrice)
            Purchase.Networkcost = State.RaisedPrice(Purchase.cost);
    }

    private bool CanWishFor(CharacterMaster master, IReadOnlyList<Wish> held, int slot, PickupIndex pickup)
    {
        if (!OpenOfferFor(master).Contains(pickup))
            return false;

        var replacing = (uint)slot < held.Count;

        return replacing || held.Count < State.WishSlots;
    }

    private void PlayWishMadeEffect()
    {
        if (!wishMadeEffectPrefab)
            return;

        EffectManager.SpawnEffect(wishMadeEffectPrefab, new EffectData
        {
            origin = transform.position,
            rotation = Quaternion.identity,
            scale = 1f,
            color = WishShrineAssets.WishColor,
        }, transmit: true);
    }

    string IInteractable.GetContextString(Interactor activator) => null;

    Interactability IInteractable.GetInteractability(Interactor activator) => Interactability.Disabled;

    void IInteractable.OnInteractionBegin(Interactor activator) { }

    bool IInteractable.ShouldIgnoreSpherecastForInteractibility(Interactor activator) => true;

    bool IInteractable.ShouldShowOnScanner() => false;

    bool IInteractable.ShouldProximityHighlight() => false;
}
