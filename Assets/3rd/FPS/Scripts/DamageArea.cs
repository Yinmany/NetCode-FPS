using System.Collections.Generic;
using UnityEngine;

public class DamageArea : MonoBehaviour
{
    [Tooltip("Area of damage when the projectile hits something")]
    public float areaOfEffectDistance = 5f;
    [Tooltip("Damage multiplier over distance for area of effect")]
    public AnimationCurve damageRatioOverDistance;

    [Header("Debug")]
    [Tooltip("Color of the area of effect radius")]
    public Color areaOfEffectColor = Color.red * 0.5f;

    public void InflictDamageInArea(float damage, Vector3 center, LayerMask layers, QueryTriggerInteraction interaction, GameObject owner)
    {
        Dictionary<Health, Damageable> uniqueDamagedHealths = new Dictionary<Health, Damageable>();

        // Create a collection of unique health components that would be damaged in the area of effect (in order to avoid damaging a same entity multiple times)
        Collider[] affectedColliders = Physics.OverlapSphere(center, areaOfEffectDistance, layers, interaction);
        foreach (var coll in affectedColliders)
        {
            Damageable damageable = coll.GetComponent<Damageable>();
            if (damageable)
            {
                Health health = damageable.GetComponentInParent<Health>();
                if (health && !uniqueDamagedHealths.ContainsKey(health))
                {
                    uniqueDamagedHealths.Add(health, damageable);
                }
            }
        }

        // Apply damages with distance falloff
        foreach (Damageable uniqueDamageable in uniqueDamagedHealths.Values)
        {
            float distance = Vector3.Distance(uniqueDamageable.transform.position, transform.position);
            uniqueDamageable.InflictDamage(damage * damageRatioOverDistance.Evaluate(distance / areaOfEffectDistance), true, owner);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = areaOfEffectColor;
        Gizmos.DrawSphere(transform.position, areaOfEffectDistance);
    }
}
