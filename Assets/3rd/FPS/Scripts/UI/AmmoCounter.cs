using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(FillBarColorChange))]
public class AmmoCounter : MonoBehaviour
{
    [Tooltip("CanvasGroup to fade the ammo UI")]
    public CanvasGroup canvasGroup;
    [Tooltip("Image for the weapon icon")]
    public Image weaponImage;
    [Tooltip("Image component for the background")]
    public Image ammoBackgroundImage;
    [Tooltip("Image component to display fill ratio")]
    public Image ammoFillImage;
    [Tooltip("Text for image index")]
    public TMPro.TextMeshProUGUI weaponIndexText;

    [Header("Selection")]
    [Range(0, 1)]
    [Tooltip("Opacity when weapon not selected")]
    public float unselectedOpacity = 0.5f;
    [Tooltip("Scale when weapon not selected")]
    public Vector3 unselectedScale = Vector3.one * 0.8f;
    [Tooltip("Root for the control keys")]
    public GameObject controlKeysRoot;

    [Header("Feedback")]
    [Tooltip("Component to animate the color when empty or full")]
    public FillBarColorChange FillBarColorChange;
    [Tooltip("Sharpness for the fill ratio movements")]
    public float ammoFillMovementSharpness = 20f;

    public int weaponCounterIndex { get; set; }

    PlayerWeaponsManager m_PlayerWeaponsManager;
    WeaponController m_Weapon;

    public void Initialize(WeaponController weapon, int weaponIndex)
    {
        m_Weapon = weapon;
        weaponCounterIndex = weaponIndex;
        weaponImage.sprite = weapon.weaponIcon;

        m_PlayerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, AmmoCounter>(m_PlayerWeaponsManager, this);

        weaponIndexText.text = (weaponCounterIndex + 1).ToString();

        FillBarColorChange.Initialize(1f, m_Weapon.GetAmmoNeededToShoot());
    }

    void Update()
    {
        float currenFillRatio = m_Weapon.currentAmmoRatio;
        ammoFillImage.fillAmount = Mathf.Lerp(ammoFillImage.fillAmount, currenFillRatio, Time.deltaTime * ammoFillMovementSharpness);

        bool isActiveWeapon = m_Weapon == m_PlayerWeaponsManager.GetActiveWeapon();

        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha , isActiveWeapon ? 1f : unselectedOpacity, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, isActiveWeapon ? Vector3.one : unselectedScale, Time.deltaTime * 10);
        controlKeysRoot.SetActive(!isActiveWeapon);

        FillBarColorChange.UpdateVisual(currenFillRatio);
    }
}
