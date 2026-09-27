using Unity.Netcode;
using UnityEngine;

public class NetworkWeaponAim : NetworkBehaviour
{
    [SerializeField] private float aimSyncThreshold = 0.05f;

    public readonly NetworkVariable<Vector2> WeaponAimAngles =
        new NetworkVariable<Vector2>(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    public readonly NetworkVariable<int> RecoilSeq =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    public readonly NetworkVariable<Vector2> RecoilKick =
        new NetworkVariable<Vector2>(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    public void OwnerSetAimAngles(Vector2 angles)
    {
        if (!IsOwner)
            return;

        if (Vector2.SqrMagnitude(WeaponAimAngles.Value - angles) < (aimSyncThreshold * aimSyncThreshold))
            return;

        WeaponAimAngles.Value = angles;
    }

    public void OwnerTriggerRecoil(Vector2 kick)
    {
        if (!IsOwner)
            return;

        RecoilKick.Value = kick;
        RecoilSeq.Value += 1;
    }
}
