using IHaveTooMuchItemBloat.Pools;
using RoR2;

namespace IHaveTooMuchItemBloat.Wishes;

internal static class WishAnnouncements
{
    public static void Made(CharacterMaster master, bool strengthened)
    {
        Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
        {
            subjectAsNetworkUser = Players.NetworkUserOf(master),
            baseToken = strengthened ? Tokens.WISH_STRENGTHENED : Tokens.WISH_MADE,
        });
    }

    public static void Granted(CharacterMaster master, PickupIndex pickup)
    {
        IHaveTooMuchItemBloatPlugin.Log.LogInfo($"Granted {Players.NameOf(master)}'s wish: {PickupNames.Display(pickup)}.");

        var pickupDef = PickupCatalog.GetPickupDef(pickup);
        if (pickupDef == null)
            return;

        Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
        {
            subjectAsNetworkUser = Players.NetworkUserOf(master),
            baseToken = Tokens.WISH_GRANTED,
            pickupToken = pickupDef.nameToken,
            pickupColor = pickupDef.baseColor,
            pickupQuantity = 1,
        });
    }
}
