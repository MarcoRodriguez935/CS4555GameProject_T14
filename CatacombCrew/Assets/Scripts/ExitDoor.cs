using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ExitDoor : MonoBehaviour
{
    public static bool betaMessageDisplayed = false;

    public GameObject betaMessage;

    [Header("Scene Settings")]
    [Tooltip("Name of the next level to load")]
    public string nextLevelName;

    [Header("Lever System Reference")]
    [Tooltip("Reference to the DoorUnlockManager that tracks levers")]
    public DoorUnlockManager doorUnlockManager;

    private bool isPlayerNear = false;
    private Transform playerInside;
    private Collider doorCollider;

    void Start()
    {
        doorCollider = GetComponent<Collider>();
        if (doorCollider != null)
        {
            doorCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError($"[ExitDoor] No Collider found on {name}! Please add one and enable IsTrigger.");
        }

        if (doorUnlockManager == null)
            Debug.LogWarning("[ExitDoor] DoorUnlockManager reference missing! Assign it in the Inspector.");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ExitDoor] Trigger entered by {other.name} (Tag: {other.tag})");

        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerInside = other.transform.root;
            Debug.Log("[ExitDoor] Player is near the door. Press R to attempt opening.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[ExitDoor] Trigger exited by {other.name}");

        if (playerInside != null && other.transform.root == playerInside)
        {
            isPlayerNear = false;
            playerInside = null;
            Debug.Log("[ExitDoor] Player left the door area.");
        }
    }

    void Update()
    {
        // --- Always keep the trigger active in case DoorUnlockManager modifies it ---
        if (doorCollider != null && !doorCollider.enabled)
        {
            doorCollider.enabled = true;
            Debug.LogWarning("[ExitDoor] Collider was disabled — re-enabling it automatically.");
        }

        // --- Optional safety: auto-revalidate player proximity if already inside bounds ---
        if (!isPlayerNear && playerInside == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                float dist = Vector3.Distance(playerObj.transform.position, transform.position);
                if (dist < 3f) // small radius for reactivation
                {
                    isPlayerNear = true;
                    playerInside = playerObj.transform;
                    Debug.Log("[ExitDoor] Player detected nearby again — restoring trigger state.");
                }
            }
        }

        // --- Player tries to open door ---
        if (isPlayerNear && Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[ExitDoor] 'R' pressed near door. Checking lever state...");
            TryOpenDoor();
        }
    }

    private void TryOpenDoor()
    {
        if (doorUnlockManager == null)
        {
            Debug.LogWarning("[ExitDoor] Cannot open door — DoorUnlockManager not assigned!");
            return;
        }

        if (doorUnlockManager.allLeversFlipped)
        {
            Debug.Log("[ExitDoor] All levers flipped! Opening next level...");
            LoadNextLevel();
        }
        else
        {
            Debug.Log("[ExitDoor]  Door locked! You must flip all the levers first.");
        }
    }

    private void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            Debug.Log($"[ExitDoor] Loading next level: {nextLevelName}");
            SceneManager.LoadScene(nextLevelName);
        }
        else if (string.IsNullOrEmpty(nextLevelName))
        {
            betaMessage.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            Debug.LogWarning("[ExitDoor]  Next level name not set in Inspector!");
        }
    }
}