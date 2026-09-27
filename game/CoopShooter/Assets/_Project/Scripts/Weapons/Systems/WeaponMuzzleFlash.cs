using System.Collections;
using UnityEngine;

public class WeaponMuzzleFlash : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject flashRoot;

    [Header("Optional")]
    [SerializeField] private Light flashLight;
    [SerializeField] private float lightDuration = 0.03f;

    private ParticleSystem[] particleSystems;
    private MeshRenderer[] meshRenderers;
    private Coroutine lightRoutine;
    private Coroutine meshRoutine;

    private void Awake()
    {
        if (flashRoot == null)
            flashRoot = gameObject;

        particleSystems = flashRoot.GetComponentsInChildren<ParticleSystem>(true);
        meshRenderers = flashRoot.GetComponentsInChildren<MeshRenderer>(true);

        // Start with meshes hidden so you don't see the flash at spawn
        SetMeshRenderers(false);

        if (flashLight != null)
            flashLight.enabled = false;
    }

    public void PlayFlash()
    {
        // Restart all particle systems cleanly
        foreach (var ps in particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        // Show mesh-based flash briefly
        if (meshRoutine != null)
            StopCoroutine(meshRoutine);
        meshRoutine = StartCoroutine(ShowMeshesBriefly());

        // Flash the light briefly
        if (flashLight != null)
        {
            if (lightRoutine != null)
                StopCoroutine(lightRoutine);
            lightRoutine = StartCoroutine(FlashLightRoutine());
        }
    }

    private IEnumerator ShowMeshesBriefly()
    {
        SetMeshRenderers(true);
        yield return new WaitForSeconds(0.03f);
        SetMeshRenderers(false);
    }

    private IEnumerator FlashLightRoutine()
    {
        flashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        flashLight.enabled = false;
    }

    private void SetMeshRenderers(bool enabled)
    {
        foreach (var mr in meshRenderers)
            mr.enabled = enabled;
    }
}