using TMPro;
using UnityEngine;

public class AmmoHUDNetcode : MonoBehaviour
{
    [SerializeField] private WeaponAmmoNetcode ammo;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text reloadText;

    private void OnEnable()
    {
        TrySubscribe();
        RefreshUI();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Bind(WeaponAmmoNetcode newAmmo)
    {
        if (ammo == newAmmo) return;

        Unsubscribe();
        ammo = newAmmo;
        TrySubscribe();
        RefreshUI();
    }

    private void TrySubscribe()
    {
        if (ammo == null) return;

        ammo.MagAmmo.OnValueChanged += OnMagAmmoChanged;
        ammo.ReserveAmmo.OnValueChanged += OnReserveAmmoChanged;
        ammo.IsReloading.OnValueChanged += OnReloadChanged;
    }

    private void Unsubscribe()
    {
        if (ammo == null) return;

        ammo.MagAmmo.OnValueChanged -= OnMagAmmoChanged;
        ammo.ReserveAmmo.OnValueChanged -= OnReserveAmmoChanged;
        ammo.IsReloading.OnValueChanged -= OnReloadChanged;
    }

    private void OnMagAmmoChanged(int oldValue, int newValue)
    {
        RefreshAmmoText();
    }

    private void OnReserveAmmoChanged(int oldValue, int newValue)
    {
        RefreshAmmoText();
    }

    private void OnReloadChanged(bool oldValue, bool newValue)
    {
        RefreshReloadText();
    }

    private void RefreshUI()
    {
        RefreshAmmoText();
        RefreshReloadText();
    }

    private void RefreshAmmoText()
    {
        if (!ammoText) return;

        if (ammo == null)
        {
            ammoText.text = "--/--";
            return;
        }

        ammoText.text = $"{ammo.MagAmmo.Value}/{ammo.ReserveAmmo.Value}";
    }

    private void RefreshReloadText()
    {
        if (!reloadText) return;

        bool show = ammo != null && ammo.IsReloading.Value;
        reloadText.gameObject.SetActive(show);
    }
}