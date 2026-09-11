using System.Collections;
using TMPro;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OttoPay;

// The coin balance belongs to the Merchant Bank Network. A player joins at any merchant's store
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
            // Shrink to fit rather than wrap. The label is three words in a one line button,
            // and wrapping pushed half of it outside the button box.
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
        }

        RectTransform sell = gui.m_sellButton.GetComponent<RectTransform>();
        RectTransform buy = gui.m_buyButton.GetComponent<RectTransform>();
        RectTransform rect = button.GetComponent<RectTransform>();

        // Read the real rendered size before touching anchors. Buy is stretch anchored, so its
        // sizeDelta is an inset from the parent and is negative. Copying Sell's point anchors
        // turns that inset into an absolute width, which made the button 40 pixels wide in the
        // negative direction and therefore invisible.
        Vector2 wanted = new(Mathf.Max(buy.rect.width, 210f), Mathf.Max(sell.rect.height, 36f));

        // Copy the Sell button's anchoring so the offset below is measured against the same
        // corner. Buy and Sell do not share anchors.
        rect.anchorMin = sell.anchorMin;
        rect.anchorMax = sell.anchorMax;
        rect.pivot = sell.pivot;

        // sizeDelta is measured on top of whatever the anchors already span, so subtract that
        // span to land on the size we actually want, whatever the anchors turn out to be.
        Vector2 span = Vector2.zero;
        if (button.transform.parent is RectTransform parent)
        {
            Vector2 parentSize = parent.rect.size;
            span = new Vector2((rect.anchorMax.x - rect.anchorMin.x) * parentSize.x,
                               (rect.anchorMax.y - rect.anchorMin.y) * parentSize.y);
        }

        rect.sizeDelta = wanted - span;

        // Line the left edges up instead of the centres. Sell is a narrow icon, so a centred
        // wide button grows back under the store panel and hides its own first word.
        // anchoredPosition addresses the pivot, so the shift is scaled by the pivot.
        float alignLeft = rect.pivot.x * (wanted.x - sell.rect.width);
        rect.anchoredPosition = sell.anchoredPosition + new Vector2(alignLeft, sell.rect.height + 8f);

        Tooltips.Strip(button.gameObject);
        Tooltips.Attach(button.gameObject, "Merchant Bank",
            "Join the Merchant Bank Network.\n\nCoins you pick up vanish into your Merchant Bank balance, held for you by merchant magic. They take no inventory slot and weigh nothing.\n\nAny merchant draws on your balance when you buy, and you can call coins back into your hand whenever you like.");

        _joinButton = button;
        Vector2 got = rect.rect.size;
        OttoPayPlugin.OttoPayLogger.LogInfo(
            $"Merchant Bank join button created at {rect.anchoredPosition}, size {got}, wanted {wanted}, labels {labels.Length}.");
        if (got.x <= 1f || got.y <= 1f)
        {
            OttoPayPlugin.OttoPayLogger.LogWarning(
                $"Join button has a degenerate size {got} and will not be visible. Falling back to a fixed size.");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = wanted;
        }
    }

    private static void Join(StoreGui gui)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || OttoPayApi.IsBankMember()) return;

        OttoPayApi.JoinBank();
        string merchant = gui.m_trader != null ? Localization.instance.Localize(gui.m_trader.m_name) : "The merchant";
        string welcome = $"{merchant} binds you to the Merchant Bank Network.\nCoins you pick up now go straight to your balance.";
        // The heads up display draws behind the store window, so the same line is also shown
        // inside the window. Otherwise a player only sees it by closing the shop in time.
        player.Message(MessageHud.MessageType.Center, welcome);
        ShowStoreMessage(gui, welcome);

        if (_joinButton != null)
        {
            _joinButton.gameObject.SetActive(false);
        }

        gui.FillList();
    }

    private static GameObject? _message;

    // Drawn as the last child of the store panel, so it sits in front of everything the
    // panel draws. A heads up display message cannot do that.
    private static void ShowStoreMessage(StoreGui gui, string text)
    {
        if (gui.m_rootPanel == null) return;
        if (_message != null) Object.Destroy(_message);

        GameObject panel = new("OttoPayStoreMessage", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(gui.m_rootPanel.transform, false);
        panel.transform.SetAsLastSibling();

        RectTransform rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(250f, 130f);
        rect.anchoredPosition = Vector2.zero;

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.06f, 0.05f, 0.04f, 0.93f);
        background.raycastTarget = false;

        GameObject textObject = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 12f);
        textRect.offsetMax = new Vector2(-12f, -12f);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 18f;
        label.raycastTarget = false;
        if (gui.m_coinText != null)
        {
            label.font = gui.m_coinText.font;
            label.color = gui.m_coinText.color;
        }

        _message = panel;
        gui.StartCoroutine(HideMessageAfter(6f));
    }

    private static IEnumerator HideMessageAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (_message != null)
        {
            Object.Destroy(_message);
            _message = null;
        }
    }
}
