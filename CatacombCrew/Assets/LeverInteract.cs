using UnityEngine;

public class LeverInteract : MonoBehaviour
{
    private LeverSwitch lever;
    private bool isPlayerNear = false;

    void Start()
    {
        lever = GetComponent<LeverSwitch>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            Debug.Log("Press R to flip lever");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.R))
        {
            if (lever != null && !lever.isFlipped)
                lever.FlipLever();
        }
    }
}