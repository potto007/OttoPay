using System.Linq;

namespace OttoPay;

// Vanilla hover tooltips for the controls this mod adds. UITooltip already handles pointer
// enter and exit by itself, so attaching one is enough. It only needs the prefab that the
// vanilla UI uses, which is not reachable from a static field, so it is borrowed from a
// control that already has one.
internal static class Tooltips
{
    private static GameObject? _prefab;

    private static GameObject? Prefab()
    {
        if (_prefab != null) return _prefab;

        // First choice: an inventory slot, which always carries a tooltip.
        UITooltip? slot = InventoryGui.instance != null && InventoryGui.instance.m_playerGrid != null
            ? InventoryGui.instance.m_playerGrid.m_elements.FirstOrDefault()?.m_tooltip
            : null;
        if (slot != null && slot.m_tooltipPrefab != null)
        {
            _prefab = slot.m_tooltipPrefab;
            return _prefab;
        }

        // Fallback: any loaded tooltip that has the prefab set.
        foreach (UITooltip candidate in Resources.FindObjectsOfTypeAll<UITooltip>())
        {
            if (candidate != null && candidate.m_tooltipPrefab != null)
            {
                _prefab = candidate.m_tooltipPrefab;
                return _prefab;
            }
        }

        return null;
    }

    internal static void Attach(GameObject? target, string topic, string text)
    {
        if (target == null) return;

        GameObject? prefab = Prefab();
        if (prefab == null)
        {
            OttoPayPlugin.OttoPayLogger.LogDebug($"No tooltip prefab found yet, so '{topic}' has no tooltip.");
            return;
        }

        UITooltip tooltip = target.GetComponent<UITooltip>() ?? target.AddComponent<UITooltip>();
        tooltip.m_tooltipPrefab = prefab;
        tooltip.m_topic = topic;
        tooltip.m_text = text;
    }
}
