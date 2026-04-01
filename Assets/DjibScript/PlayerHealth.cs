using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
   [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log("Player spawned with health: " + currentHealth);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        Debug.Log("Player took damage: " + amount + 
                  " | Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("💀 PLAYER DIED");

        // Optional:
        // Destroy(gameObject);
        // Restart level, show UI, etc.
    }
}
