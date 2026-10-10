using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace IHaveTooMuchItemBloat.Hud;

internal static class WishHud
{
    public static void Install() => HUD.onHudTargetChangedGlobal += Failsafe.Handler<HUD>("attaching the wish display", AttachTo);

    private static void AttachTo(HUD hud)
    {
        var inventoryDisplay = hud.itemInventoryDisplay;
        if (!inventoryDisplay || hud.GetComponentInChildren<WishDisplay>(includeInactive: true))
            return;

        var inventoryRoot = inventoryDisplay.transform.parent;
        var cluster = inventoryRoot ? inventoryRoot.parent : null;
        if (!cluster)
        {
            IHaveTooMuchItemBloatPlugin.Log.LogWarning("The HUD's item bar has no parent cluster; wishes won't be shown on the HUD.");
            return;
        }

        var row = new GameObject("WishDisplay", typeof(RectTransform)) { layer = inventoryDisplay.gameObject.layer };
        row.transform.SetParent(cluster, worldPositionStays: false);
        row.transform.SetSiblingIndex(inventoryRoot.GetSiblingIndex() + 1);
        row.AddComponent<Canvas>();
        row.AddComponent<GraphicRaycaster>();
        row.AddComponent<LayoutElement>();
        row.AddComponent<CanvasGroup>();

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 6f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var display = row.AddComponent<WishDisplay>();
        display.Bind(hud, inventoryDisplay.transform.Find("Subtext"));
    }
}
