using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace RuneRealm.Editor
{
    /// <summary>
    /// Automatically creates and assigns a URP pipeline asset when the project
    /// has the URP package installed but no pipeline configured.
    /// Runs once on editor domain reload.
    /// </summary>
    [InitializeOnLoad]
    public static class URPSetup
    {
        static URPSetup()
        {
            EditorApplication.delayCall += EnsureURPPipeline;
        }

        static void EnsureURPPipeline()
        {
            // Already configured — nothing to do
            if (GraphicsSettings.defaultRenderPipeline != null) return;

            // Check if URP assembly is available
            var urpAssetType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            var rendererDataType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalRendererData, Unity.RenderPipelines.Universal.Runtime");

            if (urpAssetType == null || rendererDataType == null)
            {
                Debug.Log("[URPSetup] URP types not found, skipping pipeline setup.");
                return;
            }

            // Ensure Settings folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            // Check if assets already exist on disk
            var existingPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/URP-Pipeline.asset");
            if (existingPipeline != null)
            {
                GraphicsSettings.defaultRenderPipeline = existingPipeline;
                SetAllQualityLevels(existingPipeline);
                Debug.Log("[URPSetup] Found existing URP pipeline asset, assigned to graphics settings.");
                return;
            }

            // Create renderer data
            var rendererData = ScriptableObject.CreateInstance(rendererDataType);
            AssetDatabase.CreateAsset(rendererData, "Assets/Settings/URP-Renderer.asset");

            // Create pipeline asset via Create(ScriptableRendererData) or fallback
            RenderPipelineAsset pipelineAsset = null;
            var createMethod = urpAssetType.GetMethod("Create",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null, new[] { rendererDataType.BaseType ?? rendererDataType }, null);

            if (createMethod == null)
            {
                // Try parameterless Create
                createMethod = urpAssetType.GetMethod("Create",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, System.Type.EmptyTypes, null);
            }

            if (createMethod != null)
            {
                var args = createMethod.GetParameters().Length > 0
                    ? new object[] { rendererData }
                    : null;
                pipelineAsset = createMethod.Invoke(null, args) as RenderPipelineAsset;
            }

            if (pipelineAsset == null)
            {
                // Last fallback: CreateInstance
                pipelineAsset = ScriptableObject.CreateInstance(urpAssetType) as RenderPipelineAsset;
            }

            if (pipelineAsset == null)
            {
                Debug.LogWarning("[URPSetup] Failed to create URP pipeline asset.");
                return;
            }

            AssetDatabase.CreateAsset(pipelineAsset, "Assets/Settings/URP-Pipeline.asset");
            AssetDatabase.SaveAssets();

            // Assign to project
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            SetAllQualityLevels(pipelineAsset);

            Debug.Log("[URPSetup] Created URP pipeline asset and assigned to all quality levels.");
        }

        static void SetAllQualityLevels(RenderPipelineAsset asset)
        {
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
        }
    }
}
