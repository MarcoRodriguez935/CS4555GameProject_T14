using UnityEngine;

[CreateAssetMenu(fileName = "HUDConfig", menuName = "CatacombCrew/HUD Config")]
public class HUDConfig : ScriptableObject
{
    [Header("Canvas Scaler Settings")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;

    [Header("Minimap Settings")]
    public Vector2 minimapAnchor = new Vector2(0.5f, 1f);
    public Vector2 minimapPivot = new Vector2(0.5f, 1f);
    public Vector2 minimapOffset = new Vector2(0f, -50f);
    public Vector2 minimapSize = new Vector2(300f, 300f);

    [Header("HUD Positioning")]
    public float hudXOffset = 800f;
    public float hudY = -100f;
    public float healthBarY = -50f;
    public float staminaBarY = -70f;
    public float sanityBarY = -90f;

    [Header("Inventory Settings")]
    public Vector2 inventoryBarPos = new Vector2(0f, 100f);
}