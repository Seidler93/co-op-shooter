using UnityEngine;

public class WeaponCrosshairDriver : MonoBehaviour
{
    [Header("Crosshair")]
    [SerializeField] private CrosshairController crosshair;
    [SerializeField] private float moveSpeedForMax = 6f;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private WeaponBloom weaponBloom;

    private void Awake()
    {
        if (!playerController)
            playerController = GetComponentInParent<PlayerController>();

        if (!weaponBloom)
            weaponBloom = GetComponent<WeaponBloom>();
    }

    public void TickCrosshair()
    {
        TryBindCrosshair();

        if (crosshair == null || weaponBloom == null || playerController == null)
            return;

        float bloom01 = weaponBloom.GetBloom01();

        float planar = playerController.PlanarSpeed;
        float move01 = Mathf.Clamp01(planar / Mathf.Max(0.01f, moveSpeedForMax));

        crosshair.SetBloom01(bloom01);
        crosshair.SetMove01(move01);
    }

    public void AddFireKick()
    {
        TryBindCrosshair();
        if (crosshair != null)
            crosshair.AddFireKick();
    }

    private void TryBindCrosshair()
    {
        if (crosshair != null) return;
        crosshair = CrosshairController.Instance;
    }
}