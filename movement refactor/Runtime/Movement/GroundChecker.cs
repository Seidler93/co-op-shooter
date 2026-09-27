using UnityEngine;

namespace CoopShooter.MovementRefactor
{
    public sealed class GroundChecker : MonoBehaviour
    {
        [SerializeField] private Transform groundProbe;
        [SerializeField] private float probeRadius = 0.25f;
        [SerializeField] private float probeDistance = 0.15f;
        [SerializeField] private LayerMask groundMask = ~0;

        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;

        private void Awake()
        {
            if (!groundProbe)
                groundProbe = transform;
        }

        public bool CheckGround()
        {
            Vector3 origin = groundProbe.position + Vector3.up * 0.05f;
            float distance = probeDistance + 0.05f;

            if (Physics.SphereCast(origin, probeRadius, Vector3.down, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
            {
                IsGrounded = true;
                GroundNormal = hit.normal;
                return true;
            }

            IsGrounded = false;
            GroundNormal = Vector3.up;
            return false;
        }
    }
}
