using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthBar;
    public Slider staminaBar;
    public Slider sanityBar;

    [Header("Player Attributes")]
    public float maxHealth = 100f;
    public float maxStamina = 100f;
    public float maxSanity = 100f;

    public float currentHealth;
    public float currentStamina;
    public float currentSanity;

    //  Sprinting state
    private bool isSprinting = false;

    // Public read-only properties (for safe external access)
    public float CurrentHealth => currentHealth;
    public float CurrentStamina => currentStamina;
    public float CurrentSanity => currentSanity;
    public bool IsSprinting => isSprinting; // ✅ Added read-only sprint flag

    void Start()
    {
        // Initialize all stats at max
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentSanity = maxSanity;
        UpdateUI();
    }

    void Update() => UpdateUI();

    private void UpdateUI()
    {
        if (healthBar != null)
            healthBar.value = currentHealth / maxHealth;

        if (staminaBar != null)
            staminaBar.value = currentStamina / maxStamina;

        if (sanityBar != null)
            sanityBar.value = currentSanity / maxSanity;
    }

    // ======================
    //    Public Methods
    // ======================

    //  Damage / Healing
    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateUI();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateUI();
    }

    //  Stamina (for sprinting)
    public void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
        UpdateUI();
    }

    public void RegainStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        UpdateUI();
    }

    //  Sanity
    public void ReduceSanity(float amount)
    {
        currentSanity = Mathf.Max(0, currentSanity - amount);
        UpdateUI();
    }

    public void RegainSanity(float amount)
    {
        currentSanity = Mathf.Min(maxSanity, currentSanity + amount);
        UpdateUI();
    }

    //  Sprint control (added for PlayerControl)
    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }
}