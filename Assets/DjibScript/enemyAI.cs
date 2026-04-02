using UnityEngine;
using System.Collections;   
using System.Collections.Generic;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;

    [Header("Ranges")]
    public Transform patrolArea;  // Trigger BoxCollider for patrol zone
    public float sightRange = 10f;
    public float chaseRange = 7f;
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 2f;

    [Header("Patrol")]
    public Transform[] patrolPoints;  // Auto-populate from patrolArea children
    private int currentPatrolIndex = 0;

    private enum State { Patrol, Idle, Chase, Attack }
    private State currentState = State.Patrol;
    private float lastAttackTime;
    private bool playerInSight;
    private bool playerInPatrolArea;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (patrolArea == null) patrolArea = GameObject.FindWithTag("PatrolArea").transform;  // Optional tag for area
        PopulatePatrolPoints();
        player = GameObject.FindWithTag("Player").transform;
        agent.autoBraking = false;
    }

    void Update()
    {
        playerInPatrolArea = patrolArea != null && patrolArea.GetComponent<Collider>().bounds.Contains(transform.position);
        if (!playerInPatrolArea) {
            GoToClosestPatrolPoint();  // Stay in area
            return;
        }

        UpdateAnimations();
        UpdateState();
    }

    void PopulatePatrolPoints()
    {
        if (patrolArea.childCount > 0)
            patrolPoints = new Transform[patrolArea.childCount];
        for (int i = 0; i < patrolArea.childCount; i++)
            patrolPoints[i] = patrolArea.GetChild(i);
    }

    void UpdateState()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            currentState = State.Attack;
            agent.SetDestination(transform.position);  // Stop moving
        }
        else if (distToPlayer <= chaseRange)
        {
            currentState = State.Chase;
            agent.SetDestination(player.position);
        }
        else if (playerInSight && distToPlayer <= sightRange)
        {
            currentState = State.Chase;
            agent.SetDestination(player.position);
        }
        else
        {
            currentState = State.Patrol;
        }
    }

    void UpdateAnimations()
    {
        animator.SetBool("IsAttacking", currentState == State.Attack);
        animator.SetBool("IsChasing", currentState == State.Chase);
        animator.SetBool("IsWalking", (currentState == State.Patrol || currentState == State.Chase) && agent.velocity.magnitude > 0.1f);
        animator.SetBool("IsIdle", currentState == State.Idle || agent.velocity.magnitude < 0.1f);
    }

    void GoToClosestPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        Transform closest = patrolPoints[0];
        float closestDist = Vector3.Distance(transform.position, closest.position);
        for (int i = 1; i < patrolPoints.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (dist < closestDist)
            {
                closest = patrolPoints[i];
                closestDist = dist;
            }
        }
        agent.SetDestination(closest.position);
        if (agent.remainingDistance < 0.5f)
            currentState = State.Idle;
    }

    // Trigger events from SphereColliders (Sight/Chase use OnTriggerEnter/Exit)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInSight = true;
            Debug.Log("Player sighted by " + gameObject.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInSight = false;
            Debug.Log("Player out of sight for " + gameObject.name);
        }
    }

    // Attack animation event or in Update for Attack state
    public void PerformAttack()
    {
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            player.GetComponent<PlayerHealth>().TakeDamage(attackDamage);  // Assume PlayerHealth script
            lastAttackTime = Time.time;
        }
    }

    // PatrolArea trigger (on PatrolArea script or layer check)
    void OnTriggerStay(Collider other)  // For patrol area if needed
    {
        // Patrol logic handled in Update
    }
}
