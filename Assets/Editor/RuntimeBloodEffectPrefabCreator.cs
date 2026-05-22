using System.IO;
using UnityEditor;
using UnityEngine;

public static class RuntimeBloodEffectPrefabCreator
{
    private const string EffectFolder = "Assets/Prefabs/Effects";
    private const string TexturePath = EffectFolder + "/RuntimeBloodCircle.png";
    private const string MaterialPath = EffectFolder + "/RuntimeBloodParticle.mat";
    private const string PrefabPath = EffectFolder + "/RuntimeBloodEffect.prefab";

    [MenuItem("Tools/Dino Game/Effects/Create Runtime Blood Prefab")]
    private static void CreateRuntimeBloodPrefab()
    {
        EnsureEffectFolder();

        var texture = CreateOrUpdateCircleTexture();
        var material = CreateOrUpdateMaterial(texture);
        var effectObject = new GameObject("RuntimeBloodEffect");
        var particleSystem = effectObject.AddComponent<ParticleSystem>();
        ConfigureParticleSystem(particleSystem);

        var renderer = effectObject.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 1f;

        PrefabUtility.SaveAsPrefabAsset(effectObject, PrefabPath);
        Object.DestroyImmediate(effectObject);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Debug.Log($"Created runtime blood prefab at {PrefabPath}.");
    }

    private static void EnsureEffectFolder()
    {
        if (AssetDatabase.IsValidFolder(EffectFolder))
        {
            return;
        }

        Directory.CreateDirectory(EffectFolder);
        AssetDatabase.Refresh();
    }

    private static Texture2D CreateOrUpdateCircleTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        var radius = size * 0.46f;
        var feather = size * 0.08f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), center);
                var alpha = Mathf.Clamp01((radius - distance) / feather);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(TexturePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
    }

    private static Material CreateOrUpdateMaterial(Texture2D texture)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(GetParticleShader());
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.shader = GetParticleShader();
        material.name = "RuntimeBloodParticle";
        SetMaterialColor(material, new Color(0.85f, 0.05f, 0.03f, 0.85f));
        SetMaterialTexture(material, texture);
        ConfigureTransparentParticleMaterial(material);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader GetParticleShader()
    {
        return Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Standard");
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void SetMaterialTexture(Material material, Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }
        else if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }
    }

    private static void ConfigureTransparentParticleMaterial(Material material)
    {
        SetFloatIfPresent(material, "_Surface", 1f);
        SetFloatIfPresent(material, "_Blend", 0f);
        SetFloatIfPresent(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        SetFloatIfPresent(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(material, "_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        SetFloatIfPresent(material, "_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(material, "_ZWrite", 0f);
        SetFloatIfPresent(material, "_AlphaClip", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void ConfigureParticleSystem(ParticleSystem effect)
    {
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = effect.main;
        main.duration = 0.45f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.02f, 0.01f, 0.95f),
            new Color(0.35f, 0f, 0f, 0.75f));
        main.gravityModifier = 0.9f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = effect.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 22, 34)
        });

        var shape = effect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.22f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colorOverLifetime = effect.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.85f, 0.02f, 0.01f), 0f),
                new GradientColorKey(new Color(0.35f, 0f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;
    }
}
