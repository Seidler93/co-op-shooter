using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("Owner-Only Components")]
    [SerializeField] private MonoBehaviour[] ownerOnlyBehaviours;
    [SerializeField] private GameObject[] ownerOnlyObjects;

    public override void OnNetworkSpawn()
    {
        ApplyOwnershipState();
    }

    public override void OnGainedOwnership()
    {
        ApplyOwnershipState();
    }

    public override void OnLostOwnership()
    {
        ApplyOwnershipState();
    }

    private void ApplyOwnershipState()
    {
        bool owner = IsOwner;

        if (ownerOnlyBehaviours != null)
        {
            for (int i = 0; i < ownerOnlyBehaviours.Length; i++)
            {
                if (ownerOnlyBehaviours[i] != null)
                    ownerOnlyBehaviours[i].enabled = owner;
            }
        }

        if (ownerOnlyObjects != null)
        {
            for (int i = 0; i < ownerOnlyObjects.Length; i++)
            {
                if (ownerOnlyObjects[i] != null)
                    ownerOnlyObjects[i].SetActive(owner);
            }
        }
    }
}