using System.Collections;
using BepInEx.Bootstrap;
using TMPro;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OttoPay.UI;

/// The Merchant Bank balance in the inventory screen: a coin readout cloned from the armor
/// readout, with a withdraw button and, when OttoAura is installed, the AuraPay toggle.
internal static class BalancePanel
{
    private static GameObject? _panel;
    private static Button? _withdrawButton;
    private static Button? _auraPayButton;
    private static GameObject? _shiftedFor;
    private static Coroutine? _shift;

    /// The coin icon the readout shows when nothing is dragged over it.
    internal static Sprite? CoinSprite;

    internal static void Build(InventoryGui gui)
    {
        if (_panel == null)
            CreatePanel(gui);

        if (_withdrawButton == null && _panel != null)
            CreateWithdrawButton(gui, _panel);

        // The toggle only means something when OttoAura is there to use the balance.
        if (_auraPayButton == null && _panel != null && Chainloader.PluginInfos.ContainsKey(OttoAuraGuid))
            CreateAuraPayToggle(gui, _panel);
    }

    /// Shows the panel to bank members only and brings the count up to date.
    internal static void Refresh()
    {
        if (_panel == null)
            return;
        _panel.SetActive(Membership.LocalIsMember());

        Transform? coinText = Utils.FindChild(_panel.transform, AcText);
        if (coinText == null)
            return;
        TextMeshProUGUI? label = coinText.GetComponent<TextMeshProUGUI>();
        if (label == null)
            return;
        label.text = $"{Balance.Local()}";

        // Runs on every inventory open too, so a tooltip that found no prefab at start-up still
        // gets one.
        AttachWithdrawTooltip();
        RefreshAuraPayToggle();
    }

    internal static void RefreshAuraPayToggle()
    {
        if (_auraPayButton == null)
            return;
        TextMeshProUGUI? label = _auraPayButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null)
            return;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8f;
        bool on = OttoPayApi.IsAuraPayEnabled();
        label.text = on ? "<color=#7CFFB2>AuraPay</color>" : "<color=#8A8A8A>AuraPay</color>";

        IReadOnlyList<string> serviceLines = AuraServices.Shared.Lines();
        string serviceBlock = serviceLines.Count > 0 ? "\n\n" + string.Join("\n\n", serviceLines) : "";

