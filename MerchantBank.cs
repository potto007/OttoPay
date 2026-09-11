using TMPro;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OttoPay;

// The pouch belongs to the Merchant Bank Network. A player joins at any merchant's store
// window, and until then coins behave exactly as they do in vanilla.
[HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
static class MerchantBankJoinButton
{
    private const string JoinButtonName = "MerchantBankJoinButton";
    private const string JoinLabel = "Join Merchant Bank";
    private static Button? _joinButton;

    static void Postfix(StoreGui __instance)
    {
        if (_joinButton == null)
        {
            Create(__instance);
        }

        if (_joinButton != null)
        {
            _joinButton.gameObject.SetActive(!OttoPayApi.IsBankMember());
        }
    }

    private static void Create(StoreGui gui)
    {
        // Cloned from the Buy button, not the Sell button. Sell is a bare coin icon with no
        // text child, so a clone of it kept the coin art and silently dropped the label. That
        // put a second, unexplained Sell icon next to the real one.
        if (gui.m_buyButton == null || gui.m_sellButton == null) return;

        Button button = Object.Instantiate(gui.m_buyButton, gui.m_sellButton.transform.parent);
        button.name = JoinButtonName;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => Join(gui));
        button.interactable = true;

        UIGamePad? pad = button.GetComponent<UIGamePad>();
        if (pad != null)
        {
            if (pad.m_hint) Object.Destroy(pad.m_hint);
            pad.m_hint = null;
            pad.m_zinputKey = string.Empty;
            pad.m_keyCode = KeyCode.None;
        }

        // Localize.Start re-localizes its whole subtree one frame after the clone appears and
        // would put the Buy token back over our label. The clone carries no vanilla token, so
        // the component has nothing to do here.
        foreach (Localize stale in button.GetComponentsInChildren<Localize>(true))
        {
            Object.Destroy(stale);
        }

        TextMeshProUGUI[] labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            label.text = JoinLabel;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
        }

        RectTransform sell = gui.m_sellButton.GetComponent<RectTransform>();
        RectTransform rect = button.GetComponent<RectTransform>();
        // Copy the Sell button's anchoring so the offset below is measured against the same
        // corner. Buy and Sell do not share anchors.
        rect.anchorMin = sell.anchorMin;
        rect.anchorMax = sell.anchorMax;
        rect.pivot = sell.pivot;
        rect.anchoredPosition = sell.anchoredPosition + new Vector2(0f, sell.rect.height + 8f);

        _joinButton = button;
        OttoPayPlugin.OttoPayLogger.LogInfo(
            $"Merchant Bank join button created at {rect.anchoredPosition}, size {rect.rect.size}, labels {labels.Length}.");
    }

    private static void Join(StoreGui gui)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || OttoPayApi.IsBankMember()) return;

        OttoPayApi.JoinBank();
        string merchant = gui.m_trader != null ? Localization.instance.Localize(gui.m_trader.m_name) : "The merchant";
        player.Message(MessageHud.MessageType.Center, $"{merchant} signs you into the Merchant Bank Network. Coins you pick up now go to your pouch.");
        if (_joinButton != null)
        {
            _joinButton.gameObject.SetActive(false);
        }

        gui.FillList();
    }
}
