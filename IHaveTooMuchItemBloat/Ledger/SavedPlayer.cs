using System.Collections.Generic;
using System.Linq;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;

namespace IHaveTooMuchItemBloat.Ledger;

internal sealed record SavedPlayer(SavedUserId User, List<SavedWish> Wishes)
{
    public static SavedPlayer Of(NetworkUserId user, IEnumerable<Wish> wishes) => new(SavedUserId.Of(user), [.. wishes.Select(SavedWish.Of)]);
}
