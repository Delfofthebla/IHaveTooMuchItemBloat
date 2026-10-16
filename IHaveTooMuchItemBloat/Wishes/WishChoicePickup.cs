using RoR2;
using UnityEngine;

namespace IHaveTooMuchItemBloat.Wishes;

internal sealed class WishChoicePickup : MonoBehaviour
{
    private WishChoices _choices;
    private WishChoice _choice;
    private bool _anyPicked;
    private bool _wishPicked;

    public void Bind(WishChoices choices, WishChoice choice)
    {
        _choices = choices;
        _choice = choice;

        GetComponent<PickupPickerController>().onUniquePickupSelected.AddListener(Record);
    }

    private void Record(UniquePickup picked)
    {
        _anyPicked = true;
        if (_wishPicked || picked.pickupIndex != _choice.Wish)
            return;

        _wishPicked = true;
        Failsafe.Run("granting a wish choice", () => _choices.Grant(_choice));
    }

    private void OnDestroy()
    {
        if (_wishPicked)
            return;

        if (_anyPicked)
            Failsafe.Run("declining a wish choice", () => _choices.Decline(_choice));
        else
            Failsafe.Run("abandoning a wish choice", () => _choices.Abandon(_choice));
    }
}
