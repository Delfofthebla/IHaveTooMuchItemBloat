using System;
using R2API;
using RoR2;
using RoR2.Hologram;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IHaveTooMuchItemBloat.WishShrine;

internal static class WishShrineAssets
{
    private const string ShrineOfChancePrefab = "RoR2/Base/ShrineChance/ShrineChance.prefab";
    private const string ShrineOfCombatPrefab = "RoR2/Base/ShrineCombat/ShrineCombat.prefab";
    private const string ShrineOfCombatSpawnCard = "RoR2/Base/ShrineCombat/iscShrineCombat.asset";
    private const string ShrineOfTheMountainPrefab = "RoR2/Base/ShrineBoss/ShrineBoss.prefab";
    private const string GreenColorRamp = "RoR2/Base/Common/ColorRamps/texRampHealing.png";
    private const string CraftingPanelPrefab = "RoR2/DLC3/MealPrep/MealPrepPickerPanel.prefab";
    private const string RecipeBackgroundPath = "MainPanel/Juice/BGContainer/CraftingContainer/Background";
    private const string PanelTitlePath = "MainPanel/Juice/LabelContainer/Label";
    private const string SymbolPath = "Symbol";
    private const string HoverOutlinePath = "HoverOutline";

    private static readonly Vector2 PriceLabelSize = new(200f, 36f);

    public static readonly Color WishColor = new(0.4f, 1f, 0.5f);

    public static GameObject ShrinePrefab { get; private set; }
    public static InteractableSpawnCard SpawnCard { get; private set; }

    public static void Build(Settings settings)
    {
        var panel = BuildPanel();

        ShrinePrefab = BuildShrine(panel);
        SpawnCard = BuildSpawnCard(ShrinePrefab);

        ApplyBaseCost(settings);
        settings.WishShrineBaseCost.SettingChanged += (_, _) => ApplyBaseCost(settings);
    }

    public static void ScaleCostWithDifficulty(GameObject shrine)
    {
        if (!shrine || !Run.instance || !shrine.TryGetComponent(out PurchaseInteraction purchase))
            return;

        purchase.Networkcost = Run.instance.GetDifficultyScaledCost(purchase.cost);
    }

    private static void ApplyBaseCost(Settings settings)
        => ShrinePrefab.GetComponent<PurchaseInteraction>().cost = settings.WishShrineBaseCost.Value;

    private static GameObject BuildPanel()
    {
        var panel = Require<GameObject>(CraftingPanelPrefab).InstantiateClone("WishPickerPanel", registerNetwork: false);
        var craftingPanel = panel.GetComponent<CraftingPanel>();
        if (!craftingPanel)
            throw new InvalidOperationException($"{CraftingPanelPrefab} has no {nameof(CraftingPanel)}.");

        var wishPanel = panel.AddComponent<WishPanel>();
        wishPanel.slotContainer = craftingPanel.ingredientContainer;
        wishPanel.slotPrefab = craftingPanel.slotPrefab;
        wishPanel.slotGrid = craftingPanel.gridlayoutGroup;
        wishPanel.confirmButton = craftingPanel.confirmButton;
        Object.DestroyImmediate(craftingPanel);

        var recipeBackground = panel.transform.Find(RecipeBackgroundPath);
        Hide(recipeBackground ? recipeBackground.Find("Arrow") : null);
        Hide(recipeBackground ? recipeBackground.Find("Result/Holder") : null);

        SetToken(panel.transform.Find(PanelTitlePath), Tokens.PANEL_TITLE);
        SetToken(wishPanel.confirmButton ? wishPanel.confirmButton.transform : null, Tokens.CONFIRM_WISH);

        wishPanel.priceLabel = BuildPriceLabel(wishPanel.confirmButton);
        AddFocusOutline(wishPanel.slotPrefab);

        return panel;
    }

    private static TextMeshProUGUI BuildPriceLabel(MPButton confirmButton)
    {
        var buttonText = confirmButton ? confirmButton.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true) : null;
        if (!buttonText)
            return null;

        var column = confirmButton.transform.parent;
        if (column.TryGetComponent(out VerticalLayoutGroup columnLayout))
            columnLayout.childForceExpandHeight = false;

        var label = Object.Instantiate(buttonText, column);
        label.name = "Price";
        label.transform.SetSiblingIndex(confirmButton.transform.GetSiblingIndex());

        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = PriceLabelSize;

