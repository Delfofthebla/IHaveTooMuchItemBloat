using R2API;

namespace IHaveTooMuchItemBloat;

internal static class Tokens
{
    private static readonly string Prefix = "IHAVETOOMUCHITEMBLOAT_";
    private static readonly string SelfSuffix = "_2P";

    public static readonly string WishShrineName = Prefix + "WISH_SHRINE_NAME";
    public static readonly string WishShrineContext = Prefix + "WISH_SHRINE_CONTEXT";
    public static readonly string PanelTitle = Prefix + "WISH_PANEL_TITLE";
    public static readonly string ConfirmWish = Prefix + "WISH_PANEL_CONFIRM";
    public static readonly string ConfirmStrengthen = Prefix + "WISH_PANEL_CONFIRM_STRENGTHEN";
    public static readonly string ConfirmReplace = Prefix + "WISH_PANEL_CONFIRM_REPLACE";
    public static readonly string WishMade = Prefix + "WISH_MADE";
    public static readonly string WishStrengthened = Prefix + "WISH_STRENGTHENED";
    public static readonly string WishGranted = Prefix + "WISH_GRANTED";
    public static readonly string HudLabel = Prefix + "WISH_HUD_LABEL";
    public static readonly string HudDesperation = Prefix + "WISH_HUD_DESPERATION";

    public static void Register()
    {
        LanguageAPI.Add(WishShrineName, "Shrine of Wishes");
        LanguageAPI.Add(WishShrineContext, "Make a wish");
        LanguageAPI.Add(PanelTitle, "Make a Wish");
        LanguageAPI.Add(ConfirmWish, "Wish");
        LanguageAPI.Add(ConfirmStrengthen, "Strengthen");
        LanguageAPI.Add(ConfirmReplace, "Replace");
        LanguageAPI.Add(HudLabel, "Wishes:");
        LanguageAPI.Add(HudDesperation, "<style=cShrine>Desperation: {0}</style>");

        AddChatMessage(WishMade, "<style=cShrine>{0} made a wish.</style>", "<style=cShrine>You made a wish.</style>");
        AddChatMessage(WishStrengthened, "<style=cShrine>{0} strengthened a wish.</style>", "<style=cShrine>You strengthened a wish.</style>");
        AddChatMessage(WishGranted, "<style=cShrine>{0}'s wish was granted:</style> {1}", "<style=cShrine>Your wish was granted:</style> {1}");
    }

    private static void AddChatMessage(string token, string text, string textForSelf)
    {
        LanguageAPI.Add(token, text);
        LanguageAPI.Add(token + SelfSuffix, textForSelf);
    }
}
