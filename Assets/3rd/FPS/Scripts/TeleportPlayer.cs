using UnityEngine;

// Debug script, teleports the player across the map for faster testing
public class TeleportPlayer : MonoBehaviour
{
    public KeyCode activateKey = KeyCode.F12;

    PlayerCharacterController m_PlayerCharacterController;

    void Awake()
    {
        m_PlayerCharacterController = FindObjectOfType<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, TeleportPlayer>(m_PlayerCharacterController, this);
    }

    void Update()
    {
        if (Input.GetKeyDown(activateKey))
        {
            m_PlayerCharacterController.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Health playerHealth = m_PlayerCharacterController.GetComponent<Health>();
            if(playerHealth)
            {
                playerHealth.Heal(999);
            }
        }
    }

}
