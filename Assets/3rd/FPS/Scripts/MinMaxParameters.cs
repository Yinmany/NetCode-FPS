using UnityEngine;

[System.Serializable]
public struct MinMaxFloat
{
    public float min;
    public float max;

    public float GetValueFromRatio(float ratio)
    {
        return Mathf.Lerp(min, max, ratio);
    }
}

[System.Serializable]
public struct MinMaxColor
{
    [ColorUsage(true, true)]
    public Color min;
    [ColorUsage(true, true)]
    public Color max;

    public Color GetValueFromRatio(float ratio)
    {
        return Color.Lerp(min, max, ratio);
    }
}

[System.Serializable]
public struct MinMaxVector3
{
    public Vector3 min;
    public Vector3 max;

    public Vector3 GetValueFromRatio(float ratio)
    {
        return Vector3.Lerp(min, max, ratio);
    }
}
