using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class IslandRuntimeMaterialBuilder
{
    private const string FolderPath = "Assets/Resources/RuntimeMaterials";

    static IslandRuntimeMaterialBuilder()
    {
        EditorApplication.delayCall += EnsureMaterials;
    }

    [MenuItem("Island/Ensure Runtime Materials")]
    public static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(FolderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "RuntimeMaterials");
        }

        EnsureMaterial("IslandOpaque.mat", FindShader("Legacy Shaders/Diffuse", "Standard", "Sprites/Default"), false);
        EnsureMaterial("IslandTransparent.mat", FindShader("Legacy Shaders/Transparent/Diffuse", "Legacy Shaders/Diffuse", "Standard", "Sprites/Default"), true);
        EnsureMaterial("IslandTerrain.mat", FindShader("Nature/Terrain/Standard", "Hidden/TerrainEngine/Splatmap/Standard-Base", "Legacy Shaders/Diffuse", "Standard"), false);

        AssetDatabase.SaveAssets();
    }

    private static void EnsureMaterial(string fileName, Shader shader, bool transparent)
    {
        if (shader == null)
        {
            Debug.LogWarning($"Could not create {fileName}: no compatible shader found.");
            return;
        }

        string path = Path.Combine(FolderPath, fileName).Replace('\\', '/');
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.name = Path.GetFileNameWithoutExtension(fileName);

        if (transparent)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
        }

        EditorUtility.SetDirty(material);
    }

    private static Shader FindShader(params string[] names)
    {
        foreach (string shaderName in names)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                return shader;
            }
        }

        return null;
    }
}
