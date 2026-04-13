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
        currentShopper = shopper;
        currentPlayerController = shopper != null ? shopper.GetComponent<PlayerController>() : null;

        if (shopPanel != null)
            shopPanel.SetActive(true);

        RefreshPoints();
        ShowMessage("", true);

        currentPlayerController?.SetGameplayInputBlocked(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        currentPlayerController?.SetGameplayInputBlocked(false);
        currentShopper = null;
        currentPlayerController = null;

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

        if (pointsText == null)
            return;

        if (currentShopper == null)
        {
            pointsText.text = "Points: --";
            return;
        }

        NetworkPlayer player = currentShopper.GetComponent<NetworkPlayer>();
        if (player == null)
        {
            pointsText.text = "Points: --";
            return;
        }

        pointsText.text = $"Points: {player.Score.Value}";
    }

    private void UpdateLabels()
    {
        int damageCost = currentShopper != null
            ? currentShopper.GetCurrentCost(ShopItemType.DamageUpgrade)
            : ShopCatalog.GetCost(ShopItemType.DamageUpgrade);
        int fireRateCost = currentShopper != null
            ? currentShopper.GetCurrentCost(ShopItemType.FireRateUpgrade)
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
        currentShopper.BuyItemServerRpc(itemType);
    }
}
