using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargedProjectileEffectsHandler : MonoBehaviour
{
    [Tooltip("Object that will be affected by charging scale & color changes")]
    public GameObject chargingObject;
    [Tooltip("Scale of the charged object based on charge")]
    public MinMaxVector3 scale;
    [Tooltip("Color of the charged object based on charge")]
    public MinMaxColor color;

    MeshRenderer[] m_AffectedRenderers;
    ProjectileBase m_ProjectileBase;

    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ChargedProjectileEffectsHandler>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;

        m_AffectedRenderers = chargingObject.GetComponentsInChildren<MeshRenderer>();
        foreach (var ren in m_AffectedRenderers)
        {
            ren.sharedMaterial = Instantiate(ren.sharedMaterial);
        }
    }

    void OnShoot()
    {
        chargingObject.transform.localScale = scale.GetValueFromRatio(m_ProjectileBase.initialCharge);

        foreach (var ren in m_AffectedRenderers)
        {
            ren.sharedMaterial.SetColor("_Color", color.GetValueFromRatio(m_ProjectileBase.initialCharge));
        }
    }
}
