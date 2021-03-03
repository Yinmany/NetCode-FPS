using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(SimpleCrosshair))]
public class SimpleCrosshairEditor : Editor
{
    private SimpleCrosshair simpleCrosshair;
    private Texture2D lastCrosshairTexture;
    private GUIStyle imageStyle;
    private bool realtimeUpdate = true;

    private void OnEnable()
    {
        simpleCrosshair = (SimpleCrosshair)target;
        imageStyle = new GUIStyle();
        imageStyle.alignment = TextAnchor.MiddleCenter;

    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Space(10);
        GUILayout.Label("Crosshair Preview", EditorStyles.boldLabel);
        realtimeUpdate = EditorGUILayout.Toggle("Realtime Preview", realtimeUpdate);
        if (realtimeUpdate)
        {
            lastCrosshairTexture = simpleCrosshair.DrawCrosshair(simpleCrosshair.GetCrosshair());
            GeneratePreview();
        }
        else
        {
            if (GUILayout.Button("Generate Preview"))
            {
                lastCrosshairTexture = simpleCrosshair.DrawCrosshair(simpleCrosshair.GetCrosshair());
                GeneratePreview();
            }
        }

        if(lastCrosshairTexture != null)
        {
            GUILayout.Box(lastCrosshairTexture, imageStyle);
        }
    }

    private void GeneratePreview()
    {
        if (simpleCrosshair.m_crosshairImage)
        {
            simpleCrosshair.m_crosshairImage.rectTransform.sizeDelta = new Vector2(simpleCrosshair.GetCrosshair().SizeNeeded, simpleCrosshair.GetCrosshair().SizeNeeded);
            Sprite crosshairSprite = Sprite.Create(lastCrosshairTexture,
                new Rect(0, 0, lastCrosshairTexture.width, lastCrosshairTexture.height),
                Vector2.one / 2);

            simpleCrosshair.m_crosshairImage.sprite = crosshairSprite;
        }
    }
}
