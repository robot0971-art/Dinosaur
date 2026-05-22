using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DinoGrow.Core.Data;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DinoGrow.Infrastructure.Data
{
    public sealed class DinoTableMigrationService
    {
        public void Migrate(string xlsxPath)
        {
            if (string.IsNullOrWhiteSpace(xlsxPath))
            {
                throw new ArgumentException("Excel path is empty.", nameof(xlsxPath));
            }

            if (!File.Exists(xlsxPath))
            {
                throw new FileNotFoundException("Excel file not found.", xlsxPath);
            }

            using var stream = new FileStream(xlsxPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            var workbook = new XSSFWorkbook(stream);

            var dinoSheet = workbook.GetSheet("DinoTable");
            if (dinoSheet != null)
            {
                MigrateDinoTable(workbook, dinoSheet);
            }

            AddMaxLivesToPlayerTableIfMissing(workbook);
            AddItemTableIfMissing(workbook);

            stream.Close();
            using var output = new FileStream(xlsxPath, FileMode.Create, FileAccess.Write);
            workbook.Write(output);
        }

        private static void MigrateDinoTable(IWorkbook workbook, ISheet dinoSheet)
        {
            if (workbook.GetSheet("PlayerTable") != null || workbook.GetSheet("EnemyDinoTable") != null)
            {
                return;
            }

            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(dinoSheet, formatter);

            var playerRows = new List<IRow>();
            var enemyRows = new List<IRow>();

            for (var rowIndex = 1; rowIndex <= dinoSheet.LastRowNum; rowIndex++)
            {
                var row = dinoSheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var id = ReadString(row, headerMap, "id", formatter);
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (string.Equals(id, "player", StringComparison.OrdinalIgnoreCase))
                {
                    playerRows.Add(row);
                }
                else
                {
                    enemyRows.Add(row);
                }
            }

            CreatePlayerSheet(workbook, dinoSheet, formatter, headerMap, playerRows);
            CreateEnemyDinoSheet(workbook, dinoSheet, headerMap, enemyRows);

            var sheetIndex = workbook.GetSheetIndex(dinoSheet);
            workbook.RemoveSheetAt(sheetIndex);
        }

        private static void AddMaxLivesToPlayerTableIfMissing(IWorkbook workbook)
        {
            var sheet = workbook.GetSheet("PlayerTable");
            if (sheet == null)
            {
                return;
            }

            var headerRow = sheet.GetRow(0);
            if (headerRow == null)
            {
                return;
            }

            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            if (headerMap.ContainsKey("maxLives") || headerMap.ContainsKey("최대목숨"))
            {
                return;
            }

            var maxLivesCol = headerRow.LastCellNum;
            var headerCell = headerRow.CreateCell(maxLivesCol);
            headerCell.SetCellValue("최대목숨");
            var headerStyle = CreateHeaderStyle(workbook);
            headerCell.CellStyle = headerStyle;

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    continue;
                }

                row.CreateCell(maxLivesCol).SetCellValue(3);
            }

            sheet.AutoSizeColumn(maxLivesCol);
        }

        private static void AddItemTableIfMissing(IWorkbook workbook)
        {
            if (workbook.GetSheet("ItemTable") != null)
            {
                return;
            }

            var sheet = workbook.CreateSheet("ItemTable");
            var headerStyle = CreateHeaderStyle(workbook);
            var headerRow = sheet.CreateRow(0);

            var headers = new[] { "id", "표시이름", "효과타입", "효과값", "프리팹" };
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }

            var dataRow = sheet.CreateRow(1);
            dataRow.CreateCell(0).SetCellValue("heart");
            dataRow.CreateCell(1).SetCellValue("하트");
            dataRow.CreateCell(2).SetCellValue("Heart");
            dataRow.CreateCell(3).SetCellValue(1);
            dataRow.CreateCell(4).SetCellValue("HeartPickup");

            for (var i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }
        }

        private static void CreatePlayerSheet(IWorkbook workbook, ISheet sourceSheet, DataFormatter formatter, IReadOnlyDictionary<string, int> headerMap, List<IRow> rows)
        {
            var sheet = workbook.CreateSheet("PlayerTable");
            CopyHeaderRow(workbook, sheet, sourceSheet, headerMap, new[] { "id", "displayName", "level", "exp", "speed", "size", "prefab", "maxLives" });

            foreach (var sourceRow in rows)
            {
                var newRow = sheet.CreateRow(sheet.LastRowNum + 1);
                WriteCell(newRow, 0, sourceRow.GetCell(GetIndexSafe(headerMap, "id")));
                WriteCell(newRow, 1, sourceRow.GetCell(GetIndexSafe(headerMap, "displayName")));
                WriteCell(newRow, 2, sourceRow.GetCell(GetIndexSafe(headerMap, "level")));
                WriteCell(newRow, 3, sourceRow.GetCell(GetIndexSafe(headerMap, "exp")));
                WriteCell(newRow, 4, sourceRow.GetCell(GetIndexSafe(headerMap, "speed")));
                WriteCell(newRow, 5, sourceRow.GetCell(GetIndexSafe(headerMap, "size")));
                WriteCell(newRow, 6, sourceRow.GetCell(GetIndexSafe(headerMap, "prefab")));
                WriteCell(newRow, 7, CreateCellWithValue(newRow, 3));
            }

            AutoSizeColumns(sheet, 8);
        }

        private static void CreateEnemyDinoSheet(IWorkbook workbook, ISheet sourceSheet, IReadOnlyDictionary<string, int> headerMap, List<IRow> rows)
        {
            var sheet = workbook.CreateSheet("EnemyDinoTable");
            CopyHeaderRow(workbook, sheet, sourceSheet, headerMap, new[] { "id", "displayName", "level", "exp", "speed", "size", "aiType", "colorType", "prefab" });

            foreach (var sourceRow in rows)
            {
                var newRow = sheet.CreateRow(sheet.LastRowNum + 1);
                WriteCell(newRow, 0, sourceRow.GetCell(GetIndexSafe(headerMap, "id")));
                WriteCell(newRow, 1, sourceRow.GetCell(GetIndexSafe(headerMap, "displayName")));
                WriteCell(newRow, 2, sourceRow.GetCell(GetIndexSafe(headerMap, "level")));
                WriteCell(newRow, 3, sourceRow.GetCell(GetIndexSafe(headerMap, "exp")));
                WriteCell(newRow, 4, sourceRow.GetCell(GetIndexSafe(headerMap, "speed")));
                WriteCell(newRow, 5, sourceRow.GetCell(GetIndexSafe(headerMap, "size")));
                WriteCell(newRow, 6, sourceRow.GetCell(GetIndexSafe(headerMap, "aiType")));
                WriteCell(newRow, 7, sourceRow.GetCell(GetIndexSafe(headerMap, "colorType")));
                WriteCell(newRow, 8, sourceRow.GetCell(GetIndexSafe(headerMap, "prefab")));
            }

            AutoSizeColumns(sheet, 9);
        }

        private static ICell CreateCellWithValue(IRow row, int numericValue)
        {
            var cell = row.CreateCell(7);
            cell.SetCellValue(numericValue);
            return cell;
        }

        private static void CopyHeaderRow(IWorkbook workbook, ISheet targetSheet, ISheet sourceSheet, IReadOnlyDictionary<string, int> headerMap, string[] selectedHeaders)
        {
            var sourceHeaderRow = sourceSheet.GetRow(0);
            var headerStyle = CreateHeaderStyle(workbook);
            var targetRow = targetSheet.CreateRow(0);

            for (var i = 0; i < selectedHeaders.Length; i++)
            {
                var cell = targetRow.CreateCell(i);
                if (sourceHeaderRow != null && headerMap.TryGetValue(selectedHeaders[i], out var sourceIndex) && sourceHeaderRow.GetCell(sourceIndex) != null)
                {
                    cell.SetCellValue(sourceHeaderRow.GetCell(sourceIndex).StringCellValue);
                }
                else
                {
                    cell.SetCellValue(selectedHeaders[i]);
                }
                cell.CellStyle = headerStyle;
            }
        }

        private static void WriteCell(IRow targetRow, int targetIndex, ICell sourceCell)
        {
            if (sourceCell == null)
            {
                return;
            }

            var newCell = targetRow.CreateCell(targetIndex);
            switch (sourceCell.CellType)
            {
                case CellType.String:
                    newCell.SetCellValue(sourceCell.StringCellValue);
                    break;
                case CellType.Numeric:
                    newCell.SetCellValue(sourceCell.NumericCellValue);
                    break;
                case CellType.Boolean:
                    newCell.SetCellValue(sourceCell.BooleanCellValue);
                    break;
                case CellType.Formula:
                    newCell.SetCellValue(sourceCell.CellFormula);
                    break;
            }
        }

        private static int GetIndexSafe(IReadOnlyDictionary<string, int> headerMap, string key)
        {
            return headerMap.TryGetValue(key, out var index) ? index : 0;
        }

        private static Dictionary<string, int> ReadHeaderMap(ISheet sheet, DataFormatter formatter)
        {
            var headerRow = sheet.GetRow(0) ?? throw new InvalidDataException("Table needs a header row.");
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headerRow.LastCellNum; i++)
            {
                var header = formatter.FormatCellValue(headerRow.GetCell(i)).Trim();
                if (string.IsNullOrEmpty(header) || map.ContainsKey(header))
                {
                    continue;
                }

                map.Add(header, i);
                foreach (var alias in GetHeaderAliases(header))
                {
                    if (!map.ContainsKey(alias))
                    {
                        map.Add(alias, i);
                    }
                }
            }

            return map;
        }

        private static IReadOnlyList<string> GetHeaderAliases(string header)
        {
            return header switch
            {
                "표시이름" => new[] { "displayName" },
                "레벨" => new[] { "level" },
                "경험치" => new[] { "exp" },
                "이동속도" => new[] { "speed" },
                "크기" => new[] { "size" },
                "AI유형" => new[] { "aiType" },
                "색상유형" => new[] { "colorType" },
                "프리팹" => new[] { "prefab" },
                _ => Array.Empty<string>()
            };
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

        private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            var font = workbook.CreateFont();
            font.IsBold = true;
            style.SetFont(font);
            return style;
        }

        private static void AutoSizeColumns(ISheet sheet, int columnCount)
        {
            for (var i = 0; i < columnCount; i++)
            {
                sheet.AutoSizeColumn(i);
            }
        }
    }
}
