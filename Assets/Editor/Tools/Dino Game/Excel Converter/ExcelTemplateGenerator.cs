using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dino.Editor.Tools
{
    public static class ExcelTemplateGenerator
    {
        private const string BasePath = "Assets/GameData/Excel";

        [MenuItem("Tools/Dino Game/Excel Converter/Create DinoTable Template")]
        public static void CreateDinoTableTemplate()
        {
            var template = new DinoTableTemplate();
            CreateTemplate("DinoTable.xlsx", "DinoData", template);
        }

        [MenuItem("Tools/Dino Game/Excel Converter/Create GrowthTable Template")]
        public static void CreateGrowthTableTemplate()
        {
            var template = new GrowthTableTemplate();
            CreateTemplate("GrowthTable.xlsx", "GrowthData", template);
        }

        [MenuItem("Tools/Dino Game/Excel Converter/Create SpawnTable Template")]
        public static void CreateSpawnTableTemplate()
        {
            var template = new SpawnTableTemplate();
            CreateTemplate("SpawnTable.xlsx", "SpawnData", template);
        }

        [MenuItem("Tools/Dino Game/Excel Converter/Create StageTable Template")]
        public static void CreateStageTableTemplate()
        {
            var template = new StageTableTemplate();
            CreateTemplate("StageTable.xlsx", "StageData", template);
        }

        [MenuItem("Tools/Dino Game/Excel Converter/Create All Templates")]
        public static void CreateAllTemplates()
        {
            CreateDinoTableTemplate();
            CreateGrowthTableTemplate();
            CreateSpawnTableTemplate();
            CreateStageTableTemplate();
            AssetDatabase.Refresh();
            Debug.Log("All Excel templates created successfully!");
        }

        private static void CreateTemplate<T>(string fileName, string sheetName, T template) where T : class
        {
            var filePath = Path.Combine(BasePath, fileName);
            var dir = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
                var sheet = workbook.CreateSheet(sheetName);

                var properties = typeof(T).GetProperties();
                var headerRow = sheet.CreateRow(0);

                for (int i = 0; i < properties.Length; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(properties[i].Name);

                    var cellStyle = workbook.CreateCellStyle();
                    var font = workbook.CreateFont();
                    font.IsBold = true;
                    cellStyle.SetFont(font);
                    cell.CellStyle = cellStyle;

                    sheet.AutoSizeColumn(i);
                }

                var exampleRow = sheet.CreateRow(1);
                for (int i = 0; i < properties.Length; i++)
                {
                    var propType = properties[i].PropertyType;
                    var cell = exampleRow.CreateCell(i);

                    if (propType == typeof(int))
                        cell.SetCellValue(0);
                    else if (propType == typeof(float))
                        cell.SetCellValue(0.0);
                    else if (propType == typeof(bool))
                        cell.SetCellValue(false);
                    else if (propType == typeof(string))
                        cell.SetCellValue($"Example {properties[i].Name}");
                    else
                        cell.SetCellValue("");
                }

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(stream);
                }

                Debug.Log($"Template created: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating template {fileName}: {e.Message}");
            }
        }
    }

    public class DinoTableTemplate
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public float MoveSpeed { get; set; }
        public float Scale { get; set; }
        public int ExpReward { get; set; }
        public string PrefabPath { get; set; }
        public string Description { get; set; }
    }

    public class GrowthTableTemplate
    {
        public int Level { get; set; }
        public int RequiredExp { get; set; }
        public float ScaleMultiplier { get; set; }
        public float MoveSpeed { get; set; }
        public float CameraDistance { get; set; }
        public float CameraHeight { get; set; }
    }

    public class SpawnTableTemplate
    {
        public int ID { get; set; }
        public int StageID { get; set; }
        public int DinoID { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public float SpawnWeight { get; set; }
        public float SpawnRadius { get; set; }
    }

    public class StageTableTemplate
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int MinPlayerLevel { get; set; }
        public int MaxPlayerLevel { get; set; }
        public float MapSize { get; set; }
        public string SceneName { get; set; }
    }
}