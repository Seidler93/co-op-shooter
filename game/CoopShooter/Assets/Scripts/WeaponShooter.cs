using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponShooter : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private NetworkObject projectilePrefab;

    [Header("Tuning")]
    [SerializeField] private float projectileSpeed = 35f;
    [SerializeField] private float maxAimDistance = 250f;
    [SerializeField] private float fireCooldown = 0.12f;

    [Header("Aim Raycast")]
    [Tooltip("Layers that can be aimed at. Exclude Player layer to avoid hitting yourself.")]
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Collision")]
    [SerializeField] private bool ignoreShooterCollision = true;

    private PlayerControls input;
    private InputAction fireAction;

    private float nextFireTime;
    private Camera ownerCam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        input = new PlayerControls();
        input.Enable();

        fireAction = input.Gameplay.Fire;

        // Cinemachine drives a virtual camera, but rendering is typically by Main Camera.
        ownerCam = Camera.main;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (input != null)
        {
            input.Disable();
            input.Dispose();
            input = null;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (muzzle == null || projectilePrefab == null) return;

        if (Time.time < nextFireTime) return;

        if (fireAction != null && fireAction.WasPressedThisFrame())
        {
            nextFireTime = Time.time + fireCooldown;

            if (ownerCam == null) ownerCam = Camera.main;
            if (ownerCam == null) return;

            // Ray from screen center
            Ray ray = ownerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Vector3 aimPoint;
            if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
                aimPoint = hit.point;
            else
                aimPoint = ray.origin + ray.direction * maxAimDistance;

            Vector3 dir = (aimPoint - muzzle.position);
            if (dir.sqrMagnitude < 0.0001f)
                dir = muzzle.forward;
            dir.Normalize();

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            Vector3 initialVel = dir * projectileSpeed;

            FireServerRpc(muzzle.position, rot, initialVel);
        }
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 spawnPos, Quaternion spawnRot, Vector3 initialVelocity, ServerRpcParams rpcParams = default)
    {
        spawnPos += spawnRot * Vector3.forward * 0.25f; // ~5x radius
        // Instantiate & init on server
        NetworkObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

        var projectile = proj.GetComponent<NetworkProjectile>();
        if (projectile != null)
        {
            ulong? shooterId = ignoreShooterCollision ? rpcParams.Receive.SenderClientId : (ulong?)null;
            projectile.Initialize(initialVelocity, shooterId);
        }

        proj.Spawn(true);
    }
}