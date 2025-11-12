using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FogOfWarController : MonoBehaviour
{
    [Header("Fog of War Settings")]
    public RawImage fogImage;
    [Tooltip("Assign both players here (P1, P2).")]
    public Transform[] players;
    public int textureSize = 256;
    public float revealRadius = 15f;

    [Header("Per-Player Offsets (world units)")]
    [Tooltip("Offset (x = left/right, y = forward/back) for each player. Size must match number of players.")]
    public Vector2[] playerOffsets;

    [Header("Camera Settings")]
    public Camera minimapCamera;
    [Tooltip("Optional: root or empty object representing map center (instead of using camera position).")]
    public Transform mapOrigin;

    [Header("Manual Adjustments")]
    public Vector2 globalOffset = Vector2.zero;
    public float scaleAdjust = 1f;
    public float defaultOrthoSize = 60f;
    public float orthoMin = 10f;
    public float orthoMax = 120f;

    private Texture2D revealTexture;
    private Color32[] colors;
    private Vector2 mapMin;
    private Vector2 mapMax;

    void Start()
    {
        if (minimapCamera == null)
        {
            Debug.LogError("[FogOfWarController] Missing Minimap Camera!");
            return;
        }

        ClampOrthoSize();
        CalculateBoundsFromCamera();

        revealTexture = new Texture2D(textureSize, textureSize, TextureFormat.R8, false);
        colors = new Color32[textureSize * textureSize];
        ClearFog();

        if (fogImage != null && fogImage.material != null)
            fogImage.material.SetTexture("_RevealMask", revealTexture);
    }

    void Update()
    {
        UpdateFog();
    }

    // --- Utility Functions ---
    private void ClampOrthoSize()
    {
        if (minimapCamera.orthographicSize < orthoMin || minimapCamera.orthographicSize > orthoMax)
        {
            Debug.LogWarning($"[FogOfWarController] Clamping minimap ortho size to {defaultOrthoSize}");
            minimapCamera.orthographicSize = defaultOrthoSize;
        }
    }

    private void CalculateBoundsFromCamera()
    {
        ClampOrthoSize();

        float height = minimapCamera.orthographicSize * 2f * scaleAdjust;
        float width = height * minimapCamera.aspect;

        Vector3 origin = mapOrigin != null ? mapOrigin.position : minimapCamera.transform.position;

        mapMin = new Vector2(origin.x - (width / 2f), origin.z - (height / 2f));
        mapMax = new Vector2(origin.x + (width / 2f), origin.z + (height / 2f));

        Debug.Log($"[FogOfWarController] Map bounds recalculated: X({mapMin.x}, {mapMax.x}) Z({mapMin.y}, {mapMax.y})");
    }

    private void ClearFog()
    {
        for (int i = 0; i < colors.Length; i++)
            colors[i] = new Color32(0, 0, 0, 255);
        revealTexture.SetPixels32(colors);
        revealTexture.Apply();
    }

    private void UpdateFog()
    {
        if (revealTexture == null || fogImage == null || players == null)
            return;

        foreach (Transform player in players)
        {
            if (player == null) continue;

            int index = System.Array.IndexOf(players, player);
            Vector2 offset = (playerOffsets != null && index >= 0 && index < playerOffsets.Length)
                ? playerOffsets[index]
                : Vector2.zero;

            Vector3 pos = player.position + new Vector3(
                globalOffset.x + offset.x,
                0f,
                globalOffset.y + offset.y
            );

            float nx = Mathf.InverseLerp(mapMin.x, mapMax.x, pos.x);
            float ny = Mathf.InverseLerp(mapMin.y, mapMax.y, pos.z);

            int x = Mathf.FloorToInt(nx * textureSize);
            int y = Mathf.FloorToInt(ny * textureSize);

            int radius = Mathf.RoundToInt(revealRadius);
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    int px = x + i;
                    int py = y + j;
                    if (px >= 0 && py >= 0 && px < textureSize && py < textureSize)
                    {
                        float dist = Mathf.Sqrt(i * i + j * j);
                        if (dist < radius)
                        {
                            float t = Mathf.Clamp01(dist / radius);
                            byte value = (byte)(255 * (1 - t));

                            int idx = py * textureSize + px;
                            if (colors[idx].r < value)
                                colors[idx] = new Color32(value, value, value, 255);
                        }
                    }
                }
            }
        }

        revealTexture.SetPixels32(colors);
        revealTexture.Apply();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Map bounds
        Gizmos.color = Color.cyan;
        Vector3 bottomLeft = new Vector3(mapMin.x, 0, mapMin.y);
        Vector3 topRight = new Vector3(mapMax.x, 0, mapMax.y);
        Vector3 center = (bottomLeft + topRight) / 2f;
        Vector3 size = new Vector3(mapMax.x - mapMin.x, 0.1f, mapMax.y - mapMin.y);
        Gizmos.DrawWireCube(center, size);

        // Reveal areas
        if (players == null) return;

        for (int p = 0; p < players.Length; p++)
        {
            if (players[p] == null) continue;
            Vector2 off = (playerOffsets != null && p < playerOffsets.Length) ? playerOffsets[p] : Vector2.zero;

            Vector3 pos = players[p].position + new Vector3(globalOffset.x + off.x, 0f, globalOffset.y + off.y);
            Gizmos.color = (p == 0) ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(pos, revealRadius);
        }
    }
#endif
}