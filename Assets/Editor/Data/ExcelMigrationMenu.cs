using System.IO;
using System;
using DinoGrow.Infrastructure.Data;
using UnityEditor;
using UnityEngine;

public static class ExcelMigrationMenu
{
    [MenuItem("Tools/Dino Game/Data/Migrate DinoTable to PlayerTable + EnemyDinoTable")]
    public static void MigrateDinoTable()
    {
        var excelPath = EditorUtility.OpenFilePanel("Select Game Data Excel to Migrate", "Assets/GameData/Excel", "xlsx");
        if (string.IsNullOrEmpty(excelPath))
        {
            return;
        }

        try
        {
            var migrationService = new DinoTableMigrationService();
            migrationService.Migrate(excelPath);
            AssetDatabase.Refresh();
            Debug.Log($"Migration complete: DinoTable split into PlayerTable and EnemyDinoTable in {excelPath}");

            if (EditorUtility.DisplayDialog(
                "Migration Complete",
                "DinoTable has been split into PlayerTable and EnemyDinoTable.\n\nNow run Tools/Dino Game/Data/Convert Game Data Excel To ScriptableObjects to regenerate the asset files.",
                "OK"))
            {
                EditorApplication.delayCall += () =>
                {
                    ExcelDataMenu.ConvertGameDataExcelToScriptableObjects();
                };
            }
        }
        catch (FileNotFoundException)
        {
            ShowError("The selected Excel file was not found.");
        }
        catch (InvalidDataException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Migration Failed", ex.Message, "OK");
        }
    }

    private static void ShowError(string message)
    {
        Debug.LogError(message);
        EditorUtility.DisplayDialog("Migration Failed", message, "OK");
    }
}
