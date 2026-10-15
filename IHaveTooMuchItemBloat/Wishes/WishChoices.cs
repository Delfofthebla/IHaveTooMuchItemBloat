using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace IHaveTooMuchItemBloat.Wishes;

internal sealed class WishChoices(WishRegistry wishes)
{
    private static readonly string OptionPickupPrefab = "RoR2/DLC1/OptionPickup/OptionPickup.prefab";

    private readonly Dictionary<PickupPickerController.Option[], WishChoice> _landing = [];
    private readonly List<WishChoice> _open = [];
    private GameObject _choicePrefab;

    public void Install()
    {
        _choicePrefab = Addressables.LoadAssetAsync<GameObject>(OptionPickupPrefab).WaitForCompletion();
        if (!_choicePrefab)
            IHaveTooMuchItemBloatPlugin.Log.LogError($"{OptionPickupPrefab} is missing; wishes replace drops instead of offering a choice.");

        Stage.onServerStageBegin += Failsafe.Handler<Stage>("clearing wish choices", _ => Clear());
    }

    public bool IsOpen(CharacterMaster owner, PickupIndex wish) => _open.Any(choice => choice.Owner == owner && choice.Wish == wish);

    public GenericPickupController.CreatePickupInfo Offer(CharacterMaster owner, PickupIndex wish, GenericPickupController.CreatePickupInfo pickupInfo)
    {
        var isChoiceAlready = pickupInfo.pickerOptions is { Length: > 0 };
        if (!isChoiceAlready && !_choicePrefab)
            return SwappedForWish(owner, wish, pickupInfo);

        var offered = isChoiceAlready
            ? WithWishAdded(pickupInfo, wish)
            : AsChoice(pickupInfo, wish);

        var choice = new WishChoice(owner, wish);
        _landing[offered.pickerOptions] = choice;
        _open.Add(choice);

        return offered;
    }

    public void Attach(PickupPickerController picker, PickupPickerController.Option[] options)
    {
        if (options == null || !_landing.TryGetValue(options, out var choice))
            return;

        _landing.Remove(options);
        picker.gameObject.AddComponent<WishChoicePickup>().Bind(this, choice);
    }

    public void Grant(WishChoice choice)
    {
        _open.Remove(choice);
        wishes.Grant(choice.Owner, choice.Wish);
    }

    public void Decline(WishChoice choice)
    {
        _open.Remove(choice);
        wishes.Decline(choice.Owner, choice.Wish);
    }

    public void Abandon(WishChoice choice) => _open.Remove(choice);

    private void Clear()
    {
        _landing.Clear();
        _open.Clear();
    }

    private GenericPickupController.CreatePickupInfo SwappedForWish(CharacterMaster owner, PickupIndex wish, GenericPickupController.CreatePickupInfo pickupInfo)
    {
        wishes.Grant(owner, wish);
        pickupInfo.pickup = pickupInfo.pickup.WithPickupIndex(wish);

        return pickupInfo;
    }

    private GenericPickupController.CreatePickupInfo AsChoice(GenericPickupController.CreatePickupInfo pickupInfo, PickupIndex wish)
    {
        var rolled = pickupInfo.pickup;

        pickupInfo.pickerOptions = [Available(rolled), Available(rolled.WithPickupIndex(wish))];
        pickupInfo.maxOptionCount = 1;
        pickupInfo.prefabOverride = _choicePrefab;
        pickupInfo.pickup = DisplayFor(rolled);

        return pickupInfo;
    }

    private static GenericPickupController.CreatePickupInfo WithWishAdded(GenericPickupController.CreatePickupInfo pickupInfo, PickupIndex wish)
    {
        var options = pickupInfo.pickerOptions;

        // The game gives a Potential without a pick count one pick per option past the second, so the added option would add a pick.
        pickupInfo.maxOptionCount ??= options.Length - 2;
        pickupInfo.pickerOptions = [.. options, Available(options[0].pickup.WithPickupIndex(wish))];

        return pickupInfo;
    }

    private static UniquePickup DisplayFor(UniquePickup rolled)
    {
        var pickupDef = PickupCatalog.GetPickupDef(rolled.pickupIndex);
        var tierPickup = pickupDef != null && pickupDef.itemIndex != ItemIndex.None
            ? PickupCatalog.FindPickupIndex(pickupDef.itemTier)
            : PickupIndex.none;

        return tierPickup != PickupIndex.none
            ? new UniquePickup(tierPickup)
            : rolled;
    }

    private static PickupPickerController.Option Available(UniquePickup pickup) => new() { pickup = pickup, available = true };
}
