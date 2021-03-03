using UnityEngine;

public class JetpackPickup : MonoBehaviour
{
    Pickup m_Pickup;

    void Start()
    {
        m_Pickup = GetComponent<Pickup>();
        DebugUtility.HandleErrorIfNullGetComponent<Pickup, JetpackPickup>(m_Pickup, this, gameObject);

        // Subscribe to pickup action
        m_Pickup.onPick += OnPicked;
    }

    void OnPicked(PlayerCharacterController byPlayer)
    {
        var jetpack = byPlayer.GetComponent<Jetpack>();
        if (!jetpack)
            return;

        if (jetpack.TryUnlock())
        {
            m_Pickup.PlayPickupFeedback();

            Destroy(gameObject);
        }
    }
}