        Tooltips.Attach(_auraPayButton.gameObject, "AuraPay",
            on
                ? "AuraPay is on.\n\nA ward aura may charge your Merchant Bank balance to repair the gear you are wearing." + serviceBlock + "\n\nClick to turn it off."
                : "AuraPay is off.\n\nWard auras will not charge your Merchant Bank balance for repairs." + serviceBlock + "\n\nClick to turn it on.");
    }

    /// The vanilla panels beside the armor readout move to make room for the balance, once
    /// there is a balance to show. The move waits a frame, until the panels have their layout.
    internal static void MakeRoom(InventoryGui gui)
    {
        if (!Membership.LocalIsMember())
            return;

        if (_shiftedFor != null)
        {
            if (_shift != null)
                gui.StopCoroutine(_shift);
            return;
        }

        _shiftedFor = gui.gameObject;
        _shift = gui.StartCoroutine(ShiftPanelsNextFrame(gui));
    }

    /// Jewelcrafting and QuickStackStore move the same panels, so with either installed the
    /// balance keeps its place and the other panels shift instead.
    internal static bool OverlappingPanelModInstalled()
    {
        Dictionary<string, PluginInfo> plugins = Chainloader.PluginInfos;
        return plugins.ContainsKey(QuickStackStoreGuid) || plugins.ContainsKey(JewelcraftingGuid);
    }

    private static IEnumerator ShiftPanelsNextFrame(InventoryGui gui)
    {
        yield return null;
        Transform inventory = gui.m_player.transform;
        for (int i = 0; i < inventory.childCount; ++i)
        {
            Transform child = inventory.GetChild(i);
            if (child.name is ArmorName or WeightName or JewelcraftingSynergyName or CoinPocketUIName or TrashButtonName)
            {
                RectTransform? rect = child.gameObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    switch (child.name)
                    {
                        case CoinPocketUIName when !OverlappingPanelModInstalled():
                            rect.anchoredPosition += new Vector2(0, -45);
                            break;
                        case WeightName when !OverlappingPanelModInstalled():
                            break;
                        default:
                            rect.anchoredPosition += new Vector2(0, 45);
                            break;
                    }
                }
            }

            // The armor and weight highlight frames would stay where the readouts used to be.
            if (child.name == "selected_frame")
            {
                child.transform.Find("selected (2)").gameObject.SetActive(false);
                child.transform.Find("selected (3)").gameObject.SetActive(false);
            }
        }
    }

    private static void CreatePanel(InventoryGui gui)
    {
        Transform inventory = gui.m_player.transform;
        Transform armor = inventory.Find(ArmorName);
        GameObject panel = Object.Instantiate(armor.gameObject, inventory);
        panel.name = CoinPocketUIName;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        Log.Debug($"Creating pocket UI at {panelRect.anchoredPosition}");
        if (OverlappingPanelModInstalled())
        {
            panelRect.anchoredPosition += new Vector2(0, -234);
        }
        else
        {
            // Halfway between the armor and weight readouts.
            RectTransform armorRect = armor.GetComponent<RectTransform>();
            RectTransform weightRect = inventory.Find(WeightName).GetComponent<RectTransform>();
            panel.transform.SetSiblingIndex(armor.GetSiblingIndex());
            panelRect.anchoredPosition = new Vector2(armorRect.anchoredPosition.x, (armorRect.anchoredPosition.y + weightRect.anchoredPosition.y) / 2);
        }

        Log.Debug($"Creating pocket UI at {panelRect.anchoredPosition}");
        GameObject? coins = ObjectDB.instance.GetItemPrefab(CoinsPrefabName);
        CoinSprite = coins.GetComponent<ItemDrop>().m_itemData.GetIcon();
        panel.transform.Find(ArmorIconName).GetComponent<Image>().sprite = CoinSprite;
        panel.transform.SetSiblingIndex(armor.GetSiblingIndex());
        panel.transform.Find(AcText).GetComponent<TextMeshProUGUI>().text = $"{Balance.Local()}";
        // The armor readout it is cloned from may carry a vanilla tooltip. Left in place, it
        // answers the pointer for the buttons on top of the icon too, in its own style.
        Tooltips.Strip(panel);
        panel.AddComponent<BalanceDropTarget>();
        _panel = panel;
    }

    private static void CreateWithdrawButton(InventoryGui gui, GameObject panel)
    {
        Button button = Object.Instantiate(gui.m_takeAllButton, panel.transform);
        button.name = ExtractCoinsButtonName;
        button.GetComponentInChildren<TextMeshProUGUI>().text = "\U0001F4E6";
        button.transform.SetParent(panel.transform, false);
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(Withdrawal.Start);
        DetachGamepadHint(button);

        RectTransform? rect = button.GetComponent<RectTransform>();
        if (rect == null)
            rect = button.gameObject.AddComponent<RectTransform>();
        rect.localPosition = new Vector3(2.5f, -20, 0);
        button.transform.localScale = new Vector3(0.4f, 0.4f, 1);

        Tooltips.Strip(button.gameObject);
        _withdrawButton = button;
        AttachWithdrawTooltip();
    }

    private static void CreateAuraPayToggle(InventoryGui gui, GameObject panel)
    {
        Button button = Object.Instantiate(gui.m_takeAllButton, panel.transform);
        button.name = AuraPayButtonName;
        button.transform.SetParent(panel.transform, false);
        Tooltips.Strip(button.gameObject);
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(ToggleAuraPay);
        DetachGamepadHint(button);

        button.GetComponent<RectTransform>().localPosition = new Vector3(2.5f, -38, 0);
        button.transform.localScale = new Vector3(0.4f, 0.4f, 1);
        _auraPayButton = button;
        RefreshAuraPayToggle();
    }

    private static void ToggleAuraPay()
    {
        bool enabled = !OttoPayApi.IsAuraPayEnabled();
        OttoPayApi.SetAuraPayEnabled(enabled);
        Player? player = Player.m_localPlayer;
        if (player != null)
            player.Message(MessageHud.MessageType.TopLeft, enabled ? "AuraPay on: ward auras may charge your Merchant Bank balance." : "AuraPay off: ward auras will not charge your balance.");
    }

    private static void AttachWithdrawTooltip()
    {
        if (_withdrawButton == null)
            return;
        Tooltips.Attach(_withdrawButton.gameObject, "Withdraw coins",
            "Take coins from your Merchant Bank balance and put them in your inventory.\n\nChoose how many in the dialog. Hold Ctrl when you click to take them all.\n\nDrop coins on your balance to deposit them again.");
    }

    /// The buttons are cloned from Take All, which carries a gamepad binding and its hint. Left
    /// in place, the gamepad key would press this button too.
    internal static void DetachGamepadHint(Button button)
    {
        UIGamePad? pad = button.GetComponent<UIGamePad>();
        if (pad == null)
            return;
        if (pad.m_hint != null)
            Object.Destroy(pad.m_hint);
        pad.m_hint = null;
        pad.m_zinputKey = string.Empty;
        pad.m_keyCode = KeyCode.None;
    }
}
