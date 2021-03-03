using System.Collections.Generic;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    public List<GameObject> combineParents = new List<GameObject>();

    [Header("Grid parameters")]
    public bool useGrid = false;
    public Vector3 gridCenter;
    public Vector3 gridExtents = new Vector3(10, 10, 10);
    public Vector3Int gridResolution = new Vector3Int(2, 2, 2);
    public Color gridPreviewColor = Color.green;

    private void Start()
    {
        Combine();
    }

    public void Combine()
    {
        List<MeshRenderer> validRenderers = new List<MeshRenderer>();
        foreach (GameObject combineParent in combineParents)
        {
            validRenderers.AddRange(combineParent.GetComponentsInChildren<MeshRenderer>());
        }

        if (useGrid)
        {
            for (int i = 0; i < GetGridCellCount(); i++)
            {
                if (GetGridCellBounds(i, out Bounds bounds))
                {
                    CombineAllInBounds(bounds, validRenderers);
                }
            }
        }
        else
        {
            MeshCombineUtility.Combine(validRenderers, MeshCombineUtility.RendererDisposeMethod.DestroyRendererAndFilter, "Level_Combined");
        }
    }

    void CombineAllInBounds(Bounds bounds, List<MeshRenderer> validRenderers)
    {
        List<MeshRenderer> renderersForThisCell = new List<MeshRenderer>();

        for (int i = validRenderers.Count - 1; i >= 0; i--)
        {
            MeshRenderer m = validRenderers[i];
            if (bounds.Intersects(m.bounds))
            {
                renderersForThisCell.Add(m);
                validRenderers.Remove(m);
            }
        } 

        if (renderersForThisCell.Count > 0)
        {
            MeshCombineUtility.Combine(renderersForThisCell, MeshCombineUtility.RendererDisposeMethod.DestroyRendererAndFilter, "Level_Combined");
        }
    }

    int GetGridCellCount()
    {
        return gridResolution.x * gridResolution.y * gridResolution.z;
    }

    public bool GetGridCellBounds(int index, out Bounds bounds)
    {
        bounds = default;
        if (index < 0 || index >= GetGridCellCount())
            return false;

        int xCoord = index / (gridResolution.y * gridResolution.z);
        int yCoord = (index / gridResolution.z) % gridResolution.y;
        int zCoord = index % gridResolution.z;

        Vector3 gridBottomCorner = gridCenter - (gridExtents * 0.5f);
        Vector3 cellSize = new Vector3(gridExtents.x / (float)gridResolution.x, gridExtents.y / (float)gridResolution.y, gridExtents.z / (float)gridResolution.z);
        Vector3 cellCenter = gridBottomCorner + (new Vector3((xCoord * cellSize.x) + (cellSize.x * 0.5f), (yCoord * cellSize.y) + (cellSize.y * 0.5f), (zCoord * cellSize.z) + (cellSize.z * 0.5f)));

        bounds.center = cellCenter;
        bounds.size = cellSize;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (useGrid)
        {
            Gizmos.color = gridPreviewColor;

            for (int i = 0; i < GetGridCellCount(); i++)
            {
                if(GetGridCellBounds(i, out Bounds bounds))
                {
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }
    }
}
