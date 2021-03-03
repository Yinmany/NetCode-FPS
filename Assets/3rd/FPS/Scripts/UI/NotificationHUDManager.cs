using UnityEngine;

public class NotificationHUDManager : MonoBehaviour
{
    [Tooltip("UI panel containing the layoutGroup for displaying notifications")]
    public RectTransform notificationPanel;
    [Tooltip("Prefab for the notifications")]
    public GameObject notificationPrefab;


    void Awake()
    {
        PlayerWeaponsManager playerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, NotificationHUDManager>(playerWeaponsManager, this);
        playerWeaponsManager.onAddedWeapon += OnPickupWeapon;

        Jetpack jetpack = FindObjectOfType<Jetpack>();
        DebugUtility.HandleErrorIfNullFindObject<Jetpack, NotificationHUDManager>(jetpack, this);
        jetpack.onUnlockJetpack += OnUnlockJetpack;
    }

    void OnUpdateObjective(UnityActionUpdateObjective updateObjective)
    {
        if (!string.IsNullOrEmpty(updateObjective.notificationText))
            CreateNotification(updateObjective.notificationText);
    }

    void OnPickupWeapon(WeaponController weaponController, int index)
    {
        if (index != 0)
            CreateNotification("Picked up weapon : " + weaponController.weaponName);
    }

    void OnUnlockJetpack(bool unlock)
    {
        CreateNotification("Jetpack unlocked");
    }

    public void CreateNotification(string text)
    {
        GameObject notificationInstance = Instantiate(notificationPrefab, notificationPanel);
        notificationInstance.transform.SetSiblingIndex(0);

        NotificationToast toast = notificationInstance.GetComponent<NotificationToast>();
        if (toast)
        {
            toast.Initialize(text);
        }
    }

    public void RegisterObjective(Objective objective)
    {
        objective.onUpdateObjective += OnUpdateObjective;
    }

    public void UnregisterObjective(Objective objective)
    {
        objective.onUpdateObjective -= OnUpdateObjective;
    }
}
