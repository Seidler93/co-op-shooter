using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class GroundSnapOnSpawn : NetworkBehaviour
{
    [Header("Ground")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayStartHeight = 3.0f;
    [SerializeField] private float rayDistance = 10.0f;

    [Tooltip("Extra lift so we don't start intersecting ground due to skin width / precision.")]
    [SerializeField] private float extraLift = 0.02f;

    private NavMeshAgent agent;
    private CharacterController cc;
    private CapsuleCollider capsule;
    private Collider anyCollider;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        cc = GetComponent<CharacterController>();
        capsule = GetComponent<CapsuleCollider>();
        anyCollider = GetComponent<Collider>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        SnapToGroundServer();
    }

    private void SnapToGroundServer()
    {
        // Disable agent/controller while moving transform (prevents weird corrections)
        bool agentWasEnabled = agent != null && agent.enabled;
        bool ccWasEnabled = cc != null && cc.enabled;

        if (agentWasEnabled) agent.enabled = false;
        if (ccWasEnabled) cc.enabled = false;

        float bottomOffset = ComputeBottomOffsetLocal();

        Vector3 origin = transform.position + Vector3.up * rayStartHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;

            // We want (position.y - bottomOffset) == hit.point.y
            // => position.y = hit.point.y + bottomOffset + extraLift
            p.y = hit.point.y + bottomOffset + extraLift;
            transform.position = p;
        }
        else
        {
            Debug.LogWarning($"[GroundSnapOnSpawn] No ground hit for {name}. Check groundMask / spawn height.");
        }

        // Re-enable
        if (ccWasEnabled) cc.enabled = true;

        if (agentWasEnabled)
        {
            agent.enabled = true;

            // If using navmesh, warp to sync navmesh internal position
            if (agent.isOnNavMesh)
            {
                agent.Warp(transform.position);
            }
        }
    }

    /// <summary>
    /// Returns the local-space distance from transform.position down to the bottom of the character volume.
    /// </summary>
    private float ComputeBottomOffsetLocal()
    {
        // Priority: CharacterController -> CapsuleCollider -> any collider bounds fallback
        if (cc != null)
        {
            // Bottom world = pos.y + cc.center.y - cc.height/2
            // bottomOffset = cc.height/2 - cc.center.y
            return (cc.height * 0.5f) - cc.center.y + cc.skinWidth;
        }

        if (capsule != null)
        {
            // Bottom world = pos.y + capsule.center.y - capsule.height/2
            return (capsule.height * 0.5f) - capsule.center.y;
        }

        if (anyCollider != null)
        {
            // Bounds are world-space; convert to an offset relative to transform.position.
            // Approx: bottomOffset = position.y - bounds.min.y
            // (assumes transform.position is roughly center/pivot)
            float worldBottomOffset = transform.position.y - anyCollider.bounds.min.y;
            return worldBottomOffset;
        }

        // Fallback: assume pivot at feet
        return 0f;
    }
}