using System;
using System.Collections.Generic;
using System.Linq;
using IHaveTooMuchItemBloat.Ledger;
using RoR2;

namespace IHaveTooMuchItemBloat.Wishes;

internal sealed class WishRegistry
{
    public event Action<CharacterMaster, PickupIndex> Granted;

    public IReadOnlyList<Wish> Of(CharacterMaster master)
    {
        var ledger = RunLedger.Current;

        return ledger
            ? ledger.WishesOf(master)
            : [];
    }

    public void Set(CharacterMaster master, IEnumerable<Wish> wishes)
    {
        var ledger = RunLedger.Current;
        if (ledger)
            ledger.SetWishes(master, wishes);
    }

    public void Place(CharacterMaster master, int slot, PickupIndex pickup)
    {
        var wishes = Of(master).ToList();
        var wish = new Wish(pickup, 0f);

        if ((uint)slot < wishes.Count)
            wishes[slot] = wish;
        else
            wishes.Add(wish);

        Set(master, wishes);
    }

    public void AddDesperation(CharacterMaster master, PickupIndex pickup, float desperation) => Update(master, pickup, wish => wish.WithDesperation(wish.Desperation + desperation));

    public void Forget(CharacterMaster master) => Set(master, []);

    public void Grant(CharacterMaster master, PickupIndex pickup)
    {
        var wishes = Of(master).ToList();
        var granted = wishes.FindIndex(wish => wish.Pickup == pickup);
        if (granted < 0)
            return;

        wishes.RemoveAt(granted);
        Set(master, wishes);
        Granted?.Invoke(master, pickup);
    }

    public void GrantAndRenew(CharacterMaster master, PickupIndex pickup)
    {
        Update(master, pickup, wish => wish.WithDesperation(0f));
        Granted?.Invoke(master, pickup);
    }

    private void Update(CharacterMaster master, PickupIndex pickup, Func<Wish, Wish> change) => Set(master, Of(master).Select(wish => ChangedIfMatching(wish, pickup, change)));

    private static Wish ChangedIfMatching(Wish wish, PickupIndex pickup, Func<Wish, Wish> change) => wish.Pickup == pickup
        ? change(wish)
        : wish;
}
