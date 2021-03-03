using UnityEngine;

public class Destructable : MonoBehaviour
{
    Health m_Health;

    void Start()
    {
        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, Destructable>(m_Health, this, gameObject);

        // Subscribe to damage & death actions
        m_Health.onDie += OnDie;
        m_Health.onDamaged += OnDamaged;
    }

    void OnDamaged(float damage, GameObject damageSource)
    {
        // TODO: damage reaction
    }

    void OnDie()
    {
        // this will call the OnDestroy function
        Destroy(gameObject);
    }
}
