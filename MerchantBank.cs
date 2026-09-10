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
        Button button = Object.Instantiate(gui.m_sellButton, gui.m_sellButton.transform.parent);
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

        TextMeshProUGUI? label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = "Join Merchant Bank";
        }

        RectTransform sell = gui.m_sellButton.GetComponent<RectTransform>();
        button.GetComponent<RectTransform>().anchoredPosition = sell.anchoredPosition + new Vector2(0f, sell.rect.height + 8f);
        _joinButton = button;
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
