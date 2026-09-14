using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static OttoPay.OttoPayPlugin;

namespace OttoPay;

public class PocketDrop : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler
{
    private const string TooltipHostName = "OttoPayBalanceTooltip";
    private UITooltip? uiTooltip;
    internal static bool clicked;
    internal static Image armorImage = null!;

    // The vanilla armor icon draws with the 'litpanel' material (Custom/LitGui), which
    // desaturates and darkens its sprite. That suits the coin but turns the gold deposit
    // arrow grey, so the arrow draws with the default UI material instead.
    private Material? armorMaterial;

    private void Awake()
    {
        TryCreateTooltip();
        armorImage = transform.Find(ArmorIconName).GetComponent<Image>();
        armorMaterial = armorImage.material;
    }

    private void Update()
    {
        ItemDrop.ItemData? dragItem = DraggedItem();
        if (dragItem == null || !CanDeposit(dragItem))
        {
            if (armorImage != null && armorImage.sprite != CurrencyPocket.InventoryGuiUpdatePatch.coinSprite)
            {
                armorImage.sprite = CurrencyPocket.InventoryGuiUpdatePatch.coinSprite;
                armorImage.material = armorMaterial;
            }

            return;
        }

        if (armorImage.sprite == DownloadSprite) return;
        armorImage.sprite = DownloadSprite;
        armorImage.material = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OverChildButton(eventData)) return;
        ShowBalanceTooltip(eventData.pointerEnter);
    }

    // Moving from a button back onto the icon sends no enter event, because the pointer never
    // left this object. The button's exit hid its tooltip, so the balance tooltip comes back here.
    public void OnPointerMove(PointerEventData eventData)
    {
        if (UITooltip.m_current != null || OverChildButton(eventData)) return;
        ShowBalanceTooltip(eventData.pointerEnter);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiTooltip != null && UITooltip.m_current == uiTooltip)
        {
            UITooltip.HideTooltip();
        }
    }

    // Pointer events reach this object for its child buttons too. The buttons show their own
    // tooltip, and the icon must not take it over.
    private bool OverChildButton(PointerEventData eventData)
    {
        GameObject? target = eventData.pointerEnter;
        if (target == null) return false;
        Button? button = target.GetComponentInParent<Button>();
        return button != null && button.transform != transform && button.transform.IsChildOf(transform);
    }

    // The tooltip hides itself once the pointer leaves the hovered object's rect. That is the
    // graphic under the pointer, not this object, whose rect can be smaller than its icon.
    // Otherwise the pointer move handler would restart the show delay every frame.
    private void ShowBalanceTooltip(GameObject? hovered)
    {
        TryCreateTooltip();
        if (!InventoryGui.m_instance || uiTooltip == null) return;

        ItemDrop.ItemData? dragItem = DraggedItem();
        if (dragItem != null && CanDeposit(dragItem))
        {
            uiTooltip.Set("Deposit coins", "Click to deposit these coins in your Merchant Bank balance.");
        }
        else
        {
            uiTooltip.Set("Merchant Bank",
                $"Balance: {MiscFunctions.GetPlayerCoinsFromCustomData()} coins.\n\nDrop coins here to deposit them. Any merchant draws on your balance when you buy.");
        }

        uiTooltip.OnHoverStart(hovered != null ? hovered : gameObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!InventoryGui.m_instance) return;
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left when InventoryGui.m_instance.m_dragGo && InventoryGui.m_instance.m_dragItem != null && InventoryGui.m_instance.m_dragInventory != null:
            {
                ItemDrop.ItemData? dragItem = InventoryGui.m_instance.m_dragItem;
                if (!CanDeposit(dragItem)) return;

                clicked = true;
                // Add to the pocket
                MiscFunctions.UpdatePlayerCustomData(MiscFunctions.GetPlayerCoinsFromCustomData() + InventoryGui.m_instance.m_dragAmount);

                CurrencyPocket.UpdatePocketUI();
                if (InventoryGui.m_instance.m_dragAmount == InventoryGui.m_instance.m_dragItem.m_stack)
                {
                    InventoryGui.m_instance.m_dragInventory.RemoveItem(InventoryGui.m_instance.m_dragItem);
                }
                else
                {
                    InventoryGui.m_instance.m_dragInventory.RemoveItem(InventoryGui.m_instance.m_dragItem, InventoryGui.m_instance.m_dragAmount);
                }

                clicked = false;

                InventoryGui.m_instance.SetupDragItem(null, null, 1);
                InventoryGuiOnSplitOkPatch.throwAwayInventory = null!;
                break;
            }
            case PointerEventData.InputButton.Right:
            {
                OttoPayLogger.LogWarning("Right clicked");
            }
                break;
        }
    }

    private static ItemDrop.ItemData? DraggedItem()
    {
        return InventoryGui.m_instance && InventoryGui.m_instance.m_dragGo ? InventoryGui.m_instance.m_dragItem : null;
    }

    // Only coins deposit here. Valuables cash in through OttoAura's AuraTrade at a Warden,
    // which takes a transaction fee.
    private static bool CanDeposit(ItemDrop.ItemData item) => item.m_shared.m_name == CoinToken;

    // The tooltip lives on an empty child with no graphic, so it never receives pointer events
    // of its own. On this object it would answer every hover, the child buttons' included. The
    // old code borrowed the first inventory slot's tooltip instead and rewrote that slot's text.
    private void TryCreateTooltip()
    {
        if (uiTooltip != null) return;
        GameObject host = new(TooltipHostName, typeof(RectTransform));
        host.transform.SetParent(transform, false);
        uiTooltip = Tooltips.Attach(host, "", "");
        if (uiTooltip == null) Destroy(host);
    }
}