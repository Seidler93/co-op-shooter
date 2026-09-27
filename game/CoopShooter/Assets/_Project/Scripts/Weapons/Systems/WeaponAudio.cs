using Unity.Netcode;
using UnityEngine;

public class WeaponAudio : NetworkBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private float gunshotVolume = 1f;
    [SerializeField] private float gunshotPitchMin = 0.95f;
    [SerializeField] private float gunshotPitchMax = 1.05f;

    public void PlayLocalGunshot(Vector3 position)
    {
        if (!gunshotClip) return;
        AudioSource.PlayClipAtPoint(gunshotClip, position, gunshotVolume);
    }

    [ClientRpc]
    public void PlayGunshotClientRpc(Vector3 position, ClientRpcParams rpcParams = default)
    {
        if (!gunshotClip) return;

        GameObject temp = new GameObject("GunshotAudio");
        temp.transform.position = position;

        var audio = temp.AddComponent<AudioSource>();
        audio.clip = gunshotClip;
        audio.spatialBlend = 1f;
        audio.volume = gunshotVolume;
        audio.pitch = Random.Range(gunshotPitchMin, gunshotPitchMax);
        audio.rolloffMode = AudioRolloffMode.Logarithmic;
        audio.minDistance = 5f;
        audio.maxDistance = 60f;
        audio.Play();


        Destroy(temp, gunshotClip.length + 0.1f);
    }
}