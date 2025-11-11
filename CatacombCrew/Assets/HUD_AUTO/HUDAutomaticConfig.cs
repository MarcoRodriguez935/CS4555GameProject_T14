using UnityEngine;

public class HUDAutoConfigSimple : MonoBehaviour
{
    [Header("HUD References")]
    public RectTransform player1HUD;
    public RectTransform player2HUD;

    [Header("Offsets (can be positive or negative)")]
    public Vector2 player1Offset = new Vector2(0, 0);
    public Vector2 player2Offset = new Vector2(0, 0);

    [Header("Apply on Start")]
    public bool applyOnStart = true;

    void Start()
    {
        if (applyOnStart)
        {
            ApplyOffsets();
        }
    }

    [ContextMenu("Apply Offsets Now")]
    public void ApplyOffsets()
    {
        if (player1HUD != null)
            player1HUD.anchoredPosition += player1Offset;

        if (player2HUD != null)
            player2HUD.anchoredPosition += player2Offset;
    }
}