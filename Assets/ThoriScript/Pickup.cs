using UnityEngine;

public class Pickup : MonoBehaviour
{
    public enum PickupType { Weapon, Powerup }
    
        [Header("Pickup Settings")]
        public PickupType pickupType;
        public string itemName = "Item";
        public float pickupRadius = 1.5f;
        public KeyCode pickupKey = KeyCode.E;
    
        [Header("If Weapon")]
        public GameObject weaponPrefab;         // What gets held in player's hand
        public int ammoAmount = 3;              // e.g. 3 molotovs
    
        [Header("If Powerup")]
        public float healAmount = 25f;
        public float speedBoost = 2f;
        public float powerupDuration = 5f;
    
        [Header("UI")]
        public GameObject promptUI;             // "Press E to pick up" prompt
    
        private Transform player;
        private bool playerInRange = false;
    
        void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
    
            if (promptUI != null)
                promptUI.SetActive(false);
        }
    
        void Update()
        {
            float distance = Vector3.Distance(transform.position, player.position);
            playerInRange = distance <= pickupRadius;
    
            // Show/hide prompt
            if (promptUI != null)
                promptUI.SetActive(playerInRange);
    
            if (playerInRange && Input.GetKeyDown(pickupKey))
            {
                PickUp();
            }
        }
    
        void PickUp()
        {
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) return;
    
            if (pickupType == PickupType.Weapon)
                inventory.PickUpWeapon(this);
            else
                inventory.PickUpPowerup(this);
    
            if (promptUI != null)
                promptUI.SetActive(false);
    
            Destroy(gameObject);
        }
    
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
}
