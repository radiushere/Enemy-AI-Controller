using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;

public class BossController : MonoBehaviour
{
    public enum BossState { Chasing, Fleeing, Throwing }

    [Header("Settings")]
    public Transform player;
    public float runRange = 5f;            // When the player is farther than this, the boss chases the player.
    public float throwCooldown = 2f;       // Delay after throwing before resuming movement.
    public float minDistance = 3f;         // When the player is at or within this distance, the boss should attack.
    public GameObject projectilePrefab;
    public Transform throwOrigin;

    [Header("Dynamic Projectile Speed")]
    public float minProjectileSpeed = 8f;  // Projectile speed when the player is near.
    public float maxProjectileSpeed = 15f; // Projectile speed when the player is farther away.

    [Header("Animation")]
    [SerializeField] private float runAnimationSpeed = 0.8f;

    [Header("Flee Settings")]
    public float extraSpeedMargin = 1f;    // If the player is closer than (runRange - extraSpeedMargin), the boss speeds up.
    public float extraSpeedBonus = 0.7f;
    public float stateHysteresis = 0.5f;   // Buffer to prevent rapid state switching.

    [Header("Win Case")]
    public int winThen;
    public SnailWinTrigger snailWinTrigger;
    
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private bool isOver;
    private BossState currentState;
    private float baseSpeed;  // Caches the original speed of the NavMeshAgent.
    private bool isPreparingToThrow = false;
    private int isRunningHash;
    private int isThrowingHash;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        baseSpeed = navMeshAgent.speed;  // Cache the original speed.
        animator.speed = runAnimationSpeed;

        isRunningHash = Animator.StringToHash("isRunning");
        isThrowingHash = Animator.StringToHash("isThrowing");

        if (player == null)
        {
            Debug.LogError("[BossController] Player Transform not set!");
            enabled = false;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        Debug.Log("[BossController] Initial distance to player: " + distance);

        if (distance > runRange + stateHysteresis)
        {
            ChangeState(BossState.Chasing);
            Debug.Log("[BossController] Starting in Chasing state.");
        }
        else if (distance <= runRange && distance > minDistance)
        {
            ChangeState(BossState.Fleeing);
            Debug.Log("[BossController] Starting in Fleeing state.");
        }
        else // distance <= minDistance
        {
            ChangeState(BossState.Throwing);
            Debug.Log("[BossController] Starting in Throwing state.");
            StartCoroutine(PrepareThrow(distance));
        }
    }

    void Update()
    {
        if (isOver)
            return;

        if (player == null)
        {
            Debug.LogWarning("[BossController] Player is null in Update.");
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        // Debug.Log("[BossController] Current distance to player: " + distanceToPlayer);

        if (currentState == BossState.Throwing)
        {
            // Let the coroutine handle behavior.
            return;
        }

        if (distanceToPlayer > runRange + stateHysteresis)
        {
            if (currentState != BossState.Chasing)
            {
                ChangeState(BossState.Chasing);
                Debug.Log("[BossController] Switching to Chasing state.");
            }
            HandleChase();
        }
        else if (distanceToPlayer <= runRange && distanceToPlayer > minDistance + stateHysteresis)
        {
            if (currentState != BossState.Fleeing)
            {
                ChangeState(BossState.Fleeing);
                Debug.Log("[BossController] Switching to Fleeing state.");
            }
            HandleFlee(distanceToPlayer);
        }
        else // Player is very close.
        {
            if (!isPreparingToThrow)
            {
                Debug.Log("[BossController] Player is very close, starting PrepareThrow coroutine.");
                StartCoroutine(PrepareThrow(distanceToPlayer));
            }
            else
            {
                FaceTarget(player.position);
            }
        }
    }

    void HandleChase()
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.speed = baseSpeed;
        navMeshAgent.SetDestination(player.position);
        FaceTarget(player.position);
        // Debug.Log("[BossController] Chasing player.");
    }

