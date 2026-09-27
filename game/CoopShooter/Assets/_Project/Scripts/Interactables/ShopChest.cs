using UnityEngine;

public class ShopChest : MonoBehaviour
{
    [SerializeField] private string promptText = "Press Interact to open shop";

    private PlayerShopper localShopperInRange;
    private PlayerInputReader localInputReader;

    private void Update()
    {
        if (localShopperInRange == null || localInputReader == null) return;

        if (localInputReader.InteractPressedThisFrame)
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
        localInputReader = shopper.GetComponent<PlayerInputReader>();

        if (ShopUI.Instance != null)
            ShopUI.Instance.ShowPrompt(promptText);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerShopper shopper = other.GetComponentInParent<PlayerShopper>();
        if (shopper == null || shopper != localShopperInRange) return;

        localShopperInRange = null;
        localInputReader = null;

        if (ShopUI.Instance != null)
        {
            ShopUI.Instance.HidePrompt();
            ShopUI.Instance.Close();
        }
    }
}
