using UnityEngine;

public class Lantern : MonoBehaviour
{
    private Light lanternLight;

    void Awake()
    {
        lanternLight = GetComponentInChildren<Light>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            lanternLight.enabled = !lanternLight.enabled;
        }
    }
}