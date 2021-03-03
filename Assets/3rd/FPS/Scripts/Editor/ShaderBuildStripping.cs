using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

// Simple example of stripping of a debug build configuration
class ShaderBuildStripping : IPreprocessShaders
{
    List<ShaderKeyword> m_ExcludedKeywords;

    public ShaderBuildStripping()
    {
#if MANUAL_SHADER_STRIPPING
        m_ExcludedKeywords = new List<ShaderKeyword> {
            new ShaderKeyword("DEBUG"),
            // ifdef
            new ShaderKeyword("UNITY_GATHER_SUPPORTED"),
            new ShaderKeyword("UNITY_POSTFX_SSR"),
            new ShaderKeyword("DISTORT"),
            new ShaderKeyword("BLUR_HIGH_QUALITY"),
            new ShaderKeyword("UNITY_CAN_COMPILE_TESSELLATION"),
            new ShaderKeyword("ENABLE_WIND"),
            new ShaderKeyword("WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING"),
            new ShaderKeyword("LOD_FADE_CROSSFADE"),
            new ShaderKeyword("DYNAMICLIGHTMAP_ON"),
            new ShaderKeyword("EDITOR_VISUALIZATION"),
            new ShaderKeyword("UNITY_INSTANCING_ENABLED"),
            new ShaderKeyword("STEREO_MULTIVIEW_ON"),
            new ShaderKeyword("STEREO_INSTANCING_ON"),
            new ShaderKeyword("SOFTPARTICLES_ON"),
            new ShaderKeyword("PIXELSNAP_ON"),
            new ShaderKeyword("SHADER_API_D3D11"),
            // if defined()
            new ShaderKeyword("SHADER_API_VULKAN"),
            new ShaderKeyword("UNITY_SINGLE_PASS_STEREO"),
            new ShaderKeyword("FOG_LINEAR"),
            new ShaderKeyword("FOG_EXP"),
            new ShaderKeyword("FOG_EXP2"),
            new ShaderKeyword("UNITY_PASS_DEFERRED"),
            new ShaderKeyword("LIGHTMAP_ON"),
            new ShaderKeyword("_PARALLAXMAP"),
            new ShaderKeyword("SHADOWS_SCREEN"),
        };
#endif
    }

    // Multiple callback may be implemented. 
    // The first one executed is the one where callbackOrder is returning the smallest number.
    public int callbackOrder { get { return 0; } }

    public void OnProcessShader(
        Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
    {
#if MANUAL_SHADER_STRIPPING
        // In development, don't strip debug variants
        if (EditorUserBuildSettings.development)
            return;

        for (int i = 0; i < shaderCompilerData.Count; ++i)
        {
            bool mustStrip = false;
            foreach (var kw in m_ExcludedKeywords)
            {
                if(shaderCompilerData[i].shaderKeywordSet.IsEnabled(kw))
                {
                    mustStrip = true;
                    break;
                }
            }

            if (mustStrip)
            {
                shaderCompilerData.RemoveAt(i);
                --i;
            }
        }
#endif
    }
}