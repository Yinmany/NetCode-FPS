using UnityEngine;

public class SimpleCrosshairAPIExample : MonoBehaviour
{
    public SimpleCrosshair simpleCrosshair;

    private void Start()
    {
        if(simpleCrosshair == null)
        {
            Debug.LogError("You have not set the target SimpleCrosshair. Disabling the example script.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            int curGap = simpleCrosshair.GetGap();
            simpleCrosshair.SetGap(curGap + 2, true);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            int curGap = simpleCrosshair.GetGap();
            simpleCrosshair.SetGap(curGap - 2, true);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            int curThickness = simpleCrosshair.GetThickness();
            simpleCrosshair.SetThickness(curThickness + 2, true);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            int curThickness = simpleCrosshair.GetThickness();
            simpleCrosshair.SetThickness(curThickness - 2, true);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            int curSize = simpleCrosshair.GetSize();
            simpleCrosshair.SetSize(curSize + 2, true);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            int curSize = simpleCrosshair.GetSize();
            simpleCrosshair.SetSize(curSize - 2, true);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            simpleCrosshair.SetColor(Random.ColorHSV(), true);
        }
    }
}
