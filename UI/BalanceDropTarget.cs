using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OttoPay.UI;

/// Makes the balance readout a place to drop things. Coins dropped on it are deposited, and
/// anything a registered deposit handler takes, such as OttoAura's AuraTrade valuables, goes to
/// that handler. While a depositable drag is over the inventory screen the coin icon turns into
/// a deposit arrow.
internal sealed class BalanceDropTarget : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler
{
    private const string TooltipHostName = "OttoPayBalanceTooltip";

    /// The arrow shown in place of the coin while a depositable drag is held.
    internal static Sprite? DepositSprite;

    private UITooltip? _tooltip;
    private Image? _icon;

    // The vanilla armor icon draws with the 'litpanel' material (Custom/LitGui), which
    // desaturates and darkens its sprite. That suits the coin but turns the gold deposit arrow
    // grey, so the arrow draws with the default UI material instead.
    private Material? _iconMaterial;

    private void Awake()
    {
        TryCreateTooltip();
        _icon = transform.Find(ArmorIconName).GetComponent<Image>();
        _iconMaterial = _icon.material;
    }

    private void Update()
    {
        if (_icon == null)
            return;

        ItemDrop.ItemData? dragItem = DraggedItem();
        if (dragItem == null || (!IsCoin(dragItem) && HandlerFor(dragItem) == null))
        {
            if (_icon.sprite != BalancePanel.CoinSprite)
            {
                _icon.sprite = BalancePanel.CoinSprite;
                _icon.material = _iconMaterial;
            }

            return;
        }

        if (_icon.sprite == DepositSprite)
            return;
        _icon.sprite = DepositSprite;
        _icon.material = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OverChildButton(eventData))
            return;
        ShowBalanceTooltip(eventData.pointerEnter);
    }

    /// Moving from a button back onto the icon sends no enter event, because the pointer never
    /// left this object. The button's exit hid its tooltip, so the balance tooltip comes back
    /// here.
    public void OnPointerMove(PointerEventData eventData)
    {
        if (UITooltip.m_current != null || OverChildButton(eventData))
            return;
        ShowBalanceTooltip(eventData.pointerEnter);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_tooltip != null && UITooltip.m_current == _tooltip)
            UITooltip.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        InventoryGui? gui = InventoryGui.m_instance;
        if (gui == null || eventData.button != PointerEventData.InputButton.Left)
            return;
        if (gui.m_dragGo == null || gui.m_dragItem == null || gui.m_dragInventory == null)
            return;

        ItemDrop.ItemData dragItem = gui.m_dragItem;
        if (IsCoin(dragItem))
            DepositCoins(gui, dragItem);
        else if (!DepositWithHandler(gui, dragItem))
            return;

        BalancePanel.Refresh();
        gui.SetupDragItem(null, null, 1);
        Withdrawal.Pending = null;
    }

    private static void DepositCoins(InventoryGui gui, ItemDrop.ItemData coins)
    {
        Balance.SetLocal(Balance.Local() + gui.m_dragAmount);
        if (gui.m_dragAmount == coins.m_stack)
            gui.m_dragInventory.RemoveItem(coins);
        else
            gui.m_dragInventory.RemoveItem(coins, gui.m_dragAmount);
    }

    /// The handler removes the items itself, and leaves the drag alone when it fails.
    private static bool DepositWithHandler(InventoryGui gui, ItemDrop.ItemData item)
    {
        DepositHandler? handler = HandlerFor(item);
        return handler != null && handler.Deposit(gui.m_dragInventory, item, gui.m_dragAmount);
    }

    /// Pointer events reach this object for its child buttons too. The buttons show their own
    /// tooltip, and the icon must not take it over.
    private bool OverChildButton(PointerEventData eventData)
    {
        GameObject? target = eventData.pointerEnter;
        if (target == null)
            return false;
        Button? button = target.GetComponentInParent<Button>();
        return button != null && button.transform != transform && button.transform.IsChildOf(transform);
    }

    /// The tooltip hides itself once the pointer leaves the hovered object's rect. That is the
    /// graphic under the pointer, not this object, whose rect can be smaller than its icon.
    /// Otherwise the pointer move handler would restart the show delay every frame.
    private void ShowBalanceTooltip(GameObject? hovered)
    {
        TryCreateTooltip();
        InventoryGui? gui = InventoryGui.m_instance;
        if (gui == null || _tooltip == null)
            return;

        ItemDrop.ItemData? dragItem = DraggedItem();
        DepositHandler? handler = dragItem != null ? HandlerFor(dragItem) : null;
        if (dragItem != null && IsCoin(dragItem))
        {
            _tooltip.Set("Deposit coins", "Click to deposit these coins in your Merchant Bank balance.");
        }
        else if (dragItem != null && handler != null)
        {
            _tooltip.Set(handler.Name, handler.Describe(gui.m_dragInventory, dragItem, gui.m_dragAmount));
        }
        else
        {
            _tooltip.Set("Merchant Bank",
                $"Balance: {Balance.Local()} coins.\n\nDrop coins here to deposit them. Any merchant draws on your balance when you buy.");
        }

        _tooltip.OnHoverStart(hovered != null ? hovered : gameObject);
    }

    private static ItemDrop.ItemData? DraggedItem()
    {
        InventoryGui? gui = InventoryGui.m_instance;
        return gui != null && gui.m_dragGo != null ? gui.m_dragItem : null;
    }

    /// Coins deposit here directly. Anything else only through a registered deposit handler.
    private static bool IsCoin(ItemDrop.ItemData item)
    {
        return item.m_shared.m_name == CoinToken;
    }

    private static DepositHandler? HandlerFor(ItemDrop.ItemData item)
    {
        InventoryGui? gui = InventoryGui.m_instance;
        Inventory? inventory = gui != null ? gui.m_dragInventory : null;
        if (gui == null || inventory == null || IsCoin(item))
            return null;
        return DepositHandlers.Shared.Accepting(inventory, item, gui.m_dragAmount);
    }

    /// The tooltip lives on an empty child with no graphic, so it never receives pointer events
    /// of its own. On this object it would answer every hover, the child buttons' included.
    private void TryCreateTooltip()
    {
        if (_tooltip != null)
            return;
        GameObject host = new(TooltipHostName, typeof(RectTransform));
        host.transform.SetParent(transform, false);
        _tooltip = Tooltips.Attach(host, "", "");
        if (_tooltip == null)
            Destroy(host);
    }
}
