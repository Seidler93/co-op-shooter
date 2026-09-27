using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform top;
    [SerializeField] private RectTransform bottom;
    [SerializeField] private RectTransform left;
    [SerializeField] private RectTransform right;

    [Header("Gap Settings")]
    [SerializeField] private float baseGap = 8f;
    [SerializeField] private float bloomToGap = 45f;
    [SerializeField] private float moveToGap = 22f;
    [SerializeField] private float fireKickGap = 14f;

    [Header("Smoothing")]
    [SerializeField] private float expandSpeed = 18f;
    [SerializeField] private float recoverSpeed = 12f;

    private float bloom01;
    private float move01;
    private float fireKick;
    private float currentGap;

    public static CrosshairController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        // If you want the UI to persist between scene loads, uncomment:
        // DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        currentGap = baseGap;
        ApplyGap(currentGap);
    }

    public void SetBloom01(float value01) => bloom01 = Mathf.Clamp01(value01);
    public void SetMove01(float value01) => move01 = Mathf.Clamp01(value01);

    public void AddFireKick()
    {
        fireKick = Mathf.Clamp01(fireKick + 1f);
    }

    private void Update()
    {
        // Fire kick decay
        fireKick = Mathf.MoveTowards(fireKick, 0f, Time.deltaTime * 8f);

        float targetGap =
            baseGap +
            bloom01 * bloomToGap +
            move01 * moveToGap +
            fireKick * fireKickGap;

        float speed = targetGap > currentGap ? expandSpeed : recoverSpeed;
        currentGap = Mathf.Lerp(currentGap, targetGap, speed * Time.deltaTime);

        ApplyGap(currentGap);
    }

    private void ApplyGap(float gap)
    {
        if (!top || !bottom || !left || !right) return;

        top.anchoredPosition = new Vector2(0f, gap);
        bottom.anchoredPosition = new Vector2(0f, -gap);
        left.anchoredPosition = new Vector2(-gap, 0f);
        right.anchoredPosition = new Vector2(gap, 0f);
    }
}