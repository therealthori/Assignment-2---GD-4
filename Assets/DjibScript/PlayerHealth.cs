using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
   [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float testDamageAmount = 10f;

    [Header("UI")]
    public Image healthBarFill;
    public TMP_Text healthText;
    public TMP_Text respawnText;

    [Header("Respawn")]
    public float respawnDelay = 3f;
    public GameObject playerPrefab;
    public Transform respawnPoint;

    [Header("Invincibility")]
    public float invincibilityDuration = 2f;
    private bool isInvincible = false;

    [Header("Cameras")]
    public Camera playerCamera;
    public Camera deathCamera;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();

        if (deathCamera != null)
            deathCamera.gameObject.SetActive(false);

        if (respawnText != null)
            respawnText.gameObject.SetActive(false);

        // Give spawn protection on first spawn
        StartCoroutine(InvincibilityRoutine());
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isInvincible) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Player took damage: " + amount +
                  " | Current Health: " + currentHealth);

        UpdateUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

void Update()
{
    // Press F to take damage (for testing)
    if (Input.GetKeyDown(KeyCode.F))
    {
        TakeDamage(testDamageAmount);
    }
}
    void UpdateUI()
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = currentHealth / maxHealth;

        if (healthText != null)
            healthText.text = Mathf.RoundToInt(currentHealth) + " / " + maxHealth;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("💀 PLAYER DIED");

        // Switch cameras
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);

        if (deathCamera != null)
            deathCamera.gameObject.SetActive(true);

        DisablePlayer();

        StartCoroutine(RespawnRoutine());
    }


    void DisablePlayer()
    {
        // Disable movement/combat scripts here if needed
        // Example:
        // GetComponent<PlayerMovement>().enabled = false;
    }

    IEnumerator RespawnRoutine()
    {
        float timer = respawnDelay;

        if (respawnText != null)
            respawnText.gameObject.SetActive(true);

        while (timer > 0)
        {
            if (respawnText != null)
                respawnText.text = "Respawning in: " + Mathf.Ceil(timer);

            yield return new WaitForSeconds(1f);
            timer--;
        }

        if (respawnText != null)
            respawnText.gameObject.SetActive(false);

        // Spawn new player
        GameObject newPlayer = Instantiate(playerPrefab, respawnPoint.position, respawnPoint.rotation);

        // Enable new player camera
        Camera newCam = newPlayer.GetComponentInChildren<Camera>();
        if (newCam != null)
            newCam.gameObject.SetActive(true);

        // Disable death camera
        if (deathCamera != null)
            deathCamera.gameObject.SetActive(false);

        Destroy(gameObject);
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        Debug.Log("🛡️ Player is INVINCIBLE");

        yield return new WaitForSeconds(invincibilityDuration);

        isInvincible = false;

        Debug.Log("❌ Invincibility ended");
    }
}
