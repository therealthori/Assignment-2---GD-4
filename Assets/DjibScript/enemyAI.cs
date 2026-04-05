using UnityEngine;
using System.Collections;   
using System.Collections.Generic;
using UnityEngine.AI;   

public class enemyAI : MonoBehaviour
{
    public float moveSpeed = 5f;              // How fast the enemy moves
    public float detectionRange = 20f;        // Only chase if a player is within this distance

    private Rigidbody rb;
    private Transform closestPlayer;
    private string playerTag = "Player";


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("EnemyAI: missing Rigidbody component!");
        }
    }


    void Update()
    {
        FindClosestPlayer();
        if (closestPlayer != null)
        {
            Vector3 direction = (closestPlayer.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            // Optional: rotate to face the player
            transform.LookAt(closestPlayer);
        }
    }


    void FindClosestPlayer()
    {
        // Find all objects with the "Player" tag
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);

        closestPlayer = null;
        float closestDistance = detectionRange;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player.transform;
            }
        }
    }
}
