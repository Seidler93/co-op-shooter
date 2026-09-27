using UnityEngine;

[RequireComponent(typeof(Health))]
public class HealthAudioFeedback : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private AudioSource audioSource;

    [Header("Hit Audio")]
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private float hitVolume = 1f;
    [SerializeField] private float hitPitchMin = 0.96f;
    [SerializeField] private float hitPitchMax = 1.04f;
    [SerializeField] private float minHitInterval = 0.05f;

    [Header("Death Audio")]
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField] private float deathVolume = 1f;
    [SerializeField] private float deathPitchMin = 0.96f;
    [SerializeField] private float deathPitchMax = 1.04f;

    private float lastHitTime = -999f;
    private int lastHitClipIndex = -1;
    private int lastDeathClipIndex = -1;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 24f;
    }

    private void OnEnable()
    {
        if (health == null)
            return;

        health.HealthValueChanged += HandleHealthValueChanged;
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.HealthValueChanged -= HandleHealthValueChanged;
        health.Died -= HandleDied;
    }

    private void HandleHealthValueChanged(int previous, int current)
    {
        if (health == null)
            return;

        if (current >= previous)
            return;

        if (!health.IsAlive)
            return;

        if (Time.time - lastHitTime < minHitInterval)
            return;

        PlayRandomClip(hitClips, hitVolume, hitPitchMin, hitPitchMax, ref lastHitClipIndex);
        lastHitTime = Time.time;
    }

    private void HandleDied(Health deadHealth)
    {
        PlayRandomClip(deathClips, deathVolume, deathPitchMin, deathPitchMax, ref lastDeathClipIndex);
    }

    private void PlayRandomClip(AudioClip[] clips, float volume, float pitchMin, float pitchMax, ref int lastClipIndex)
    {
        if (clips == null || clips.Length == 0 || audioSource == null)
            return;

        int clipIndex = ChooseClipIndex(clips, lastClipIndex);
        AudioClip clip = clips[clipIndex];
        if (clip == null)
            return;

        lastClipIndex = clipIndex;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(clip, volume);
    }

    private int ChooseClipIndex(AudioClip[] clips, int previousIndex)
    {
        if (clips.Length <= 1)
            return 0;

        int nextIndex = Random.Range(0, clips.Length);
        if (nextIndex == previousIndex)
            nextIndex = (nextIndex + 1) % clips.Length;

        return nextIndex;
    }
}
