using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;

public static class MeshCombineUtility
{
    public class RenderBatchData
    {
        public class MeshAndTRS
        {
            public Mesh mesh;
            public Matrix4x4 trs;

            public MeshAndTRS(Mesh m, Matrix4x4 t)
            {
                mesh = m;
                trs = t;
            }
        }

        public Material material;
        public int submeshIndex = 0;
        public ShadowCastingMode shadowMode;
        public bool receiveShadows;
        public MotionVectorGenerationMode motionVectors;
        public List<MeshAndTRS> meshesWithTRS = new List<MeshAndTRS>();
    }

    public enum RendererDisposeMethod
    {
        DestroyGameObject,
        DestroyRendererAndFilter,
        DisableGameObject,
        DisableRenderer,
    }

    public static void Combine(List<MeshRenderer> renderers, RendererDisposeMethod disposeMethod, string newObjectName)
    {
        int renderersCount = renderers.Count;

        List<RenderBatchData> renderBatches = new List<RenderBatchData>();
        
        // Build render batches for all unique material + submeshIndex combinations
        for (int i = 0; i < renderersCount; i++)
        {
            MeshRenderer meshRenderer = renderers[i];

            if (meshRenderer == null)
                continue;

            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();

            if (meshFilter == null)
                continue;

            Mesh mesh = meshFilter.sharedMesh;

            if (mesh == null)
                continue;

            Transform t = meshRenderer.GetComponent<Transform>();
            Material[] materials = meshRenderer.sharedMaterials;

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                if (materials[s] == null)
                    continue;

                int batchIndex = GetExistingRenderBatch(renderBatches, materials[s], meshRenderer, s);
                if(batchIndex >= 0)
                {
                    renderBatches[batchIndex].meshesWithTRS.Add(new RenderBatchData.MeshAndTRS(mesh, Matrix4x4.TRS(t.position, t.rotation, t.lossyScale)));
                }
                else
                {
                    RenderBatchData newBatchData = new RenderBatchData();
                    newBatchData.material = materials[s];
                    newBatchData.submeshIndex = s;
                    newBatchData.shadowMode = meshRenderer.shadowCastingMode;
                    newBatchData.receiveShadows = meshRenderer.receiveShadows;
                    newBatchData.meshesWithTRS.Add(new RenderBatchData.MeshAndTRS(mesh, Matrix4x4.TRS(t.position, t.rotation, t.lossyScale)));

                    renderBatches.Add(newBatchData);
                }
            }

            // Destroy probuilder component if present
            ProBuilderMesh pbm = meshRenderer.GetComponent<ProBuilderMesh>();
            if(pbm)
            {
                GameObject.Destroy(pbm);
            }

            switch (disposeMethod)
            {
                case RendererDisposeMethod.DestroyGameObject:
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(meshRenderer.gameObject);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(meshRenderer.gameObject);
                    }
                    break;
                case RendererDisposeMethod.DestroyRendererAndFilter:
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(meshRenderer);
                        GameObject.Destroy(meshFilter);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(meshRenderer);
                        GameObject.DestroyImmediate(meshFilter);
                    }
                    break;
                case RendererDisposeMethod.DisableGameObject:
                    meshRenderer.gameObject.SetActive(false);
                    break;
                case RendererDisposeMethod.DisableRenderer:
                    meshRenderer.enabled = false;
                    break;
            }
        }

        // Combine each unique render batch
        for (int i = 0; i < renderBatches.Count; i++)
        {
            RenderBatchData rbd = renderBatches[i];

            Mesh newMesh = new Mesh();
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            CombineInstance[] combineInstances = new CombineInstance[rbd.meshesWithTRS.Count];

            for (int j = 0; j < rbd.meshesWithTRS.Count; j++)
            {
                combineInstances[j].subMeshIndex = rbd.submeshIndex;
                combineInstances[j].mesh = rbd.meshesWithTRS[j].mesh;
                combineInstances[j].transform = rbd.meshesWithTRS[j].trs;
            }

            // Create mesh
            newMesh.CombineMeshes(combineInstances);
            newMesh.RecalculateBounds();

            // Create the gameObject
            GameObject combinedObject = new GameObject(newObjectName);
            MeshFilter mf = combinedObject.AddComponent<MeshFilter>();
            mf.sharedMesh = newMesh;
            MeshRenderer mr = combinedObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = rbd.material;
            mr.shadowCastingMode = rbd.shadowMode;
        }
    }

    private static int GetExistingRenderBatch(List<RenderBatchData> renderBatches, Material mat, MeshRenderer ren, int submeshIndex)
    {
        for (int i = 0; i < renderBatches.Count; i++)
        {
            RenderBatchData data = renderBatches[i];
            if (data.material == mat && 
                data.submeshIndex == submeshIndex &&
                data.shadowMode == ren.shadowCastingMode &&
                data.receiveShadows == ren.receiveShadows)
            {
                return i;
            }
        }

        return -1;
    }
}
