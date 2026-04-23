using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;


public class FatZombie : NetworkBehaviour
{
 public enum DashState
    {
        Idle,
        WindUp,
        Dashing,
        Stagger
    }

    [Header("Ranges")]
    public float detectionRange = 15f;
    public float attackRange = 3f;
    public float dashRange = 6f;

    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float dashSpeed = 8f;
    public float dashDuration = 0.5f;
    public float dashCooldown = 3f;

    [Header("Damage")]
    public float dashDamage = 25f;

    [Header("NavMesh / Nav Fixes")]
    public float stoppingDistanceBuffer = 0.3f;

    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;

    [Header("Debug")]
    public Color gizmoDashColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    private Transform targetPlayer;
    private string playerTag = "Player";

    private bool isGrounded;
    private float lastDashTime;

    private DashState currentDashState = DashState.Idle;
    private string gizmoState = "Idle";

    // Check if we’re using Netcode at all
#if UNITY_NETCODE_PRESENT
    private bool IsNetcodeEnabled => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
#endif

    void OnEnable()
    {
#if UNITY_NETCODE_PRESENT
        // If Netcode is present, use it as before
        if (IsNetcodeEnabled)
        {
            // No special NetworkObject setup here; just run logic on server or local host
            if (NetworkManager.Singleton.IsServer)
            {
                SetupAgent();
            }
        }
        else
#endif
        {
            // Running in offline mode
            SetupAgent();
        }
    }

    void SetupAgent()
    {
        if (agent == null) return;

        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.stoppingDistance = attackRange - stoppingDistanceBuffer;
        agent.autoBraking = true;
        agent.acceleration = 30f;
        agent.angularSpeed = 720f;
        agent.speed = walkSpeed;
    }

    void Update()
    {
        // Run AI only if agent exists
        if (agent == null) return;

#if UNITY_NETCODE_PRESENT
        // If Netcode is enabled, only run AI on server / host
        if (IsNetcodeEnabled && !NetworkManager.Singleton.IsServer) return;
#endif

        FindClosestPlayer();
        CheckGrounded();

        // ---- State Machine ----
        switch (currentDashState)
        {
            case DashState.Idle:
                PatrolOrChase();
                break;

            case DashState.WindUp:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;

                if (Time.time - lastDashTime > dashCooldown)
                {
                    currentDashState = DashState.Dashing;
                }
                break;

            case DashState.Dashing:
                DoDash();
                break;

            case DashState.Stagger:
                agent.speed = walkSpeed;
                agent.isStopped = false;
                if (Time.time - lastDashTime > dashCooldown + 0.3f)
                {
                    currentDashState = DashState.Idle;
                }
                break;
        }

        UpdateAnimations();
    }

    // -------- Movement --------

    void PatrolOrChase()
    {
        if (targetPlayer == null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, targetPlayer.position);
        agent.isStopped = false;
        agent.speed = walkSpeed;

        // Start dash setup
        if (distToTarget <= attackRange && Time.time - lastDashTime > dashCooldown)
        {
            currentDashState = DashState.WindUp;
            lastDashTime = Time.time;
        }
    }

    void DoDash()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        Vector3 dir = (targetPlayer.position - transform.position).normalized;
        Vector3 dashTarget = transform.position + dir * dashRange;

        Vector3 moveDir = (dashTarget - transform.position).normalized;
        Vector3 nextPos = Vector3.MoveTowards(transform.position, dashTarget, dashSpeed * Time.deltaTime);
        agent.nextPosition = nextPos;

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        if (dist <= 2f)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 2f, LayerMask.GetMask("Player"));
            foreach (Collider col in hits)
            {
                if (col.CompareTag("Player"))
                {
                    col.GetComponent<PlayerHealth>()?.TakeDamage(dashDamage);
                }
            }
        }

        if (Time.time - lastDashTime >= dashDuration)
        {
            currentDashState = DashState.Stagger;
        }
    }

    // -------- Player / Ground --------

    void FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);

        targetPlayer = null;
        float closestDist = detectionRange;

        foreach (GameObject player in players)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                targetPlayer = player.transform;
            }
        }
    }

    void CheckGrounded()
    {
        isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.3f,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer
        );
    }

    // -------- Animations --------

    void UpdateAnimations()
    {
        gizmoState = currentDashState.ToString();

        bool isWalking = currentDashState == DashState.Idle;
        bool isRunning = currentDashState == DashState.WindUp;
        bool isAttacking = currentDashState == DashState.Dashing;
        bool isStaggered = currentDashState == DashState.Stagger;

        if (animator != null)
        {
            animator.SetBool("isWalking", isWalking);
            animator.SetBool("isRunning", isRunning);
            animator.SetBool("isAttacking", isAttacking);
            animator.SetBool("isStaggered", isStaggered);
            animator.SetBool("isFalling", !isGrounded);
        }
    }

    // -------- No knockback --------

    public void ApplyKnockback(Vector3 force)
    {
        // Fat zombie ignores knockback, in offline and online mode
    }

    // -------- Debug Gizmos --------

    private void OnDrawGizmos()
    {
        if (agent == null) return;

        Color oldColor = Gizmos.color;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = gizmoDashColor;
        Gizmos.DrawWireSphere(transform.position, dashRange);

        if (targetPlayer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPlayer.position);
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(
            transform.position + Vector3.up * 0.3f,
            Vector3.down * (groundCheckDistance + 0.1f)
        );

        if (currentDashState == DashState.Dashing)
        {
            Vector3 dashTarget = transform.position +
                (targetPlayer.position - transform.position).normalized * dashRange;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, dashTarget);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.4f, gizmoState);
#endif

        Gizmos.color = oldColor;
    }
}
