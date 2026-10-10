using System;
using System.Collections.Generic;
using RoR2;

namespace IHaveTooMuchItemBloat.Pools;

internal sealed class DropPool(string name, ColorCatalog.ColorIndex color, Func<Run, List<PickupIndex>> availablePickups, Func<PickupDef, bool> matches)
{
    public string Name { get; } = name;

    public ColorCatalog.ColorIndex Color { get; } = color;

    public List<PickupIndex> AvailableIn(Run run) => availablePickups(run);

    public bool Matches(PickupIndex pickup)
    {
        var pickupDef = PickupCatalog.GetPickupDef(pickup);

        return pickupDef != null && matches(pickupDef);
    }
}
