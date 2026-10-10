using RoR2;

namespace IHaveTooMuchItemBloat.Wishes;

internal readonly record struct Wish(PickupIndex Pickup, float Desperation)
{
    public Wish WithDesperation(float desperation) => this with { Desperation = desperation };
}
