using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    public RectTransform compasRect;
    public float visibilityAngle = 180f;
    public float heightDifferenceMultiplier = 2f;
    public float minScale = 0.5f;
    public float distanceMinScale = 50f;
    public float compasMarginRatio = 0.8f;

    public GameObject MarkerDirectionPrefab;

    Transform m_PlayerTransform;
    Dictionary<Transform, CompassMarker> m_ElementsDictionnary = new Dictionary<Transform, CompassMarker>();

    float m_WidthMultiplier;
    float m_heightOffset;

    void Awake()
    {
        PlayerCharacterController playerCharacterController = FindObjectOfType<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, Compass>(playerCharacterController, this);
        m_PlayerTransform = playerCharacterController.transform;

        m_WidthMultiplier = compasRect.rect.width / visibilityAngle;
        m_heightOffset = -compasRect.rect.height / 2;
    }

    void Update()
    {
        // this is all very WIP, and needs to be reworked
        foreach (var element in m_ElementsDictionnary)
        {
            float distanceRatio = 1;
            float heightDifference = 0;
            float angle;

            if (element.Value.isDirection)
            {
                angle = Vector3.SignedAngle(m_PlayerTransform.forward, element.Key.transform.localPosition.normalized, Vector3.up);
            }
            else
            {
                Vector3 targetDir = (element.Key.transform.position - m_PlayerTransform.position).normalized;
                targetDir = Vector3.ProjectOnPlane(targetDir, Vector3.up);
                Vector3 playerForward = Vector3.ProjectOnPlane(m_PlayerTransform.forward, Vector3.up);
                angle = Vector3.SignedAngle(playerForward, targetDir, Vector3.up);

                Vector3 directionVector = element.Key.transform.position - m_PlayerTransform.position;

                heightDifference = (directionVector.y) * heightDifferenceMultiplier;
                heightDifference = Mathf.Clamp(heightDifference, -compasRect.rect.height / 2 * compasMarginRatio, compasRect.rect.height / 2 * compasMarginRatio);

                distanceRatio = directionVector.magnitude / distanceMinScale;
                distanceRatio = Mathf.Clamp01(distanceRatio);
            }

            if (angle > -visibilityAngle / 2 && angle < visibilityAngle / 2)
            {
                element.Value.canvasGroup.alpha = 1;
                element.Value.canvasGroup.transform.localPosition = new Vector2(m_WidthMultiplier * angle, heightDifference + m_heightOffset);
                element.Value.canvasGroup.transform.localScale = Vector3.one * Mathf.Lerp(1, minScale, distanceRatio);
            }
            else
            {
                element.Value.canvasGroup.alpha = 0;
            }
        }
    }

    public void RegisterCompassElement(Transform element, CompassMarker marker)
    {
        marker.transform.SetParent(compasRect);

        m_ElementsDictionnary.Add(element, marker);
    }

    public void UnregisterCompassElement(Transform element)
    {
        if (m_ElementsDictionnary.TryGetValue(element, out CompassMarker marker) && marker.canvasGroup != null)
            Destroy(marker.canvasGroup.gameObject);
        m_ElementsDictionnary.Remove(element);
    }
}
