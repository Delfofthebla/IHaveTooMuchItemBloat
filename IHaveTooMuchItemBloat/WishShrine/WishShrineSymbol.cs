using RoR2;
using UnityEngine;

namespace IHaveTooMuchItemBloat.WishShrine;

internal sealed class WishShrineSymbol : MonoBehaviour
{
    public GameObject symbol;

    private PurchaseInteraction _purchase;
    private WishShrineState _state;

    private void Awake()
    {
        _purchase = GetComponent<PurchaseInteraction>();
        _state = GetComponent<WishShrineState>();
    }

    private void Update()
    {
        if (symbol)
            symbol.SetActive(_purchase.available && !SpentForLocalPlayer());
    }

    private bool SpentForLocalPlayer()
    {
        var master = LocalUserManager.GetFirstLocalUser()?.cachedMaster;

        return master && _state.IsExhaustedFor(master);
    }
}
