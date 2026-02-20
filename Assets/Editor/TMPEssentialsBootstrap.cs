using UnityEngine;
using UnityEditor;
using TMPro;

namespace RuneRealm.Editor
{
    /// <summary>
    /// Automatically creates TMP essential resources on first editor load so the
    /// TMP_PackageResourceImporter window never fires (which crashes with a null
    /// path or fails in play mode).
    /// </summary>
    [InitializeOnLoad]
    public static class TMPEssentialsBootstrap
    {
        static TMPEssentialsBootstrap()
        {
            // Defer so AssetDatabase is fully ready
            EditorApplication.delayCall += EnsureTMPResources;
        }

        static void EnsureTMPResources()
        {
            // If settings already exist, nothing to do
            if (Resources.Load<TMP_Settings>("TMP Settings") != null)
                return;

            // Create the directory tree TMP expects
            if (!AssetDatabase.IsValidFolder("Assets/TextMeshPro"))
                AssetDatabase.CreateFolder("Assets", "TextMeshPro");
            if (!AssetDatabase.IsValidFolder("Assets/TextMeshPro/Resources"))
                AssetDatabase.CreateFolder("Assets/TextMeshPro", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/TextMeshPro/Resources/Fonts & Materials"))
                AssetDatabase.CreateFolder("Assets/TextMeshPro/Resources", "Fonts & Materials");
            if (!AssetDatabase.IsValidFolder("Assets/TextMeshPro/Resources/Sprite Assets"))
                AssetDatabase.CreateFolder("Assets/TextMeshPro/Resources", "Sprite Assets");
            if (!AssetDatabase.IsValidFolder("Assets/TextMeshPro/Resources/Style Sheets"))
                AssetDatabase.CreateFolder("Assets/TextMeshPro/Resources", "Style Sheets");

            // Create a default TMP Settings asset
            var settings = ScriptableObject.CreateInstance<TMP_Settings>();
            AssetDatabase.CreateAsset(settings, "Assets/TextMeshPro/Resources/TMP Settings.asset");
            AssetDatabase.SaveAssets();

            Debug.Log("[RuneRealm] TMP essential resources created. " +
                      "Restart the editor if you still see the TMP importer window.");
        }
    }
}
