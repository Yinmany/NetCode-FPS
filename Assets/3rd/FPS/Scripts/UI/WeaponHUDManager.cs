using System.Collections.Generic;
using UnityEngine;

public class WeaponHUDManager : MonoBehaviour
{
    [Tooltip("UI panel containing the layoutGroup for displaying weapon ammos")]
    public RectTransform ammosPanel;
    [Tooltip("Prefab for displaying weapon ammo")]
    public GameObject ammoCounterPrefab;

    PlayerWeaponsManager m_PlayerWeaponsManager;
    List<AmmoCounter> m_AmmoCounters = new List<AmmoCounter>();

    void Start()
    {
        m_PlayerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, WeaponHUDManager>(m_PlayerWeaponsManager, this);

        WeaponController activeWeapon = m_PlayerWeaponsManager.GetActiveWeapon();
        if (activeWeapon)
        {
            AddWeapon(activeWeapon, m_PlayerWeaponsManager.activeWeaponIndex);
            ChangeWeapon(activeWeapon);
        }

        m_PlayerWeaponsManager.onAddedWeapon += AddWeapon;
        m_PlayerWeaponsManager.onRemovedWeapon += RemoveWeapon;
        m_PlayerWeaponsManager.onSwitchedToWeapon += ChangeWeapon;
    }

    void AddWeapon(WeaponController newWeapon, int weaponIndex)
    {
        GameObject ammoCounterInstance = Instantiate(ammoCounterPrefab, ammosPanel);
        AmmoCounter newAmmoCounter = ammoCounterInstance.GetComponent<AmmoCounter>();
        DebugUtility.HandleErrorIfNullGetComponent<AmmoCounter, WeaponHUDManager>(newAmmoCounter, this, ammoCounterInstance.gameObject);

        newAmmoCounter.Initialize(newWeapon, weaponIndex);

        m_AmmoCounters.Add(newAmmoCounter);
    }

    void RemoveWeapon(WeaponController newWeapon, int weaponIndex)
    {
        int foundCounterIndex = -1;
        for (int i = 0; i < m_AmmoCounters.Count; i++)
        {
            if(m_AmmoCounters[i].weaponCounterIndex == weaponIndex)
            {
                foundCounterIndex = i;
                Destroy(m_AmmoCounters[i].gameObject);
            }
        }

        if(foundCounterIndex >= 0)
        {
            m_AmmoCounters.RemoveAt(foundCounterIndex);
        }
    }

    void ChangeWeapon(WeaponController weapon)
    {
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(ammosPanel);
    }
}
