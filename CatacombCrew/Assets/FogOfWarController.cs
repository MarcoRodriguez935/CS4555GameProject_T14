using UnityEngine;
using UnityEngine.UI;

public class FogOfWarController : MonoBehaviour
{
    [Header("Fog of War Settings")]
    public RawImage fogImage;
    public Transform[] players;
    public int textureSize = 256;
    public float revealRadius = 15f;

    [Header("Camera Settings")]
    public Camera minimapCamera;  // assign your Minimap Camera here

    private Texture2D revealTexture;
    private Color32[] colors;

    // Calculated automatically
    private Vector2 mapMin;
    private Vector2 mapMax;

    void Start()
    {
        if (minimapCamera == null)
        {
            Debug.LogError("FogOfWarController: Please assign your Minimap Camera.");
            return;
        }

        // Automatically compute map bounds from the minimap camera
        CalculateBoundsFromCamera();

        revealTexture = new Texture2D(textureSize, textureSize, TextureFormat.R8, false);
        colors = new Color32[textureSize * textureSize];
        ClearFog();

        // Apply the texture to the fog material
        if (fogImage.material != null)
            fogImage.material.SetTexture("_RevealMask", revealTexture);
    }

    void Update()
    {
        UpdateFog();
    }

    void CalculateBoundsFromCamera()
    {
        // Compute world-space bounds from camera size and aspect ratio
        float height = minimapCamera.orthographicSize * 2f;
        float width = height * minimapCamera.aspect;

        Vector3 camPos = minimapCamera.transform.position;
        mapMin = new Vector2(camPos.x - (width / 2f), camPos.z - (height / 2f));
        mapMax = new Vector2(camPos.x + (width / 2f), camPos.z + (height / 2f));

        Debug.Log($"[FogOfWarController] Auto map bounds: X({mapMin.x}, {mapMax.x}) Z({mapMin.y}, {mapMax.y})");
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
}