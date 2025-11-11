using UnityEngine;
using UnityEngine.UI;

public class HUDAutoConfigurator : MonoBehaviour
{
    [Header("Player Settings")]
    public bool isPlayer1 = true;

    [Header("Config Files")]
    public HUDConfig player1Config;
    public HUDConfig player2Config;

    [Header("Canvas Elements")]
    public CanvasScaler canvasScaler;
    public RectTransform minimap;
    public RectTransform fogOfWar;

    [Header("Player HUD")]
    public RectTransform healthBar;
    public RectTransform staminaBar;
    public RectTransform sanityBar;

    [Header("Inventory Bar")]
    public RectTransform inventoryBar;

    private HUDConfig activeConfig;

    void Awake()
    {
        // Pick which config to use
        activeConfig = isPlayer1 ? player1Config : player2Config;

        if (activeConfig == null)
        {
            Debug.LogWarning($"[{name}] No HUDConfig assigned for {(isPlayer1 ? "Player 1" : "Player 2")}.");
            return;
        }

        ApplyCanvasSettings();
        ApplyMinimap();
        ApplyBars();
        ApplyInventory();
    }

    void ApplyCanvasSettings()
    {
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = activeConfig.referenceResolution;
            canvasScaler.matchWidthOrHeight = activeConfig.matchWidthOrHeight;
        }
    }

    void ApplyMinimap()
    {
        if (minimap != null)
        {
            minimap.anchorMin = activeConfig.minimapAnchor;
            minimap.anchorMax = activeConfig.minimapAnchor;
            minimap.pivot = activeConfig.minimapPivot;
            minimap.anchoredPosition = activeConfig.minimapOffset;
            minimap.sizeDelta = activeConfig.minimapSize;
        }
    }

    void ApplyBars()
    {
        // Don’t flip the sign — just use the values as given in the config.
        float sideOffset = activeConfig.hudXOffset;

        if (healthBar != null)
            healthBar.anchoredPosition = new Vector2(sideOffset, activeConfig.healthBarY);

        if (staminaBar != null)
            staminaBar.anchoredPosition = new Vector2(sideOffset, activeConfig.staminaBarY);

        if (sanityBar != null)
            sanityBar.anchoredPosition = new Vector2(sideOffset, activeConfig.sanityBarY);
    }

    void ApplyInventory()
    {
        if (inventoryBar != null)
            inventoryBar.anchoredPosition = activeConfig.inventoryBarPos;
    }
}