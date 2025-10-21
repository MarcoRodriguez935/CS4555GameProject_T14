using UnityEngine;
using UnityEngine.UI;

public class P2InventoryHUD : MonoBehaviour
{
    [Header("Inventory Settings")]
    public Image[] slots;              // Assign slots in the Inspector
    public RectTransform selector;     // The yellow highlight box
    private int currentIndex = 0;

    void Start()
    {
        UpdateSelector();
    }

    void Update()
    {
        // Move Right with P
        if (Input.GetKeyDown(KeyCode.P))
        {
            currentIndex = (currentIndex + 1) % slots.Length;
            UpdateSelector();
        }

        // Move Left with O
        if (Input.GetKeyDown(KeyCode.O))
        {
            currentIndex = (currentIndex - 1 + slots.Length) % slots.Length;
            UpdateSelector();
        }
    }

    void UpdateSelector()
    {
        // Use localPosition so it lines up correctly within the UI
        selector.localPosition = slots[currentIndex].rectTransform.localPosition;
    }
}