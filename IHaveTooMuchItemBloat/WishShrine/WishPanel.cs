using System.Text;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IHaveTooMuchItemBloat.WishShrine;

internal sealed class WishPanel : MonoBehaviour
{
    public const string FocusOutlineName = "FocusOutline";

    private const float UnfocusedAlpha = 0.35f;
    private const float FocusedScale = 1.1f;

    private static readonly Color EmptySlotColor = new(0.5f, 0.5f, 0.5f);
    private static readonly StringBuilder PriceText = new();

    public RectTransform slotContainer;
    public GameObject slotPrefab;
    public GridLayoutGroup slotGrid;
    public MPButton confirmButton;
    public TextMeshProUGUI priceLabel;

    private UIElementAllocator<MPButton> _slots;
    private LanguageTextMeshController _confirmLabel;
    private WishShrineController _controller;

    private void Awake()
    {
        _slots = new UIElementAllocator<MPButton>(slotContainer, slotPrefab);
        _slots.onCreateElement = WireSlot;
        _confirmLabel = confirmButton.GetComponent<LanguageTextMeshController>();
        confirmButton.onClick.AddListener(Confirm);
    }

    public void Bind(WishShrineController controller)
    {
        _controller = controller;

        slotGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        slotGrid.constraintCount = controller.SlotCount;
        _slots.AllocateElements(controller.SlotCount);

        Refresh();
    }

    public void Refresh()
    {
        for (var i = 0; i < _slots.elements.Count; i++)
            ShowInSlot(_slots.elements[i], _controller.ShownIn(i), i == _controller.FocusedSlot);

        var action = _controller.StagedAction;
        confirmButton.interactable = action != WishAction.None;

        if (_confirmLabel)
            _confirmLabel.token = ConfirmTokenFor(action);

        ShowPrice();
    }

    private void WireSlot(int index, MPButton button)
    {
        button.interactable = true;
        button.onClick.AddListener(() => _controller.FocusSlot(index));
    }

    private static void ShowInSlot(MPButton slot, PickupIndex pickup, bool focused)
    {
        var pickupDef = PickupCatalog.GetPickupDef(pickup);
        slot.interactable = true;
        slot.transform.localScale = Vector3.one * (focused ? FocusedScale : 1f);

        var icon = IconOf(slot);
        if (icon)
        {
            icon.sprite = pickupDef?.iconSprite;
            icon.enabled = icon.sprite;
        }

        var tierColor = pickupDef != null ? pickupDef.baseColor : EmptySlotColor;
        tierColor.a = focused ? 1f : UnfocusedAlpha;

        if (slot.TryGetComponent(out Image background))
            background.color = tierColor;

        var outline = slot.transform.Find("BaseOutline");
        if (outline && outline.TryGetComponent(out Image outlineImage))
            outlineImage.color = focused ? Color.white : tierColor;

        var focusOutline = slot.transform.Find(FocusOutlineName);
        if (focusOutline && focusOutline.TryGetComponent(out Image focusOutlineImage))
            focusOutlineImage.color = focused ? WishShrineAssets.WishColor : Color.clear;
    }

    private static Image IconOf(MPButton slot)
    {
        var childLocator = slot.GetComponent<ChildLocator>();
        var icon = childLocator ? childLocator.FindChild("Icon") : null;

        return icon ? icon.GetComponent<Image>() : null;
    }

    private void ShowPrice()
    {
        if (!priceLabel)
            return;

        PriceText.Clear();
        _controller.CostType.BuildCostStringStyled(_controller.Price, PriceText, forWorldDisplay: false);

        priceLabel.SetText(PriceText);
    }

    private static string ConfirmTokenFor(WishAction action) => action switch
    {
        WishAction.Strengthen => Tokens.CONFIRM_STRENGTHEN,
        WishAction.Replace => Tokens.CONFIRM_REPLACE,
        _ => Tokens.CONFIRM_WISH,
    };

    private void Confirm()
    {
        _controller.ConfirmSelection();
        Destroy(gameObject);
    }
}
