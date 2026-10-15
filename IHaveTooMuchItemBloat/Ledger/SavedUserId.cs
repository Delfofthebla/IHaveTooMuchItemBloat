using RoR2;

namespace IHaveTooMuchItemBloat.Ledger;

internal sealed record SavedUserId(ulong Value, string StringValue, byte SubId)
{
    public NetworkUserId ToNetworkUserId() => string.IsNullOrEmpty(StringValue)
        ? NetworkUserId.FromId(Value, SubId)
        : new NetworkUserId(StringValue, SubId);

    public static SavedUserId Of(NetworkUserId id) => new(id.value, id.strValue ?? "", id.subId);
}
