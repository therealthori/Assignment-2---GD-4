using System.Collections;
using UnityEngine;

public class Molotov : MonoBehaviour
{
    [Header("Fire Settings")]
    public float fireDamage = 5f;           
    public float fireTickRate = 0.5f;       
    public float fireDuration = 4f;         
    public float fireRadius = 3f;           

    [Header("Effects")]
    public GameObject fireEffectPrefab;     
    public GameObject bottlePrefab;         

    private bool hasExploded = false;

    void OnCollisionEnter(Collision collision)
    {
        if (!hasExploded)
        {
            hasExploded = true;
            Explode(collision.contacts[0].point);
        }
    }

    void Explode(Vector3 impactPoint)
    {
        // Spawn fire effect at impact
        if (fireEffectPrefab != null)
        {
            GameObject fire = Instantiate(fireEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(fire, fireDuration);
        }

        // Start burning all enemies in radius
        StartCoroutine(BurnArea(impactPoint));

        // Hide the bottle immediately
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }

    IEnumerator BurnArea(Vector3 fireOrigin)
    {
        float elapsed = 0f;

        while (elapsed < fireDuration)
        {
            // Find all enemies in fire radius every tick
            Collider[] hits = Physics.OverlapSphere(fireOrigin, fireRadius);

            foreach (Collider hit in hits)
            {
                CombatController enemy = hit.GetComponent<CombatController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(fireDamage);       

                    // Trigger gethit animation
                    AIEnemy ai = hit.GetComponent<AIEnemy>();
                }
            }

            elapsed += fireTickRate;
            yield return new WaitForSeconds(fireTickRate);
        }

        Destroy(gameObject);
    }

    // Draw fire radius in editor for easy tuning
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fireRadius);
    }
}