        return label;
    }

    private static void AddFocusOutline(GameObject slotPrefab)
    {
        var hoverOutline = FindIn(slotPrefab, HoverOutlinePath);
        if (!hoverOutline)
            return;

        var focusOutline = Object.Instantiate(hoverOutline.gameObject, slotPrefab.transform);
        focusOutline.name = WishPanel.FocusOutlineName;
    }

    private static GameObject BuildShrine(GameObject panel)
    {
        var shrineOfChance = Load<GameObject>(ShrineOfChancePrefab);
        var shrine = Require<GameObject>(ShrineOfCombatPrefab).InstantiateClone("WishShrine", registerNetwork: true);

        RemoveCombatShrineBehavior(shrine);
        ConfigurePurchase(shrine);

        if (shrineOfChance)
            AddPriceHologram(shrine, shrineOfChance);

        var symbol = shrine.transform.Find(SymbolPath);
        ReplaceSymbol(symbol, FindIn(Load<GameObject>(ShrineOfTheMountainPrefab), SymbolPath));

        shrine.AddComponent<WishShrineState>();
        shrine.AddComponent<NetworkUIPromptController>();

        var controller = shrine.AddComponent<WishShrineController>();
        controller.panelPrefab = panel;
        controller.cutoffDistance = 10f;
        controller.wishMadeEffectPrefab = WishMadeEffectFrom(shrineOfChance);

        HideSymbolPerPlayer(shrine, symbol);

        return shrine;
    }

    private static void RemoveCombatShrineBehavior(GameObject shrine)
    {
        Object.DestroyImmediate(shrine.GetComponent<ShrineCombatBehavior>());
        Object.DestroyImmediate(shrine.GetComponent<CombatDirector>());
        Object.DestroyImmediate(shrine.GetComponent<CombatSquad>());
        Object.DestroyImmediate(shrine.GetComponent<GenericInspectInfoProvider>());
    }

    private static void ConfigurePurchase(GameObject shrine)
    {
        var purchase = shrine.GetComponent<PurchaseInteraction>();
        purchase.displayNameToken = Tokens.WISH_SHRINE_NAME;
        purchase.contextToken = Tokens.WISH_SHRINE_CONTEXT;
        purchase.costType = CostTypeIndex.Money;
        purchase.automaticallyScaleCostWithDifficulty = false;
        purchase.saleStarCompatible = false;
#pragma warning disable CS0618
        purchase.onPurchase = new PurchaseEvent();
#pragma warning restore CS0618
        purchase.onDetailedPurchaseServer = new DetailedPurchaseEvent();

        if (shrine.TryGetComponent(out GenericDisplayNameProvider displayName))
            displayName.displayToken = Tokens.WISH_SHRINE_NAME;
    }

    private static void AddPriceHologram(GameObject shrine, GameObject shrineOfChance)
    {
        var template = shrineOfChance.GetComponent<HologramProjector>();
        if (!template || !template.hologramPivot)
            return;

        var pivot = new GameObject("HologramPivot").transform;
        pivot.SetParent(shrine.transform, worldPositionStays: false);
        pivot.localPosition = template.hologramPivot.localPosition;
        pivot.localRotation = template.hologramPivot.localRotation;

        var projector = shrine.AddComponent<HologramProjector>();
        projector.hologramPivot = pivot;
        projector.displayDistance = template.displayDistance;
        projector.disableHologramRotation = template.disableHologramRotation;
    }

    private static GameObject WishMadeEffectFrom(GameObject shrineOfChance)
    {
        var behavior = shrineOfChance ? shrineOfChance.GetComponent<ShrineChanceBehavior>() : null;

        return behavior ? behavior.effectPrefabShrineRewardNormal : null;
    }

    private static void HideSymbolPerPlayer(GameObject shrine, Transform symbol)
    {
        if (!symbol)
            return;

        var availabilityIndicator = shrine.GetComponent<PurchaseAvailabilityIndicator>();
        if (availabilityIndicator && availabilityIndicator.indicatorObject == symbol.gameObject)
            availabilityIndicator.indicatorObject = null;

        shrine.AddComponent<WishShrineSymbol>().symbol = symbol.gameObject;
    }

    private static void ReplaceSymbol(Transform symbol, Transform donorSymbol)
    {
        if (!symbol || !donorSymbol)
            return;

        if (!symbol.TryGetComponent(out MeshFilter meshFilter) || !donorSymbol.TryGetComponent(out MeshFilter donorMeshFilter))
            return;

        if (!symbol.TryGetComponent(out MeshRenderer renderer) || !donorSymbol.TryGetComponent(out MeshRenderer donorRenderer))
            return;

        meshFilter.sharedMesh = donorMeshFilter.sharedMesh;
        renderer.sharedMaterials = donorRenderer.sharedMaterials;

        Recolor(renderer);
    }

    private static void Recolor(MeshRenderer renderer)
    {
        var greenRamp = Load<Texture2D>(GreenColorRamp);
        if (!greenRamp || !renderer.sharedMaterial)
            return;

        var material = new Material(renderer.sharedMaterial);
        material.SetTexture("_RemapTex", greenRamp);
        material.SetColor("_TintColor", Color.white);

        renderer.sharedMaterial = material;
    }

    private static InteractableSpawnCard BuildSpawnCard(GameObject shrine)
    {
        var spawnCard = Object.Instantiate(Require<InteractableSpawnCard>(ShrineOfCombatSpawnCard));
        spawnCard.name = "iscWishShrine";
        spawnCard.prefab = shrine;

        return spawnCard;
    }

    private static Transform FindIn(GameObject root, string path) => root ? root.transform.Find(path) : null;

    private static void Hide(Transform transform)
    {
        if (transform)
            transform.gameObject.SetActive(false);
    }

    private static void SetToken(Transform transform, string token)
    {
        if (transform && transform.TryGetComponent(out LanguageTextMeshController text))
            text.token = token;
    }

    private static T Require<T>(string key) where T : Object
    {
        var asset = Load<T>(key);
        if (!asset)
            throw new InvalidOperationException($"Required asset {key} is missing.");

        return asset;
    }

    private static T Load<T>(string key) where T : Object
    {
        var asset = Failsafe.Run($"loading {key}", () => Addressables.LoadAssetAsync<T>(key).WaitForCompletion(), null);
        if (!asset)
            IHaveTooMuchItemBloatPlugin.Log.LogWarning($"Asset {key} is missing.");

        return asset;
    }
}
