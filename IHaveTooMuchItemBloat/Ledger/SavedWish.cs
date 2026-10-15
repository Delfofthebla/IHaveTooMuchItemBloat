using IHaveTooMuchItemBloat.Wishes;
using RoR2;

namespace IHaveTooMuchItemBloat.Ledger;

internal sealed record SavedWish(string Pickup, float Desperation)
{
    public bool IsInGame() => PickupCatalog.FindPickupIndex(Pickup) != PickupIndex.none;

    public Wish ToWish() => new(PickupCatalog.FindPickupIndex(Pickup), Desperation);

    public static SavedWish Of(Wish wish) => new(PickupCatalog.GetPickupDef(wish.Pickup)?.internalName ?? "", wish.Desperation);
}
