using UnityEngine;

public class ShopChest : MonoBehaviour
{
    [SerializeField] private string promptText = "Press Interact to open shop";

    private PlayerShopper localShopperInRange;
    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        if (localShopperInRange == null) return;

        if (controls.Gameplay.Interact.triggered)
        {
            if (ShopUI.Instance != null)
            {
                ShopUI.Instance.Open(localShopperInRange);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerShopper shopper = other.GetComponentInParent<PlayerShopper>();
        if (shopper == null || !shopper.IsOwner) return;

        localShopperInRange = shopper;

        if (ShopUI.Instance != null)
            ShopUI.Instance.ShowPrompt(promptText);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerShopper shopper = other.GetComponentInParent<PlayerShopper>();
        if (shopper == null || shopper != localShopperInRange) return;

        localShopperInRange = null;

        if (ShopUI.Instance != null)
        {
            ShopUI.Instance.HidePrompt();
            ShopUI.Instance.Close();
        }
    }
}