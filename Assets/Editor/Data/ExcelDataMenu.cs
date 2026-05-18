using System.IO;
using DinoGrow.Core.Data;
using DinoGrow.Infrastructure.Data;
using UnityEditor;
using UnityEngine;

public static class ExcelDataMenu
{
    private const string DefaultExcelFolder = "Assets/GameData/Excel";
    private const string DefaultGeneratedFolder = "Assets/GameData/Generated";

    [MenuItem("Tools/Dino Game/Data/Create Dino Excel Template")]
    public static void CreateDinoExcelTemplate()
    {
        var defaultPath = Path.Combine(DefaultExcelFolder, "DinoTable.xlsx");
        var path = EditorUtility.SaveFilePanel("Create Dino Excel Template", DefaultExcelFolder, "DinoTable", "xlsx");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var dataService = new ExcelDataService();
        dataService.CreateDinoTemplate(path);
        AssetDatabase.Refresh();

        Debug.Log($"Dino Excel template created: {ToAssetPath(path) ?? path}");
    }

    [MenuItem("Tools/Dino Game/Data/Convert Dino Excel To ScriptableObject")]
    public static void ConvertDinoExcelToScriptableObject()
    {
        var excelPath = EditorUtility.OpenFilePanel("Select Dino Excel", DefaultExcelFolder, "xlsx");
        if (string.IsNullOrEmpty(excelPath))
        {
            return;
        }

        Directory.CreateDirectory(DefaultGeneratedFolder);

        var assetPath = EditorUtility.SaveFilePanelInProject(
            "Save Dino Database",
            "DinoDatabase",
            "asset",
            "Choose where to save the generated DinoDatabase.",
            DefaultGeneratedFolder);

        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        var dataService = new ExcelDataService();
        var records = dataService.LoadDinoRows(excelPath);
        var database = ScriptableObject.CreateInstance<DinoDatabase>();
        database.SetRecords(records);

        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(database, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Converted {records.Count} dino rows to {assetPath}");
    }

    private static string ToAssetPath(string absolutePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (projectRoot == null)
        {
            return null;
        }

        var normalizedRoot = projectRoot.Replace('\\', '/').TrimEnd('/');
        var normalizedPath = absolutePath.Replace('\\', '/');
        return normalizedPath.StartsWith(normalizedRoot)
            ? normalizedPath.Substring(normalizedRoot.Length + 1)
            : null;
    }
}
