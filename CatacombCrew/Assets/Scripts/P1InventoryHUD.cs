using UnityEngine;
using UnityEngine.UI;

public class InventoryHUD : MonoBehaviour
{
    public Image[] slots;           // Assign slots in the Inspector
    public RectTransform selector;  // Assign the Selector (yellow box)
    private int currentIndex = 0;

    void Start()
    {
        UpdateSelector();
    }

    void Update()
    {
        // Move Right
        if (Input.GetKeyDown(KeyCode.E))
        {
            currentIndex = (currentIndex + 1) % slots.Length;
            UpdateSelector();
        }

        // Move Left
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentIndex = (currentIndex - 1 + slots.Length) % slots.Length;
            UpdateSelector();
        }
    }

    void UpdateSelector()
    {
        selector.position = slots[currentIndex].transform.position;
    }
}