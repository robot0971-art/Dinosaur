using UnityEditor;
using UnityEngine;

public static class DinoShaderRepairUtility
{
    private static readonly string[] ImportedMapRoots =
    {
        "Assets/IslandMap",
        "Assets/Pure Poly"
    };

    private static readonly string[] VfxRoots =
    {
        "Assets/Vefects"
    };

    [MenuItem("Tools/Dino Game/Rendering/Repair Imported Map Materials")]
    private static void RepairImportedMapMaterials()
    {
        var repairedCount = RepairMaterialsInRoots(ImportedMapRoots, "Universal Render Pipeline/Lit");
        Debug.Log($"Repaired {repairedCount} imported map materials.");
    }

    [MenuItem("Tools/Dino Game/Rendering/Repair Blood VFX Materials")]
    private static void RepairBloodVfxMaterials()
    {
        var repairedCount = RepairMaterialsInRoots(VfxRoots, "Universal Render Pipeline/Particles/Unlit");
        Debug.Log($"Repaired {repairedCount} blood VFX materials.");
    }

    [MenuItem("Tools/Dino Game/Rendering/Repair Selected Materials")]
    private static void RepairSelectedMaterials()
    {
        var repairedCount = 0;
        foreach (var selectedObject in Selection.objects)
        {
            if (selectedObject is Material material && RepairMaterial(material, "Universal Render Pipeline/Lit"))
            {
                repairedCount++;
            }
        }

        Debug.Log($"Repaired {repairedCount} selected materials.");
    }

    private static int RepairMaterialsInRoots(string[] roots, string shaderName)
    {
        var shader = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("No compatible fallback shader was found.");
            return 0;
        }

        var repairedCount = 0;
        var materialGuids = AssetDatabase.FindAssets("t:Material", roots);
        foreach (var guid in materialGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null && RepairMaterial(material, shader))
            {
                repairedCount++;
            }
        }

        if (repairedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        return repairedCount;
    }

    private static bool RepairMaterial(Material material, string shaderName)
    {
        var shader = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return shader != null && RepairMaterial(material, shader);
    }

    private static bool RepairMaterial(Material material, Shader shader)
    {
        if (material.shader == shader)
        {
            return false;
        }

        var color = Color.white;
        if (material.HasProperty("_BaseColor"))
        {
            color = material.GetColor("_BaseColor");
        }
        else if (material.HasProperty("_Color"))
        {
            color = material.GetColor("_Color");
        }

        Undo.RecordObject(material, "Repair Material Shader");
        material.shader = shader;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        EditorUtility.SetDirty(material);
        return true;
    }
}
