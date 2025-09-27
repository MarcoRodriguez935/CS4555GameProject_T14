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

            // parent to holdPoint
            currentItem.transform.SetParent(holdPoint);
            currentItem.transform.localPosition = Vector3.zero;
            currentItem.transform.localRotation = Quaternion.identity;

            // remove rigidbody while held
            Rigidbody rb = currentItem.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            // disable collider while held
            Collider col = currentItem.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    void DropItem()
    {
        if (currentItem != null)
        {
            // unparent from player
            currentItem.transform.SetParent(null);

            // add rigidbody back if missing
            Rigidbody rb = currentItem.GetComponent<Rigidbody>();
            if (rb == null) rb = currentItem.AddComponent<Rigidbody>();

            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.mass = 1f;

            // re-enable collider
            Collider col = currentItem.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // clear reference
            currentItem = null;
        }
    }
}