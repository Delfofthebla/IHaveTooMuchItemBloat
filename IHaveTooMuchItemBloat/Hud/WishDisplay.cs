using System.Collections.Generic;
using System.Linq;
using IHaveTooMuchItemBloat.Wishes;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IHaveTooMuchItemBloat.Hud;

internal sealed class WishDisplay : MonoBehaviour
{
    private const float IconSize = 36f;
    private const float RefreshInterval = 0.25f;

    private readonly List<GameObject> _icons = [];
    private List<Wish> _shownWishes = [];
    private HUD _hud;
    private LayoutElement _layoutElement;
    private CanvasGroup _canvasGroup;
    private float _refreshTimer;

    public void Bind(HUD hud, Transform labelTemplate)
    {
        _hud = hud;
        _layoutElement = GetComponent<LayoutElement>();
        _layoutElement.preferredHeight = IconSize;
        _canvasGroup = GetComponent<CanvasGroup>();

        if (labelTemplate)
            CreateLabel(labelTemplate);

        SetVisible(false);
    }

    private void Update()
    {
        var scoreboardOpen = _hud && _hud.scoreboardPanel && _hud.scoreboardPanel.activeInHierarchy;
        if (!scoreboardOpen)
        {
            SetVisible(false);
            _refreshTimer = 0f;
            return;
        }

        RefreshWishes();
        SetVisible(_shownWishes.Count > 0);
    }

    private void RefreshWishes()
    {
        _refreshTimer -= Time.unscaledDeltaTime;
        if (_refreshTimer > 0f)
            return;

        _refreshTimer = RefreshInterval;

        var wishes = IHaveTooMuchItemBloatPlugin.Wishes.Of(_hud.targetMaster);
        if (!wishes.SequenceEqual(_shownWishes))
            Show(wishes);
    }

    private void SetVisible(bool visible)
    {
        _layoutElement.ignoreLayout = !visible;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void Show(IReadOnlyList<Wish> wishes)
    {
        _shownWishes = [.. wishes];

        foreach (var icon in _icons)
            Destroy(icon);

        _icons.Clear();

        foreach (var wish in wishes)
            _icons.Add(CreateIcon(wish));
    }

    private void CreateLabel(Transform template)
    {
        var label = Instantiate(template.gameObject, transform, worldPositionStays: false);
        label.name = "Label";
        label.SetActive(true);

        if (label.TryGetComponent(out TextMeshProUGUI text))
            text.text = Language.GetString(Tokens.HUD_LABEL);
    }

    private GameObject CreateIcon(Wish wish)
    {
        var pickupDef = PickupCatalog.GetPickupDef(wish.Pickup);

        var icon = new GameObject(pickupDef.internalName, typeof(RectTransform)) { layer = gameObject.layer };
        icon.transform.SetParent(transform, worldPositionStays: false);

        var image = icon.AddComponent<Image>();
        image.sprite = pickupDef.iconSprite;
        image.preserveAspect = true;

        var layoutElement = icon.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = IconSize;
        layoutElement.preferredHeight = IconSize;

        var tooltip = icon.AddComponent<TooltipProvider>();
        tooltip.titleToken = pickupDef.nameToken;
        tooltip.titleColor = pickupDef.darkColor;
        tooltip.bodyToken = DescriptionTokenOf(pickupDef);
        tooltip.overrideBodyText = Language.GetString(tooltip.bodyToken) + "\n\n" + Language.GetStringFormatted(Tokens.HUD_DESPERATION, wish.Desperation.ToString("0.##"));

        return icon;
    }

    private static string DescriptionTokenOf(PickupDef pickupDef)
    {
        if (pickupDef.itemIndex != ItemIndex.None)
            return ItemCatalog.GetItemDef(pickupDef.itemIndex).pickupToken;

        if (pickupDef.equipmentIndex != EquipmentIndex.None)
            return EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex).pickupToken;

        return "";
    }
}
