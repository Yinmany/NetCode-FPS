using UnityEngine;
using UnityEngine.UI;

public class FillBarColorChange : MonoBehaviour
{
    [Header("Foreground")]
    [Tooltip("Image for the foreground")]
    public Image foregroundImage;
    [Tooltip("Default foreground color")]
    public Color defaultForegroundColor;
    [Tooltip("Flash foreground color when full")]
    public Color flashForegroundColorFull;

    [Header("Background")]
    [Tooltip("Image for the background")]
    public Image backgroundImage;
    [Tooltip("Flash background color when empty")]
    public Color defaultBackgroundColor;
    [Tooltip("Sharpness for the color change")]
    public Color flashBackgroundColorEmpty;

    [Header("Values")]
    [Tooltip("Value to consider full")]
    public float fullValue = 1f;
    [Tooltip("Value to consider empty")]
    public float emptyValue = 0f;
    [Tooltip("Sharpness for the color change")]
    public float colorChangeSharpness = 5f;

    float m_PreviousValue;

    public void Initialize(float fullValueRatio, float emptyValueRatio)
    {
        fullValue = fullValueRatio;
        emptyValue = emptyValueRatio;

        m_PreviousValue = fullValueRatio;
    }

    public void UpdateVisual(float currentRatio)
    {
        if (currentRatio == fullValue && currentRatio != m_PreviousValue)
        {
            foregroundImage.color = flashForegroundColorFull;
        }
        else if (currentRatio < emptyValue)
        {
            backgroundImage.color = flashBackgroundColorEmpty;
        }
        else
        {
            foregroundImage.color = Color.Lerp(foregroundImage.color, defaultForegroundColor, Time.deltaTime * colorChangeSharpness);
            backgroundImage.color = Color.Lerp(backgroundImage.color, defaultBackgroundColor, Time.deltaTime * colorChangeSharpness);
        }

        m_PreviousValue = currentRatio;
    }
}
