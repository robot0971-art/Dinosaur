using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DinoGrow.Core.Data;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DinoGrow.Infrastructure.Data
{
    public sealed class ExcelDataService : IDataService
    {
        private static readonly string[] DinoHeaders =
        {
            "id",
            "displayName",
            "level",
            "exp",
            "speed",
            "size",
            "aiType",
            "colorType",
            "prefab"
        };

        public IReadOnlyList<DinoDataRecord> LoadDinoRows(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath))
            {
                throw new ArgumentException("Excel path is empty.", nameof(xlsxPath));
            }

            if (!File.Exists(xlsxPath))
            {
                throw new FileNotFoundException("Excel file not found.", xlsxPath);
            }

            using var stream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var workbook = new XSSFWorkbook(stream);
            var sheet = workbook.GetSheet("DinoTable") ?? workbook.GetSheetAt(0);
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<DinoDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new DinoDataRecord
                {
                    id = ReadString(row, headerMap, "id", formatter),
                    displayName = ReadString(row, headerMap, "displayName", formatter),
                    level = ReadInt(row, headerMap, "level", formatter, 1),
                    exp = ReadInt(row, headerMap, "exp", formatter, 10),
                    speed = ReadFloat(row, headerMap, "speed", formatter, 1f),
                    size = ReadFloat(row, headerMap, "size", formatter, 1f),
                    aiType = ReadString(row, headerMap, "aiType", formatter),
                    colorType = ReadString(row, headerMap, "colorType", formatter),
                    prefab = ReadString(row, headerMap, "prefab", formatter)
                };

                if (!string.IsNullOrWhiteSpace(record.id))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public void CreateDinoTemplate(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath))
            {
                throw new ArgumentException("Excel path is empty.", nameof(xlsxPath));
            }

            var directory = Path.GetDirectoryName(xlsxPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("DinoTable");
            var headerStyle = CreateHeaderStyle(workbook);
            var headerRow = sheet.CreateRow(0);

            for (var i = 0; i < DinoHeaders.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(DinoHeaders[i]);
                cell.CellStyle = headerStyle;
            }

            WriteSampleRow(sheet.CreateRow(1), "dino_001", "Egg", 1, 10, 0f, 0.8f, "Idle", "Food", "EggPrefab");
            WriteSampleRow(sheet.CreateRow(2), "dino_002", "Small Raptor", 2, 20, 2.5f, 1.0f, "Wander", "Danger", "SmallRaptorPrefab");
            WriteSampleRow(sheet.CreateRow(3), "dino_003", "Young Raptor", 3, 30, 3.2f, 1.2f, "Wander", "Danger", "YoungRaptorPrefab");

            for (var i = 0; i < DinoHeaders.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            using var output = new FileStream(xlsxPath, FileMode.Create, FileAccess.Write);
            workbook.Write(output);
        }

        private static Dictionary<string, int> ReadHeaderMap(ISheet sheet, DataFormatter formatter)
        {
            var headerRow = sheet.GetRow(0) ?? throw new InvalidDataException("DinoTable needs a header row.");
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headerRow.LastCellNum; i++)
            {
                var header = formatter.FormatCellValue(headerRow.GetCell(i)).Trim();
                if (!string.IsNullOrEmpty(header) && !map.ContainsKey(header))
                {
                    map.Add(header, i);
                }
            }

            return map;
        }

        private static bool IsRowEmpty(IRow row, DataFormatter formatter)
        {
            for (var i = row.FirstCellNum; i < row.LastCellNum; i++)
            {
                if (!string.IsNullOrWhiteSpace(formatter.FormatCellValue(row.GetCell(i))))
                {
                    return false;
                }
            }

            return true;
        }

        private static string ReadString(IRow row, IReadOnlyDictionary<string, int> headerMap, string header, DataFormatter formatter)
        {
            return headerMap.TryGetValue(header, out var index)
                ? formatter.FormatCellValue(row.GetCell(index)).Trim()
                : "";
        }

        private static int ReadInt(IRow row, IReadOnlyDictionary<string, int> headerMap, string header, DataFormatter formatter, int fallback)
        {
            var value = ReadString(row, headerMap, header, formatter);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static float ReadFloat(IRow row, IReadOnlyDictionary<string, int> headerMap, string header, DataFormatter formatter, float fallback)
        {
            var value = ReadString(row, headerMap, header, formatter);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.IsBold = true;
            style.SetFont(font);
            return style;
        }

        private static void WriteSampleRow(IRow row, string id, string displayName, int level, int exp, float speed, float size, string aiType, string colorType, string prefab)
        {
            row.CreateCell(0).SetCellValue(id);
            row.CreateCell(1).SetCellValue(displayName);
            row.CreateCell(2).SetCellValue(level);
            row.CreateCell(3).SetCellValue(exp);
            row.CreateCell(4).SetCellValue(speed);
            row.CreateCell(5).SetCellValue(size);
            row.CreateCell(6).SetCellValue(aiType);
            row.CreateCell(7).SetCellValue(colorType);
            row.CreateCell(8).SetCellValue(prefab);
        }
    }
}
