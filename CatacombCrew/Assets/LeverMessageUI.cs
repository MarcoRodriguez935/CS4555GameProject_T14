using UnityEngine;
using TMPro;

public class LeverMessageUI : MonoBehaviour
{
    public static LeverMessageUI instance;

    public TextMeshProUGUI messageText;
    public float displayTime = 2f;

    private float timer = 0f;

    void Awake()
    {
        instance = this;
        messageText.alpha = 0; // hidden at start
    }

    public void ShowMessage(string msg)
    {
        messageText.text = msg;
        messageText.alpha = 1;   // show message
        timer = displayTime;     // reset timer
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
                messageText.alpha = 0; // hide
        }
    }
}