    void HandleFlee(float distanceToPlayer)
    {
        navMeshAgent.isStopped = false;
        Vector3 fleeDirection = (transform.position - player.position).normalized;
        Vector3 fleePosition = transform.position + fleeDirection * runRange;
        navMeshAgent.SetDestination(fleePosition);

        if (distanceToPlayer < (runRange - extraSpeedMargin))
        {
            navMeshAgent.speed = baseSpeed + extraSpeedBonus;
            Debug.Log("[BossController] Fleeing fast due to close player.");
        }
        else
        {
            navMeshAgent.speed = baseSpeed;
        }

        FaceAwayFrom(player.position);
        // Debug.Log("[BossController] Fleeing from player.");
    }

    void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, navMeshAgent.angularSpeed * Time.deltaTime);
        }
    }

    void FaceAwayFrom(Vector3 targetPos)
    {
        Vector3 direction = (transform.position - targetPos).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, navMeshAgent.angularSpeed * Time.deltaTime);
        }
    }

    IEnumerator PrepareThrow(float distanceToPlayer)
    {
        if (player == null)
        {
            Debug.LogWarning("[BossController] Player is null in PrepareThrow.");
            yield break;
        }

        isPreparingToThrow = true;
        ChangeState(BossState.Throwing);
        navMeshAgent.isStopped = true;
        animator.SetBool(isRunningHash, false);
        Debug.Log("[BossController] Preparing to throw...");

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        float t = 0f;
        while (t < 1f)
        {
            if (player == null)
            {
                yield break;
            }
            t += Time.deltaTime * 2f;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            yield return null;
        }

        animator.SetBool(isThrowingHash, true);
        Debug.Log("[BossController] Throw animation triggered.");
        yield return new WaitForSeconds(0.3f);

        float lerpFactor = Mathf.InverseLerp(minDistance, runRange, distanceToPlayer);
        float dynamicSpeed = Mathf.Lerp(minProjectileSpeed, maxProjectileSpeed, lerpFactor);
        Debug.Log("[BossController] Calculated dynamicSpeed: " + dynamicSpeed);

        if (projectilePrefab && throwOrigin)
        {
            GameObject projectile = Instantiate(projectilePrefab, throwOrigin.position, Quaternion.identity);
            Debug.Log("[BossController] Projectile instantiated.");
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 throwDirection = (player.position - throwOrigin.position).normalized;
                rb.velocity = throwDirection * dynamicSpeed;
                Debug.Log("[BossController] Projectile velocity set.");
            }
        }
        else
        {
            Debug.LogWarning("[BossController] projectilePrefab or throwOrigin not set!");
        }

        yield return new WaitForSeconds(throwCooldown);
        animator.SetBool(isThrowingHash, false);

        float updatedDistance = Vector3.Distance(transform.position, player.position);
        Debug.Log("[BossController] Updated distance to player: " + updatedDistance);

        if (updatedDistance <= minDistance + stateHysteresis)
        {
            Debug.Log("[BossController] Player still very close. Repeating throw.");
            StartCoroutine(PrepareThrow(updatedDistance));
        }
        else if (updatedDistance > runRange + stateHysteresis)
        {
            ChangeState(BossState.Chasing);
            Debug.Log("[BossController] Switching to Chasing state post-throw.");
        }
        else if (updatedDistance <= runRange && updatedDistance > minDistance + stateHysteresis)
        {
            ChangeState(BossState.Fleeing);
            Debug.Log("[BossController] Switching to Fleeing state post-throw.");
        }
        else
        {
            ChangeState(BossState.Chasing);
            Debug.Log("[BossController] Defaulting to Chasing state post-throw.");
        }

        navMeshAgent.isStopped = false;
        isPreparingToThrow = false;
    }

    void ChangeState(BossState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case BossState.Chasing:
                animator.SetBool(isRunningHash, true);
                navMeshAgent.isStopped = false;
                break;
            case BossState.Fleeing:
                animator.SetBool(isRunningHash, true);
                navMeshAgent.isStopped = false;
                break;
            case BossState.Throwing:
                animator.SetBool(isRunningHash, false);
                navMeshAgent.isStopped = true;
                break;
        }
        Debug.Log("[BossController] Changed state to " + newState.ToString());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[BossController] OnTriggerEnter with Player.");
            snailWinTrigger.totalEnemies--;
            Debug.Log("[BossController] Updated enemy counter: " + snailWinTrigger.totalEnemies);
            gameObject.SetActive(false);
        }
    }
}