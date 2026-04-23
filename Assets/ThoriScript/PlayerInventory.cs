using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    [Header("References")]
        public Transform holdPoint;             // Empty GameObject where held weapon sits
        public TextMeshProUGUI itemNameText;    // Optional UI label
        public TextMeshProUGUI ammoText;        // Optional ammo counter
    
        private GameObject heldWeapon;
        private ObjectLogic currentThrowLogic; // Your existing throw script
        private Coroutine activePowerup;
    
        public void PickUpWeapon(Pickup pickup)
        {
            // Drop current weapon first if holding one
            if (heldWeapon != null)
                DropCurrentWeapon();
    
            // Spawn weapon at hold point
            heldWeapon = Instantiate(pickup.weaponPrefab, holdPoint.position, holdPoint.rotation);
            heldWeapon.transform.SetParent(holdPoint);  // ✅ Attach to player's hand
    
            // Hook into your existing throw script
            currentThrowLogic = heldWeapon.GetComponent<ObjectLogic>();
            if (currentThrowLogic != null)
            {
                currentThrowLogic.totalThrows = pickup.ammoAmount;
                currentThrowLogic.cam = Camera.main.transform;
                currentThrowLogic.attackPoint = holdPoint;
            }
    
            UpdateUI(pickup.itemName, pickup.ammoAmount);
            Debug.Log("Picked up: " + pickup.itemName);
        }
    
        public void PickUpPowerup(Pickup pickup)
        {
            // Cancel existing powerup if one is running
            if (activePowerup != null)
                StopCoroutine(activePowerup);
    
            activePowerup = StartCoroutine(ApplyPowerup(pickup));
            Debug.Log("Used powerup: " + pickup.itemName);
        }
    
        IEnumerator ApplyPowerup(Pickup pickup)
        {
            // Heal
            if (pickup.healAmount > 0)
            {
                HealthBar playerHealth = GetComponent<HealthBar>();
                if (playerHealth != null)
                    playerHealth.health = Mathf.Min(
                        playerHealth.health + pickup.healAmount,
                        playerHealth.maxHealth
                    );
            }
    
            // Speed boost
            enemyAI movement = GetComponent<enemyAI>();     // Swap with your player movement script
            // Add your own player speed boost logic here
    
            UpdateUI(pickup.itemName + " active!", -1);
            yield return new WaitForSeconds(pickup.powerupDuration);
    
            // Revert speed after duration
            UpdateUI("", 0);
            activePowerup = null;
        }
    
        void DropCurrentWeapon()
        {
            if (heldWeapon == null) return;
    
            heldWeapon.transform.SetParent(null);   // Detach from hand
    
            Rigidbody rb = heldWeapon.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
            }
    
            heldWeapon = null;
            currentThrowLogic = null;
            UpdateUI("", 0);
        }
    
        void UpdateUI(string name, int ammo)
        {
            if (itemNameText != null)
                itemNameText.text = name;
    
            if (ammoText != null)
                ammoText.text = ammo > 0 ? "x" + ammo : "";
        }
}
