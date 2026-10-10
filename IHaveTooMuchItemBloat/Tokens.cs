using R2API;

namespace IHaveTooMuchItemBloat;

internal static class Tokens
{
    private const string Prefix = "IHAVETOOMUCHITEMBLOAT_";
    private const string SelfSuffix = "_2P";

    public const string WISH_ENTRY_NAME = Prefix + "WISH_ENTRY_NAME";
    public const string WISH_SHRINE_NAME = Prefix + "WISH_SHRINE_NAME";
    public const string WISH_SHRINE_CONTEXT = Prefix + "WISH_SHRINE_CONTEXT";
    public const string PANEL_TITLE = Prefix + "WISH_PANEL_TITLE";
    public const string CONFIRM_WISH = Prefix + "WISH_PANEL_CONFIRM";
    public const string CONFIRM_STRENGTHEN = Prefix + "WISH_PANEL_CONFIRM_STRENGTHEN";
    public const string CONFIRM_REPLACE = Prefix + "WISH_PANEL_CONFIRM_REPLACE";
    public const string WISH_MADE = Prefix + "WISH_MADE";
    public const string WISH_STRENGTHENED = Prefix + "WISH_STRENGTHENED";
    public const string WISH_GRANTED = Prefix + "WISH_GRANTED";
    public const string HUD_LABEL = Prefix + "WISH_HUD_LABEL";
    public const string HUD_DESPERATION = Prefix + "WISH_HUD_DESPERATION";

    public static void Register()
    {
        LanguageAPI.Add(WISH_ENTRY_NAME, "Wish");
        LanguageAPI.Add(WISH_SHRINE_NAME, "Shrine of Wishes");
        LanguageAPI.Add(WISH_SHRINE_CONTEXT, "Make a wish");
        LanguageAPI.Add(PANEL_TITLE, "Make a Wish");
        LanguageAPI.Add(CONFIRM_WISH, "Wish");
        LanguageAPI.Add(CONFIRM_STRENGTHEN, "Strengthen");
        LanguageAPI.Add(CONFIRM_REPLACE, "Replace");
        LanguageAPI.Add(HUD_LABEL, "Wishes:");
        LanguageAPI.Add(HUD_DESPERATION, "<style=cShrine>Desperation: {0}</style>");

        AddChatMessage(WISH_MADE, "<style=cShrine>{0} made a wish.</style>", "<style=cShrine>You made a wish.</style>");
        AddChatMessage(WISH_STRENGTHENED, "<style=cShrine>{0} strengthened a wish.</style>", "<style=cShrine>You strengthened a wish.</style>");
        AddChatMessage(WISH_GRANTED, "<style=cShrine>{0}'s wish was granted:</style> {1}", "<style=cShrine>Your wish was granted:</style> {1}");
    }

    private static void AddChatMessage(string token, string text, string textForSelf)
    {
        LanguageAPI.Add(token, text);
        LanguageAPI.Add(token + SelfSuffix, textForSelf);
    }
}
