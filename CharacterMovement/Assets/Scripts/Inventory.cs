using UnityEngine;

public class Inventory : MonoBehaviour
{
    public Transform holdPoint;   // where the item will sit in the player's hand
    private GameObject currentItem;

    void Update()
    {
        // Drop item with Q
        if (currentItem != null && Input.GetKeyDown(KeyCode.Q))
        {
            DropItem();
        }
    }

    public void PickUpItem(GameObject item)
    {
        if (currentItem == null)
        {
            currentItem = item;

            // Parent to hold point
            currentItem.transform.SetParent(holdPoint);
            currentItem.transform.localPosition = Vector3.zero;
            currentItem.transform.localRotation = Quaternion.identity;

            // Disable physics while held
            Rigidbody rb = currentItem.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; // safer than Destroy
            Collider col = currentItem.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    void DropItem()
    {
        if (currentItem != null)
        {
            currentItem.transform.SetParent(null);

            // Re-enable physics
            Rigidbody rb = currentItem.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false; // let gravity work
            Collider col = currentItem.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            currentItem = null;
        }
    }
}