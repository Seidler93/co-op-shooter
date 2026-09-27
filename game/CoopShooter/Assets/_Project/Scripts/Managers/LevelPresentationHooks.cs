using UnityEngine;

public class LevelPresentationHooks : MonoBehaviour
{
    public static LevelPresentationHooks Instance { get; private set; }

    [Header("Roots")]
    [SerializeField] private Transform worldVfxRoot;
    [SerializeField] private Transform ambientVfxRoot;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip levelMusic;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool loopMusic = true;
    [SerializeField] private float musicVolume = 0.7f;

    public Transform WorldVfxRoot => worldVfxRoot;
    public Transform AmbientVfxRoot => ambientVfxRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (playMusicOnStart)
            PlayLevelMusic();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayLevelMusic()
    {
        if (musicSource == null || levelMusic == null)
            return;

        musicSource.clip = levelMusic;
        musicSource.loop = loopMusic;
        musicSource.volume = musicVolume;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void StopLevelMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
    }
}
