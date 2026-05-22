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
            "\ud45c\uc2dc\uc774\ub984",
            "\ub808\ubca8",
            "\uacbd\ud5d8\uce58",
            "\uc774\ub3d9\uc18d\ub3c4",
            "\ud06c\uae30",
            "AI\uc720\ud615",
            "\uc0c9\uc0c1\uc720\ud615",
            "\ud504\ub9ac\ud339"
        };

        private static readonly string[] StageHeaders =
        {
            "stageId",
            "표시이름",
            "스폰중심X",
            "스폰중심Z",
            "스폰범위X",
            "스폰범위Z",
            "스폰높이Y",
            "플레이어최소거리",
            "제한시간"
        };

        private static readonly string[] SpawnHeaders =
        {
            "stageId",
            "dinoId",
            "\ucd5c\uc18c\ub808\ubca8",
            "\ucd5c\ub300\ub808\ubca8",
            "\uc0dd\uc131\uc218",
            "\ucc98\uce58\uacbd\ud5d8\uce58",
            "\uac00\uc911\uce58",
            "\ucd5c\uc18c\ubc30\ud68c\uc18d\ub3c4",
            "\ucd5c\ub300\ubc30\ud68c\uc18d\ub3c4"
        };

        private static readonly string[] PlayerGrowthHeaders =
        {
            "\ub808\ubca8",
            "\ud544\uc694\uacbd\ud5d8\uce58",
            "\ud06c\uae30\ubc30\uc728",
            "\uce74\uba54\ub77c\uac70\ub9ac",
            "\uce74\uba54\ub77c\ub192\uc774"
        };

        public IReadOnlyList<DinoDataRecord> LoadDinoRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
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

        public IReadOnlyList<StageDataRecord> LoadStageRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
            var sheet = workbook.GetSheet("StageTable") ?? throw new InvalidDataException("StageTable sheet was not found.");
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<StageDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new StageDataRecord
                {
                    stageId = ReadInt(row, headerMap, "stageId", formatter, 1),
                    displayName = ReadString(row, headerMap, "displayName", formatter),
                    spawnCenterX = ReadFloat(row, headerMap, "spawnCenterX", formatter, 0f),
                    spawnCenterZ = ReadFloat(row, headerMap, "spawnCenterZ", formatter, 0f),
                    spawnSizeX = ReadFloat(row, headerMap, "spawnSizeX", formatter, 80f),
                    spawnSizeZ = ReadFloat(row, headerMap, "spawnSizeZ", formatter, 80f),
                    spawnY = ReadFloat(row, headerMap, "spawnY", formatter, 0.75f),
                    minDistanceFromPlayer = ReadFloat(row, headerMap, "minDistanceFromPlayer", formatter, 8f),
                    timeLimit = ReadFloat(row, headerMap, "timeLimit", formatter, 0f)
                };

                if (record.stageId > 0)
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public IReadOnlyList<SpawnDataRecord> LoadSpawnRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
            var sheet = workbook.GetSheet("SpawnTable") ?? throw new InvalidDataException("SpawnTable sheet was not found.");
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<SpawnDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new SpawnDataRecord
                {
                    stageId = ReadInt(row, headerMap, "stageId", formatter, 1),
                    dinoId = ReadString(row, headerMap, "dinoId", formatter),
                    minLevel = ReadInt(row, headerMap, "minLevel", formatter, 1),
                    maxLevel = ReadInt(row, headerMap, "maxLevel", formatter, 1),
                    count = ReadInt(row, headerMap, "count", formatter, 1),
                    weight = ReadInt(row, headerMap, "weight", formatter, 1),
                    minWanderSpeed = ReadFloat(row, headerMap, "minWanderSpeed", formatter, 2.4f),
                    maxWanderSpeed = ReadFloat(row, headerMap, "maxWanderSpeed", formatter, 4.2f)
                };
                record.defeatExp = ReadInt(row, headerMap, "defeatExp", formatter, Math.Max(1, record.maxLevel) * 10);

                if (record.stageId > 0 && !string.IsNullOrWhiteSpace(record.dinoId))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public IReadOnlyList<PlayerGrowthDataRecord> LoadPlayerGrowthRows(string xlsxPath)
        {
            using var workbook = OpenWorkbook(xlsxPath);
            var sheet = workbook.GetSheet("PlayerGrowthTable") ?? throw new InvalidDataException("PlayerGrowthTable sheet was not found.");
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var headerMap = ReadHeaderMap(sheet, formatter);
            var records = new List<PlayerGrowthDataRecord>();

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row, formatter))
                {
                    continue;
                }

                var record = new PlayerGrowthDataRecord
                {
                    level = ReadInt(row, headerMap, "level", formatter, 1),
                    requiredExp = ReadInt(row, headerMap, "requiredExp", formatter, 50),
                    scaleMultiplier = ReadFloat(row, headerMap, "scaleMultiplier", formatter, 1f),
                    cameraDistance = ReadFloat(row, headerMap, "cameraDistance", formatter, 6f),
                    cameraHeight = ReadFloat(row, headerMap, "cameraHeight", formatter, 4f)
                };

                if (record.level > 0)
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public void CreateDinoTemplate(string xlsxPath)
        {
            EnsureOutputDirectory(xlsxPath);

            var workbook = new XSSFWorkbook();
            CreateDinoSheet(workbook);
            WriteWorkbook(workbook, xlsxPath);
        }

        public void CreateGameDataTemplate(string xlsxPath)
        {
            EnsureOutputDirectory(xlsxPath);

            var workbook = new XSSFWorkbook();
            CreateDinoSheet(workbook);
            CreateStageSheet(workbook);
            CreateSpawnSheet(workbook);
            CreatePlayerGrowthSheet(workbook);
            WriteWorkbook(workbook, xlsxPath);
        }

        private static XSSFWorkbook OpenWorkbook(string xlsxPath)
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
            return new XSSFWorkbook(stream);
        }

        private static void EnsureOutputDirectory(string xlsxPath)
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
        }

        private static void WriteWorkbook(IWorkbook workbook, string xlsxPath)
        {
            using var output = new FileStream(xlsxPath, FileMode.Create, FileAccess.Write);
            workbook.Write(output);
        }

        private static void CreateDinoSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("DinoTable");
            WriteHeaders(workbook, sheet, DinoHeaders);
            WriteDinoSampleRow(sheet.CreateRow(1), "player", "\ud50c\ub808\uc774\uc5b4", 1, 0, 5f, 1f, "\ud50c\ub808\uc774\uc5b4", "\ud50c\ub808\uc774\uc5b4", "Player");
            AutoSizeColumns(sheet, DinoHeaders.Length);
        }

        private static void CreateStageSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("StageTable");
            WriteHeaders(workbook, sheet, StageHeaders);
            WriteStageSampleRow(sheet.CreateRow(1), 1, "\ucd08\uc6d0", 0f, 0f, 80f, 80f, 0.75f, 8f, 0f);
            WriteStageSampleRow(sheet.CreateRow(2), 2, "\ub113\uc740 \ucd08\uc6d0", 0f, 0f, 100f, 100f, 0.75f, 10f, 0f);
            AutoSizeColumns(sheet, StageHeaders.Length);
        }

        private static void CreateSpawnSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("SpawnTable");
            WriteHeaders(workbook, sheet, SpawnHeaders);
            WriteSpawnSampleRow(sheet.CreateRow(1), 1, "dino_001", 1, 1, 10, 10, 60, 0f, 0f);
            WriteSpawnSampleRow(sheet.CreateRow(2), 1, "dino_002", 1, 2, 8, 20, 30, 2.4f, 3.4f);
            WriteSpawnSampleRow(sheet.CreateRow(3), 1, "dino_003", 2, 3, 6, 30, 10, 3.0f, 4.2f);
            WriteSpawnSampleRow(sheet.CreateRow(4), 2, "dino_002", 2, 4, 12, 40, 50, 2.6f, 3.8f);
            WriteSpawnSampleRow(sheet.CreateRow(5), 2, "dino_003", 3, 5, 10, 50, 50, 3.2f, 4.4f);
            AutoSizeColumns(sheet, SpawnHeaders.Length);
        }

        private static void CreatePlayerGrowthSheet(IWorkbook workbook)
        {
            var sheet = workbook.CreateSheet("PlayerGrowthTable");
            WriteHeaders(workbook, sheet, PlayerGrowthHeaders);

            for (var level = 1; level <= 20; level++)
            {
                var row = sheet.CreateRow(level);
                var scale = 1f + (level - 1) * 0.08f;
                var cameraDistance = GetCameraDistance(level);
                var cameraHeight = GetCameraHeight(level);
                WritePlayerGrowthSampleRow(row, level, 50, scale, cameraDistance, cameraHeight);
            }

            AutoSizeColumns(sheet, PlayerGrowthHeaders.Length);
        }

        private static float GetCameraDistance(int level)
        {
            if (level <= 4)
            {
                return 6f;
            }

            if (level <= 8)
            {
                return 7f;
            }

            if (level <= 12)
            {
                return 8f;
            }

            if (level <= 16)
            {
                return 9f;
            }

            return 10f;
        }

        private static float GetCameraHeight(int level)
        {
            if (level <= 4)
            {
                return 4f;
            }

            if (level <= 8)
            {
                return 4.5f;
            }

            if (level <= 12)
            {
                return 5f;
            }

            if (level <= 16)
            {
                return 6f;
            }

            return 7f;
        }

        private static void WriteHeaders(IWorkbook workbook, ISheet sheet, IReadOnlyList<string> headers)
        {
            var headerStyle = CreateHeaderStyle(workbook);
            var headerRow = sheet.CreateRow(0);

            for (var i = 0; i < headers.Count; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }
        }

        private static void AutoSizeColumns(ISheet sheet, int columnCount)
        {
            for (var i = 0; i < columnCount; i++)
            {
                sheet.AutoSizeColumn(i);
            }
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
                "\ud45c\uc2dc\uc774\ub984" => new[] { "displayName" },
                "\ub808\ubca8" => new[] { "level" },
                "\uacbd\ud5d8\uce58" => new[] { "exp" },
                "\uc774\ub3d9\uc18d\ub3c4" => new[] { "speed" },
                "\ud06c\uae30" => new[] { "size" },
                "AI\uc720\ud615" => new[] { "aiType" },
                "\uc0c9\uc0c1\uc720\ud615" => new[] { "colorType" },
                "\ud504\ub9ac\ud339" => new[] { "prefab" },
                "\uc2a4\ud3f0\uc911\uc2ecX" => new[] { "spawnCenterX" },
                "\uc2a4\ud3f0\uc911\uc2ecZ" => new[] { "spawnCenterZ" },
                "\uc2a4\ud3f0\ubc94\uc704X" => new[] { "spawnSizeX" },
                "\uc2a4\ud3f0\ubc94\uc704Z" => new[] { "spawnSizeZ" },
                "\uc2a4\ud3f0\ub192\uc774Y" => new[] { "spawnY" },
                "\ud50c\ub808\uc774\uc5b4\ucd5c\uc18c\uac70\ub9ac" => new[] { "minDistanceFromPlayer" },
                "\uc81c\ud55c\uc2dc\uac04" => new[] { "timeLimit" },
                "\ucd5c\uc18c\ub808\ubca8" => new[] { "minLevel" },
                "\ucd5c\ub300\ub808\ubca8" => new[] { "maxLevel" },
                "\uc0dd\uc131\uc218" => new[] { "count" },
                "\ucc98\uce58\uacbd\ud5d8\uce58" => new[] { "defeatExp" },
                "\uac00\uc911\uce58" => new[] { "weight" },
                "\ucd5c\uc18c\ubc30\ud68c\uc18d\ub3c4" => new[] { "minWanderSpeed" },
                "\ucd5c\ub300\ubc30\ud68c\uc18d\ub3c4" => new[] { "maxWanderSpeed" },
                "\ud544\uc694\uacbd\ud5d8\uce58" => new[] { "requiredExp" },
                "\ud06c\uae30\ubc30\uc728" => new[] { "scaleMultiplier" },
                "\uce74\uba54\ub77c\uac70\ub9ac" => new[] { "cameraDistance" },
                "\uce74\uba54\ub77c\ub192\uc774" => new[] { "cameraHeight" },
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

        private static void WriteDinoSampleRow(IRow row, string id, string displayName, int level, int exp, float speed, float size, string aiType, string colorType, string prefab)
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

        private static void WriteStageSampleRow(IRow row, int stageId, string displayName, float spawnCenterX, float spawnCenterZ, float spawnSizeX, float spawnSizeZ, float spawnY, float minDistanceFromPlayer, float timeLimit)
        {
            row.CreateCell(0).SetCellValue(stageId);
            row.CreateCell(1).SetCellValue(displayName);
            row.CreateCell(2).SetCellValue(spawnCenterX);
            row.CreateCell(3).SetCellValue(spawnCenterZ);
            row.CreateCell(4).SetCellValue(spawnSizeX);
            row.CreateCell(5).SetCellValue(spawnSizeZ);
            row.CreateCell(6).SetCellValue(spawnY);
            row.CreateCell(7).SetCellValue(minDistanceFromPlayer);
            row.CreateCell(8).SetCellValue(timeLimit);
        }

        private static void WriteSpawnSampleRow(IRow row, int stageId, string dinoId, int minLevel, int maxLevel, int count, int defeatExp, int weight, float minWanderSpeed, float maxWanderSpeed)
        {
            row.CreateCell(0).SetCellValue(stageId);
            row.CreateCell(1).SetCellValue(dinoId);
            row.CreateCell(2).SetCellValue(minLevel);
            row.CreateCell(3).SetCellValue(maxLevel);
            row.CreateCell(4).SetCellValue(count);
            row.CreateCell(5).SetCellValue(defeatExp);
            row.CreateCell(6).SetCellValue(weight);
            row.CreateCell(7).SetCellValue(minWanderSpeed);
            row.CreateCell(8).SetCellValue(maxWanderSpeed);
        }

        private static void WritePlayerGrowthSampleRow(IRow row, int level, int requiredExp, float scaleMultiplier, float cameraDistance, float cameraHeight)
        {
            row.CreateCell(0).SetCellValue(level);
            row.CreateCell(1).SetCellValue(requiredExp);
            row.CreateCell(2).SetCellValue(scaleMultiplier);
            row.CreateCell(3).SetCellValue(cameraDistance);
            row.CreateCell(4).SetCellValue(cameraHeight);
        }
    }
}
