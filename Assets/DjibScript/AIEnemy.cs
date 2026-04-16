using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class AIEnemy : NetworkBehaviour
{
  public enum EnemyState { Patrol, Chase, Attack, Dead }

    [Header("Ranges")]
    public float detectionRange = 15f;
    public float attackRange = 2.5f;

    [Header("Movement")]
    public float patrolRadius = 10f;

    public float walkSpeed = 2f;   // 🟢 NEW
    public float runSpeed = 5f;    // 🟢 NEW

    [Header("Nav Fix (IMPORTANT)")]
    public float stoppingDistanceBuffer = 0.3f;

    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;

    private Transform targetPlayer;
    private EnemyState currentState;

    private Vector3 patrolPoint;
    private bool patrolPointSet;

    private string playerTag = "Player";

    private NetworkVariable<EnemyState> netState = new NetworkVariable<EnemyState>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentState = EnemyState.Patrol;
            netState.Value = currentState;
            Debug.Log("[AI] Spawned → PATROL");
        }
    }

    void Start()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening)
        {
            currentState = EnemyState.Patrol;
        }

        // 🔥 NAV FIXES (important for overshoot)
        agent.stoppingDistance = attackRange - stoppingDistanceBuffer;
        agent.autoBraking = true;
        agent.acceleration = 30f;
        agent.angularSpeed = 720f;
    }

    void Update()
    {
        bool isMultiplayerRunning =
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening;

        if (isMultiplayerRunning && !IsServer)
        {
            UpdateAnimations(netState.Value);
            return;
        }

        FindClosestPlayer();

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();

                if (targetPlayer != null)
                    ChangeState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
                Chase();
                break;

            case EnemyState.Attack:
                Attack();
                break;

            case EnemyState.Dead:
                agent.isStopped = true;
                break;
        }
    }

    // ========================
    // PATROL
    // ========================

    void Patrol()
    {
        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0f;

        if (!patrolPointSet)
            SearchPatrolPoint();

        if (patrolPointSet)
            agent.SetDestination(patrolPoint);

        if (Vector3.Distance(transform.position, patrolPoint) < 2f)
            patrolPointSet = false;

        UpdateAnimations(EnemyState.Patrol);
    }

    // ========================
    // CHASE (FIXED OVERSHOOT)
    // ========================

    void Chase()
    {
        if (targetPlayer == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        agent.isStopped = false;
        agent.speed = runSpeed;

        // 🔥 CRITICAL FIX FOR OVERSHOOT
        agent.stoppingDistance = attackRange;

        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        agent.SetDestination(targetPlayer.position);

        if (distance <= attackRange + 0.2f)
            ChangeState(EnemyState.Attack);
        else if (distance > detectionRange)
            ChangeState(EnemyState.Patrol);

        UpdateAnimations(EnemyState.Chase);
    }

    // ========================
    // ATTACK (CLEAN STOP)
    // ========================

    void Attack()
    {
        if (targetPlayer == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        // FAST TURNING
        Vector3 dir = (targetPlayer.position - transform.position);
        dir.y = 0;

        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                rot,
                720f * Time.deltaTime
            );
        }

        if (distance > attackRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        UpdateAnimations(EnemyState.Attack);
    }

    // ========================
    // PATROL POINT
    // ========================

    void SearchPatrolPoint()
    {
        float randomZ = Random.Range(-patrolRadius, patrolRadius);
        float randomX = Random.Range(-patrolRadius, patrolRadius);

        patrolPoint = new Vector3(
            transform.position.x + randomX,
            transform.position.y,
            transform.position.z + randomZ
        );

        if (NavMesh.SamplePosition(patrolPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
            patrolPointSet = true;
        }
    }

    // ========================
    // PLAYER DETECTION
    // ========================

    void FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);

        targetPlayer = null;
        float closestDistance = detectionRange;

        foreach (GameObject player in players)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                targetPlayer = player.transform;
            }
        }
    }

    // ========================
    // STATE
    // ========================

    void ChangeState(EnemyState newState)
    {
        currentState = newState;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            netState.Value = newState;
    }

    // ========================
    // ANIMATION
    // ========================

    void UpdateAnimations(EnemyState state)
    {
        animator.SetBool("isWalking", state == EnemyState.Patrol);
        animator.SetBool("isRunning", state == EnemyState.Chase);
        animator.SetBool("isAttacking", state == EnemyState.Attack);
    }

    // ========================
    // GIZMOS
    // ========================

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (targetPlayer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPlayer.position);
        }
    }
}
