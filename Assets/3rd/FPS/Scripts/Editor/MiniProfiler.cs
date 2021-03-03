using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MiniProfiler : EditorWindow
{
    private class BoundsAndCount
    {
        public Bounds bounds;
        public int count;
    }

    private class CellData
    {
        public Bounds bounds;
        public int count;
        public float ratio;
        public Color color;
    }

    Vector2 m_ScrollPos;
    bool m_MustRepaint = false;
    bool m_MustLaunchHeatmapNextFrame = false;
    bool m_HeatmapIsCalculating = false;
    float m_CellTransparency = 0.9f;
    float m_CellThreshold = 0f;
    string m_LevelAnalysisString = "";
    List<string> m_SuggestionStrings = new List<string>();

    static private List<CellData> m_CellDatas = new List<CellData>();

    const float k_CellSize = 10;
    const string k_NewLine = "\n";
    const string k_HeaderSeparator = "==============================";

    // Add menu item named "My Window" to the Window menu
    [MenuItem("Tools/MiniProfiler")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(MiniProfiler));
    }

    private void OnEnable()
    {
#if UNITY_2019_1_OR_NEWER
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
#elif UNITY_2018_1_OR_NEWER
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
    }

    void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, false, false);

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Performance Tips");
        DisplayTips();

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Level Analysis");
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("You must exit Play mode for this feature to be available", MessageType.Warning);
        }
        else
        {
            if (GUILayout.Button("Analyze"))
            {
                AnalyzeLevel();
            }

            if (m_LevelAnalysisString != null && m_LevelAnalysisString != "")
            {
                EditorGUILayout.HelpBox(m_LevelAnalysisString, MessageType.None);
            }
            if (m_SuggestionStrings.Count > 0)
            {
                EditorGUILayout.LabelField("Suggestions");
                foreach (var s in m_SuggestionStrings)
                {
                    EditorGUILayout.HelpBox(s, MessageType.Warning);
                }
            }
            if (GUILayout.Button("Clear Analysis"))
            {
                ClearAnalysis();
                m_MustRepaint = true;
            }
        }


        GUILayout.Space(20);
        EditorGUILayout.LabelField("Polygon count Heatmap");
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("You must exit Play mode for this feature to be available", MessageType.Warning);
        }
        else
        {
            if (m_MustLaunchHeatmapNextFrame)
            {
                DoPolycountMap();
                m_CellTransparency = 0.9f;
                m_CellThreshold = 0f;
                m_MustLaunchHeatmapNextFrame = false;
                m_MustRepaint = true;
            }
            if (GUILayout.Button("Build Heatmap"))
            {
                m_MustLaunchHeatmapNextFrame = true;
                m_HeatmapIsCalculating = true;
            }

            if (m_CellDatas.Count > 0)
            {
                float prevAlpha = m_CellTransparency;
                m_CellTransparency = EditorGUILayout.Slider("Cell Transparency", m_CellTransparency, 0f, 1f);
                if (m_CellTransparency != prevAlpha)
                {
                    m_MustRepaint = true;
                }

                float prevTreshold = m_CellThreshold;
                m_CellThreshold = EditorGUILayout.Slider("Cell Display Threshold", m_CellThreshold, 0f, 1f);
                if (m_CellThreshold != prevTreshold)
                {
                    m_MustRepaint = true;
                }
            }

            if (GUILayout.Button("Clear Heatmap"))
            {
                m_MustRepaint = true;
                m_CellDatas.Clear();
            }
        }

        EditorGUILayout.EndScrollView();

        if(m_MustRepaint)
        {
            EditorWindow.GetWindow<SceneView>().Repaint();
            m_MustRepaint = false;
        }

        if (m_HeatmapIsCalculating)
            EditorUtility.DisplayProgressBar("Polygon Count Heatmap", "Calculations in progress", 0.99f);
    }

    void OnSceneGUI(SceneView sceneView)
    {
        // Draw heatmap
        foreach (CellData c in m_CellDatas)
        {
            if (c.ratio >= m_CellThreshold && c.count > 0)
            {
                Color col = c.color;
                col.a = 1f - m_CellTransparency;
                Handles.color = col;
                Handles.CubeHandleCap(0, c.bounds.center, Quaternion.identity, c.bounds.extents.x * 2f, EventType.Repaint);
            }
        }
    }

    void ClearAnalysis()
    {
        m_LevelAnalysisString = "";
        m_SuggestionStrings.Clear();
    }

    void DisplayTips()
    {
        EditorGUILayout.HelpBox("All of your meshes that will never move (floor/wall meshes, for examples) should be placed as children of the \"Level\" GameObject in the scene. This is because the \"Mesh Combiner\" script on that object will take care of combining all meshes under it on game start, and this reduces the cost of rendering them. It is more efficient to render one big mesh than lots of small meshes, even when the number of polygons is the same.", MessageType.None);
        EditorGUILayout.HelpBox("Every light added to the level will have a performance cost. If you do add more lights to the level, consider making them not cast any shadows to reduce the performance impact. However, be aware that in WebGL there is a limit of 4 lights to be drawn on screen at the same time", MessageType.None);
        EditorGUILayout.HelpBox("Transparent objects are more expensive for performance than opaque objects", MessageType.None);
        EditorGUILayout.HelpBox("Animated 3D models (known as \"Skinned Meshes\") are more expensive for performance than regular meshes", MessageType.None);
        EditorGUILayout.HelpBox("Having a lot of enemies in the level could impact performance, due to their AI logic", MessageType.None);
        EditorGUILayout.HelpBox("Adding rigidbodies (physics objects) to the level could impact performance", MessageType.None);
        EditorGUILayout.HelpBox("Open the Profiler window from the top menu bar (Window > Analysis > Profiler) to see in-depth information about your game's performance while you are playing", MessageType.None);
    }

    void AnalyzeLevel()
    {
        ClearAnalysis();
        EditorStyles.textArea.wordWrap = true;
        MeshCombiner mainMeshCombiner = GameObject.FindObjectOfType<MeshCombiner>();

        // Analyze
        MeshFilter[] meshFilters = GameObject.FindObjectsOfType<MeshFilter>();
        SkinnedMeshRenderer[] skinnedMeshes = GameObject.FindObjectsOfType<SkinnedMeshRenderer>();
        int skinnedMeshesCount = skinnedMeshes.Length;
        int meshCount = meshFilters.Length;
        int nonCombinedMeshCount = 0;
        int polyCount = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            if (!mf.sharedMesh)
                continue;

            polyCount += mf.sharedMesh.triangles.Length / 3;

            bool willBeCombined = false;
            if(mainMeshCombiner)
            {
                foreach (GameObject combineParent in mainMeshCombiner.combineParents)
                {
                    if(mf.transform.IsChildOf(combineParent.transform))
                    {
                        willBeCombined = true;
                    }
                }
            }

            if(!willBeCombined)
            {
                if (!(mf.GetComponentInParent<PlayerCharacterController>() ||
                    mf.GetComponentInParent<EnemyController>() ||
                    mf.GetComponentInParent<Pickup>() ||
                    mf.GetComponentInParent<Objective>()))
                {
                    nonCombinedMeshCount++;
                }
            }
        }

        foreach (SkinnedMeshRenderer sm in skinnedMeshes)
        {
            polyCount += sm.sharedMesh.triangles.Length / 3;
        }

        int rigidbodiesCount = 0;
        foreach (var r in GameObject.FindObjectsOfType<Rigidbody>())
        {
            if (!r.isKinematic)
            {
                rigidbodiesCount++;
            }
        }
        int lightsCount = GameObject.FindObjectsOfType<Light>().Length;
        int enemyCount = GameObject.FindObjectsOfType<EnemyController>().Length;

        // Level analysis 
        m_LevelAnalysisString += "- Meshes count: " + meshCount;
        m_LevelAnalysisString += k_NewLine;
        m_LevelAnalysisString += "- Animated models (SkinnedMeshes) count: " + skinnedMeshesCount;
        m_LevelAnalysisString += k_NewLine;
        m_LevelAnalysisString += "- Polygon count: " + polyCount;
        m_LevelAnalysisString += k_NewLine;
        m_LevelAnalysisString += "- Physics objects (rigidbodies) count: " + rigidbodiesCount;
        m_LevelAnalysisString += k_NewLine;
        m_LevelAnalysisString += "- Lights count: " + lightsCount;
        m_LevelAnalysisString += k_NewLine;
        m_LevelAnalysisString += "- Enemy count: " + enemyCount;

        // Suggestions
        if (nonCombinedMeshCount > 50)
        {
            m_SuggestionStrings.Add(nonCombinedMeshCount + " meshes in the scene are not setup to be combined on game start. Make sure that all the meshes " +
                "that will never move, change, or be removed during play are under the \"Level\" gameObject in the scene, so they can be combined for greater performance. \n \n" + 
                "Note that it is always normal to have a few meshes that will not be combined, such as pickups, player meshes, enemy meshes, etc....");
        }
    }

    void DoPolycountMap()
    {
        m_CellDatas.Clear();
        List<BoundsAndCount> meshBoundsAndCount = new List<BoundsAndCount>();
        Bounds levelBounds = new Bounds();
        Renderer[] allRenderers = GameObject.FindObjectsOfType<Renderer>();

        // Get level bounds and list of bounds & polycount
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer r = allRenderers[i];
            if (r.gameObject.GetComponent<IgnoreHeatMap>())
                continue;

            levelBounds.Encapsulate(r.bounds);

            MeshRenderer mr = (r as MeshRenderer);
            if (mr)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf && mf.sharedMesh != null)
                {
                    BoundsAndCount b = new BoundsAndCount();
                    b.bounds = r.bounds;
                    b.count = mf.sharedMesh.triangles.Length / 3;

                    meshBoundsAndCount.Add(b);
                }
            }
            else
            {
                SkinnedMeshRenderer smr = (r as SkinnedMeshRenderer);
                if (smr)
                {
                    if (smr.sharedMesh != null)
                    {
                        BoundsAndCount b = new BoundsAndCount();
                        b.bounds = r.bounds;
                        b.count = smr.sharedMesh.triangles.Length / 3;

                        meshBoundsAndCount.Add(b);
                    }
                }
            }
        }

        Vector3 boundsBottomCorner = levelBounds.center - levelBounds.extents;
        Vector3Int gridResolution = new Vector3Int(Mathf.CeilToInt((levelBounds.extents.x * 2f) / k_CellSize), Mathf.CeilToInt((levelBounds.extents.y * 2f) / k_CellSize), Mathf.CeilToInt((levelBounds.extents.z * 2f) / k_CellSize));

        int highestCount = 0;
        for (int x = 0; x < gridResolution.x; x++)
        {
            for (int y = 0; y < gridResolution.y; y++)
            {
                for (int z = 0; z < gridResolution.z; z++)
                {
                    CellData cellData = new CellData();

                    Vector3 cellCenter = boundsBottomCorner + (new Vector3(x, y, z) * k_CellSize) + (Vector3.one * k_CellSize * 0.5f);
                    cellData.bounds = new Bounds(cellCenter, Vector3.one * k_CellSize);
                    for (int i = 0; i < meshBoundsAndCount.Count; i++)
                    {
                        if (cellData.bounds.Intersects(meshBoundsAndCount[i].bounds))
                        {
                            cellData.count += meshBoundsAndCount[i].count;
                        }
                    }

                    if (cellData.count > highestCount)
                    {
                        highestCount = cellData.count;
                    }

                    m_CellDatas.Add(cellData);
                }
            }
        }

        for (int i = 0; i < m_CellDatas.Count; i++)
        {
            m_CellDatas[i].ratio = (float)m_CellDatas[i].count / (float)highestCount;
            Color col = Color.Lerp(Color.green, Color.red, m_CellDatas[i].ratio);
            m_CellDatas[i].color = col;
        }

        m_HeatmapIsCalculating = false;
        EditorUtility.ClearProgressBar();
    }
}