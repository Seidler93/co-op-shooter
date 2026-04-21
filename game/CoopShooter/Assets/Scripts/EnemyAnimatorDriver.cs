using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class EnemyAnimatorDriver : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkAnimator networkAnimator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Health health;

    [Header("Tuning")]
    [SerializeField] private float moveNormalizationSpeed = 3.5f;
    [SerializeField] private float moveDampTime = 0.08f;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private Vector3 lastPosition;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!networkAnimator) networkAnimator = GetComponent<NetworkAnimator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!health) health = GetComponent<Health>();
        lastPosition = transform.position;
    }

    public override void OnNetworkSpawn()
    {
        lastPosition = transform.position;

        if (health != null)
        {
            health.Died -= OnDied;
            health.Died += OnDied;
        }

        if (IsServer && animator != null)
        {
            animator.SetBool(DeadHash, health != null && !health.IsAlive);
            animator.SetFloat(MoveSpeedHash, 0f);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (health != null)
            health.Died -= OnDied;
    }

    private void Update()
    {
        if (!IsServer || animator == null)
            return;

        if (health != null && !health.IsAlive)
        {
            animator.SetFloat(MoveSpeedHash, 0f);
            return;
        }

        float worldSpeed = 0f;

        if (agent != null && agent.enabled)
        {
            worldSpeed = agent.velocity.magnitude;
        }
        else
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            worldSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        }

        lastPosition = transform.position;

        float normalized = moveNormalizationSpeed > 0.001f
            ? Mathf.Clamp01(worldSpeed / moveNormalizationSpeed)
            : 0f;

        animator.SetFloat(MoveSpeedHash, normalized, moveDampTime, Time.deltaTime);
    }

    public void TriggerAttack()
    {
        if (!IsServer || animator == null)
            return;

        animator.SetTrigger(AttackHash);
    }

    private void OnDied(Health _)
    {
        if (!IsServer || animator == null)
            return;

        animator.SetBool(DeadHash, true);
        animator.SetFloat(MoveSpeedHash, 0f);
    }
}
