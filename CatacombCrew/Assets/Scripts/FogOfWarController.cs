using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FogOfWarController : MonoBehaviour
{
    [Header("Fog of War Settings")]
    public RawImage fogImage;
    public Transform[] players;
    public int textureSize = 256;
    public float revealRadius = 15f;

    [Header("Camera Settings")]
    public Camera minimapCamera;

    [Header("Auto-Alignment Settings")]
    public Transform mapRoot; // ✅ assign your ground / level root object
    public float padding = 5f; // extra margin around map edges
    public bool autoAlignOnStart = true;

    [Header("Manual Tweaks (if needed)")]
    public Vector2 positionOffset = Vector2.zero; // fine-tune offset
    public float scaleAdjust = 1f; // adjust if map feels zoomed

    private Texture2D revealTexture;
    private Color32[] colors;

    private Vector2 mapMin;
    private Vector2 mapMax;

    void Start()
    {
        if (minimapCamera == null)
        {
            Debug.LogError("FogOfWarController: Please assign your Minimap Camera.");
            return;
        }

        if (autoAlignOnStart && mapRoot != null)
            AutoCalibrateMap();

        CalculateBoundsFromCamera();

        revealTexture = new Texture2D(textureSize, textureSize, TextureFormat.R8, false);
        colors = new Color32[textureSize * textureSize];
        ClearFog();

        if (fogImage.material != null)
            fogImage.material.SetTexture("_RevealMask", revealTexture);
    }

    void Update()
    {
        UpdateFog();
    }

    // ✅ Automatically centers and scales minimap to mapRoot bounds
    void AutoCalibrateMap()
    {
        Renderer[] renderers = mapRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[FogOfWarController] No renderers found under mapRoot.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        Vector3 center = bounds.center;
        float width = bounds.size.x + padding;
        float height = bounds.size.z + padding;

        // Center minimap camera above the map
        minimapCamera.transform.position = new Vector3(center.x, minimapCamera.transform.position.y, center.z);

        // Fit camera to bounds
        minimapCamera.orthographicSize = Mathf.Max(width, height) / 2f * scaleAdjust;

        Debug.Log($"[FogOfWarController] Auto-calibrated minimap to map bounds at {center} (size {width}x{height})");
    }

    void CalculateBoundsFromCamera()
    {
        float height = minimapCamera.orthographicSize * 2f * scaleAdjust;
        float width = height * minimapCamera.aspect;

        Vector3 camPos = minimapCamera.transform.position;
        mapMin = new Vector2(camPos.x - (width / 2f), camPos.z - (height / 2f));
        mapMax = new Vector2(camPos.x + (width / 2f), camPos.z + (height / 2f));

        Debug.Log($"[FogOfWarController] Map bounds recalculated: X({mapMin.x}, {mapMax.x}) Z({mapMin.y}, {mapMax.y})");
    }

    void ClearFog()
    {
        for (int i = 0; i < colors.Length; i++)
            colors[i] = new Color32(0, 0, 0, 255);
        revealTexture.SetPixels32(colors);
        revealTexture.Apply();
    }

    void UpdateFog()
    {
        if (revealTexture == null || fogImage == null)
            return;

        foreach (Transform player in players)
        {
            if (player == null) continue;

            Vector3 pos = player.position;
            pos.x += positionOffset.x;
            pos.z += positionOffset.y;

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

                            int index = py * textureSize + px;
                            if (colors[index].r < value)
                                colors[index] = new Color32(value, value, value, 255);
                        }
                    }
                }
            }
        }

        revealTexture.SetPixels32(colors);
        revealTexture.Apply();
    }

#if UNITY_EDITOR
    // ✅ Visual Gizmos for alignment debugging in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 bottomLeft = new Vector3(mapMin.x, 0, mapMin.y);
        Vector3 topRight = new Vector3(mapMax.x, 0, mapMax.y);
        Vector3 center = (bottomLeft + topRight) / 2f;
        Vector3 size = new Vector3(mapMax.x - mapMin.x, 0.1f, mapMax.y - mapMin.y);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}