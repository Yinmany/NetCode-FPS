using UnityEngine;
using System.Collections.Generic;

public class OverheatBehavior : MonoBehaviour
{
    [System.Serializable]
    public struct RendererIndexData
    {
        public Renderer renderer;
        public int materialIndex;

        public RendererIndexData(Renderer renderer, int index)
        {
            this.renderer = renderer;
            this.materialIndex = index;
        }
    }

    [Header("Visual")]
    [Tooltip("The VFX to scale the spawn rate based on the ammo ratio")]
    public ParticleSystem steamVFX;
    [Tooltip("The emission rate for the effect when fully overheated")]
    public float steamVFXEmissionRateMax = 8f;

    //Set gradient field to HDR
    [GradientUsage(true)] 
    [Tooltip("Overheat color based on ammo ratio")]
    public Gradient overheatGradient;
    [Tooltip("The material for overheating color animation")]
    public Material overheatingMaterial;

    [Header("Sound")]
    [Tooltip("Sound played when a cell are cooling")]
    public AudioClip coolingCellsSound;
    [Tooltip("Curve for ammo to volume ratio")]
    public AnimationCurve ammoToVolumeRatioCurve;


    WeaponController m_Weapon;
    AudioSource m_AudioSource;
    List<RendererIndexData> m_OverheatingRenderersData;
    MaterialPropertyBlock overheatMaterialPropertyBlock;
    float m_LastAmmoRatio;
    ParticleSystem.EmissionModule m_SteamVFXEmissionModule;

    void Awake()
    {
        var emissionModule = steamVFX.emission;
        emissionModule.rateOverTimeMultiplier = 0f;

        m_OverheatingRenderersData = new List<RendererIndexData>();
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (renderer.sharedMaterials[i] == overheatingMaterial)
                    m_OverheatingRenderersData.Add(new RendererIndexData(renderer, i));
            }
        }

        overheatMaterialPropertyBlock = new MaterialPropertyBlock();
        m_SteamVFXEmissionModule = steamVFX.emission;

        m_Weapon = GetComponent<WeaponController>();
        DebugUtility.HandleErrorIfNullGetComponent<WeaponController, OverheatBehavior>(m_Weapon, this, gameObject);

        m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.clip = coolingCellsSound;
        m_AudioSource.outputAudioMixerGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponOverheat);
    }

    void Update()
    {
        // visual smoke shooting out of the gun
        float currentAmmoRatio = m_Weapon.currentAmmoRatio;
        if (currentAmmoRatio != m_LastAmmoRatio)
        {
            overheatMaterialPropertyBlock.SetColor("_EmissionColor", overheatGradient.Evaluate(1f - currentAmmoRatio));

            foreach (var data in m_OverheatingRenderersData)
            {
                data.renderer.SetPropertyBlock(overheatMaterialPropertyBlock, data.materialIndex);
            }

            m_SteamVFXEmissionModule.rateOverTimeMultiplier = steamVFXEmissionRateMax * (1f - currentAmmoRatio);
        }

        // cooling sound
        if (coolingCellsSound)
        {
            if (!m_AudioSource.isPlaying
                && currentAmmoRatio != 1
                && m_Weapon.isWeaponActive
                && m_Weapon.isCooling)
            {
                m_AudioSource.Play();
            }
            else if (m_AudioSource.isPlaying
                && (currentAmmoRatio == 1 || !m_Weapon.isWeaponActive || !m_Weapon.isCooling))
            {
                m_AudioSource.Stop();
                return;
            }

            m_AudioSource.volume = ammoToVolumeRatioCurve.Evaluate(1 - currentAmmoRatio);
        }

        m_LastAmmoRatio = currentAmmoRatio;
    }
}
