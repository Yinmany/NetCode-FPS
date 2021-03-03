using UnityEngine;

[RequireComponent(typeof(Pickup))]
public class WeaponPickup : MonoBehaviour
{
    [Tooltip("The prefab for the weapon that will be added to the player on pickup")]
    public WeaponController weaponPrefab;

    Pickup m_Pickup;

    void Start()
    {
        m_Pickup = GetComponent<Pickup>();
        DebugUtility.HandleErrorIfNullGetComponent<Pickup, WeaponPickup>(m_Pickup, this, gameObject);

        // Subscribe to pickup action
        m_Pickup.onPick += OnPicked;

        // Set all children layers to default (to prefent seeing weapons through meshes)
        foreach(Transform t in GetComponentsInChildren<Transform>())
        {
            if (t != transform)
                t.gameObject.layer = 0;
        }
    }

    void OnPicked(PlayerCharacterController byPlayer)
    {
        PlayerWeaponsManager playerWeaponsManager = byPlayer.GetComponent<PlayerWeaponsManager>();
        if (playerWeaponsManager)
        {
            if (playerWeaponsManager.AddWeapon(weaponPrefab))
            {
                // Handle auto-switching to weapon if no weapons currently
                if (playerWeaponsManager.GetActiveWeapon() == null)
                {
                    playerWeaponsManager.SwitchWeapon(true);
                }

                m_Pickup.PlayPickupFeedback();

                Destroy(gameObject);
            }
        }
    }
}
