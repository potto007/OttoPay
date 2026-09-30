namespace OttoPay.UI;

/// Vanilla hover tooltips for the controls this mod adds. UITooltip handles pointer enter and
/// exit by itself, but every UITooltip shares one static tooltip object. Whichever component
/// hovers first decides which prefab that object is built from, and whichever hovers last
/// decides its text. Two rules keep the look consistent:
///   1. Every tooltip in this mod uses the same prefab, the boxed one from an inventory slot.
///   2. Only one UITooltip per control. Clones of vanilla widgets bring their own, with their
///      own prefab, so those are stripped before ours goes on.
internal static class Tooltips
{
    private static GameObject? _prefab;

    /// Only an inventory slot's prefab will do, because other vanilla tooltips use prefabs
    /// without the box. The grid builds its slots on the first redraw, which is after
    /// InventoryGui.Awake where this mod's controls are made, so the slot prefab is read first
    /// and a live slot second. Falling back to any loaded tooltip gave some controls an unboxed
    /// style.
    internal static GameObject? Prefab()
    {
        if (_prefab != null)
            return _prefab;

        InventoryGrid? grid = InventoryGui.instance != null ? InventoryGui.instance.m_playerGrid : null;
        if (grid == null)
            return null;
        UITooltip? slot = grid.m_elementPrefab != null ? grid.m_elementPrefab.GetComponentInChildren<UITooltip>(true) : null;
        if (slot == null || slot.m_tooltipPrefab == null)
            slot = grid.m_elements.FirstOrDefault()?.m_tooltip;
        if (slot == null || slot.m_tooltipPrefab == null)
            return null;

        _prefab = slot.m_tooltipPrefab;
        Log.Info($"Tooltips use the '{_prefab.name}' prefab.");
        return _prefab;
    }

    /// Removes every UITooltip in a cloned subtree, so a vanilla tooltip cannot answer the
    /// pointer before this mod's does.
    internal static void Strip(GameObject root)
    {
        foreach (UITooltip stale in root.GetComponentsInChildren<UITooltip>(true))
        {
            Log.Info($"Removed cloned tooltip on '{stale.name}' (prefab '{(stale.m_tooltipPrefab != null ? stale.m_tooltipPrefab.name : "none")}').");
            UnityEngine.Object.DestroyImmediate(stale);
        }
    }

    internal static UITooltip? Attach(GameObject? target, string topic, string text)
    {
        if (target == null)
            return null;

        GameObject? prefab = Prefab();
        if (prefab == null)
        {
            Log.Warning($"No tooltip prefab found yet, so '{topic}' has no tooltip.");
            return null;
        }

        UITooltip? tooltip = target.GetComponent<UITooltip>();
        if (tooltip == null)
            tooltip = target.AddComponent<UITooltip>();
        tooltip.m_tooltipPrefab = prefab;
        tooltip.m_topic = topic;
        tooltip.m_text = text;
        return tooltip;
    }
}
