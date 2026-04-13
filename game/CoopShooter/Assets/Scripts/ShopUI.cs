using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text messageText;

    [Header("Buttons")]
    [SerializeField] private Button ammoButton;
    [SerializeField] private Button healthButton;
    [SerializeField] private Button damageButton;
    [SerializeField] private Button fireRateButton;
    [SerializeField] private Button closeButton;

    [Header("Labels")]
    [SerializeField] private TMP_Text ammoLabel;
    [SerializeField] private TMP_Text healthLabel;
    [SerializeField] private TMP_Text damageLabel;
    [SerializeField] private TMP_Text fireRateLabel;

    private PlayerShopper currentShopper;
    private PlayerController currentPlayerController;
    private NetworkPlayer currentNetworkPlayer;
    private int? authoritativePointsOverride;
    private int? authoritativeDamageLevelOverride;
    private int? authoritativeFireRateLevelOverride;
    private bool purchasePending;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        ammoButton.onClick.AddListener(() => Buy(ShopItemType.Ammo));
        healthButton.onClick.AddListener(() => Buy(ShopItemType.Health));
        damageButton.onClick.AddListener(() => Buy(ShopItemType.DamageUpgrade));
        fireRateButton.onClick.AddListener(() => Buy(ShopItemType.FireRateUpgrade));
        closeButton.onClick.AddListener(Close);

        UpdateLabels();
    }

    public void Open(PlayerShopper shopper)
    {
        UnbindCurrentPlayer();

        currentShopper = shopper;
        currentPlayerController = shopper != null ? shopper.GetComponent<PlayerController>() : null;
        currentNetworkPlayer = shopper != null ? shopper.GetComponent<NetworkPlayer>() : null;
        authoritativePointsOverride = null;
        authoritativeDamageLevelOverride = null;
        authoritativeFireRateLevelOverride = null;
        purchasePending = false;

        if (currentNetworkPlayer != null)
            currentNetworkPlayer.Score.OnValueChanged += OnScoreChanged;

        if (currentShopper != null)
        {
            currentShopper.DamageUpgradeLevel.OnValueChanged += OnDamageUpgradeLevelChanged;
            currentShopper.FireRateUpgradeLevel.OnValueChanged += OnFireRateUpgradeLevelChanged;
        }

        if (shopPanel != null)
            shopPanel.SetActive(true);

        UpdateButtonInteractable();
        RefreshPoints();
        ShowMessage("", true);

        currentPlayerController?.SetGameplayInputBlocked(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        currentPlayerController?.SetGameplayInputBlocked(false);
        UnbindCurrentPlayer();
        currentShopper = null;
        currentPlayerController = null;
        currentNetworkPlayer = null;
        authoritativePointsOverride = null;
        authoritativeDamageLevelOverride = null;
        authoritativeFireRateLevelOverride = null;
        purchasePending = false;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowPrompt(string text)
    {
        if (promptText == null) return;
        promptText.gameObject.SetActive(true);
        promptText.text = text;
    }

    public void HidePrompt()
    {
        if (promptText == null) return;
        promptText.gameObject.SetActive(false);
    }

    public void ShowMessage(string msg, bool success)
    {
        if (messageText == null) return;
        messageText.text = msg;
    }

    public void RefreshPoints()
    {
        UpdateLabels();
        UpdateButtonInteractable();

        if (pointsText == null)
            return;

        if (currentNetworkPlayer == null)
        {
            pointsText.text = "Points: --";
            return;
        }

        int points = authoritativePointsOverride ?? currentNetworkPlayer.Score.Value;
        pointsText.text = $"Points: {points}";
    }

    private void UpdateLabels()
    {
        int damageLevel = currentShopper != null
            ? (authoritativeDamageLevelOverride ?? currentShopper.CurrentDamageUpgradeLevel)
            : 0;
        int fireRateLevel = currentShopper != null
            ? (authoritativeFireRateLevelOverride ?? currentShopper.CurrentFireRateUpgradeLevel)
            : 0;

        int damageCost = currentShopper != null
            ? ShopCatalog.GetCost(ShopItemType.DamageUpgrade, damageLevel)
            : ShopCatalog.GetCost(ShopItemType.DamageUpgrade);
        int fireRateCost = currentShopper != null
            ? ShopCatalog.GetCost(ShopItemType.FireRateUpgrade, fireRateLevel)
            : ShopCatalog.GetCost(ShopItemType.FireRateUpgrade);

        if (ammoLabel != null)
            ammoLabel.text = $"Ammo ({ShopCatalog.GetCost(ShopItemType.Ammo)})";

        if (healthLabel != null)
            healthLabel.text = $"Health ({ShopCatalog.GetCost(ShopItemType.Health)})";

        if (damageLabel != null)
            damageLabel.text = $"Damage Upgrade ({damageCost})";

        if (fireRateLabel != null)
            fireRateLabel.text = $"Fire Rate Upgrade ({fireRateCost})";
    }

    private void Buy(ShopItemType itemType)
    {
        if (currentShopper == null) return;
        if (purchasePending) return;

        purchasePending = true;
        UpdateButtonInteractable();
        currentShopper.BuyItemServerRpc(itemType);
    }

    public void HandlePurchaseResult(bool success, string message, int authoritativeScore, int damageLevel, int fireRateLevel)
    {
        authoritativePointsOverride = authoritativeScore;
        authoritativeDamageLevelOverride = damageLevel;
        authoritativeFireRateLevelOverride = fireRateLevel;
        purchasePending = false;

        ShowMessage(message, success);
        RefreshPoints();
    }

    private void OnScoreChanged(int previousValue, int newValue)
    {
        authoritativePointsOverride = null;
        RefreshPoints();
    }

    private void OnDamageUpgradeLevelChanged(int previousValue, int newValue)
    {
        authoritativeDamageLevelOverride = null;
        UpdateLabels();
        UpdateButtonInteractable();
    }

    private void OnFireRateUpgradeLevelChanged(int previousValue, int newValue)
    {
        authoritativeFireRateLevelOverride = null;
        UpdateLabels();
        UpdateButtonInteractable();
    }

    private void UpdateButtonInteractable()
    {
        bool canInteract = currentShopper != null && !purchasePending;

        if (ammoButton != null) ammoButton.interactable = canInteract;
        if (healthButton != null) healthButton.interactable = canInteract;
        if (damageButton != null) damageButton.interactable = canInteract;
        if (fireRateButton != null) fireRateButton.interactable = canInteract;
    }

    private void UnbindCurrentPlayer()
    {
        if (currentNetworkPlayer != null)
            currentNetworkPlayer.Score.OnValueChanged -= OnScoreChanged;

        if (currentShopper != null)
        {
            currentShopper.DamageUpgradeLevel.OnValueChanged -= OnDamageUpgradeLevelChanged;
            currentShopper.FireRateUpgradeLevel.OnValueChanged -= OnFireRateUpgradeLevelChanged;
        }
    }

    private void OnDestroy()
    {
        UnbindCurrentPlayer();
    }
}
