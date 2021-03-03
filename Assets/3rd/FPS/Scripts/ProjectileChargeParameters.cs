using UnityEngine;

public class ProjectileChargeParameters : MonoBehaviour
{
    public MinMaxFloat damage;
    public MinMaxFloat radius;
    public MinMaxFloat speed;
    public MinMaxFloat gravityDownAcceleration;
    public MinMaxFloat areaOfEffectDistance;

    ProjectileBase m_ProjectileBase;

    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ProjectileChargeParameters>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;
    }

    void OnShoot()
    {
        // Apply the parameters based on projectile charge
        ProjectileStandard proj = GetComponent<ProjectileStandard>();
        if(proj)
        {
            proj.damage = damage.GetValueFromRatio(m_ProjectileBase.initialCharge);
            proj.radius = radius.GetValueFromRatio(m_ProjectileBase.initialCharge);
            proj.speed = speed.GetValueFromRatio(m_ProjectileBase.initialCharge);
            proj.gravityDownAcceleration = gravityDownAcceleration.GetValueFromRatio(m_ProjectileBase.initialCharge);
        }
    }
}
