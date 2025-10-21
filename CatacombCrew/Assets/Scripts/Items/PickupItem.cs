using UnityEngine;

public class PickupItem : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            Inventory inv = other.GetComponent<Inventory>();
            if (inv != null)
            {
                inv.PickUpItem(gameObject); // pass THIS lantern object
            }
        }
    }
}