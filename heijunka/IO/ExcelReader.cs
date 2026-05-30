using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using heijunka.Models;

namespace heijunka.IO
{
    public class ExcelReader
    {
        private readonly PlanSettings _settings;

        public ExcelReader(PlanSettings settings)
        {
            _settings = settings;
        }

        // ── 소요량 파일 읽기 (타임펜스용) ───────────
        public (Dictionary<string, int[]> demand,
                Dictionary<string, int> initialStock)
            ReadDemandFile(string filePath,
                Dictionary<string, Item>? items = null)
        {
            AppLogger.Section("소요량 파일 읽기 시작");
            AppLogger.Info($"파일: {filePath}");

            using var workbook = new XLWorkbook(filePath);
            var emptyItems = items ?? new Dictionary<string, Item>();
            var result = ReadDemandSheet(workbook, emptyItems, 1);

            AppLogger.Section("소요량 파일 읽기 완료");
            return result;
        }

        // ── 기준정보만 읽기 ────────────────────────
        public Dictionary<string, Item> ReadItemsOnly(
            string filePath)
        {
            AppLogger.Section("기준정보 파일 읽기");
            using var workbook = new XLWorkbook(filePath);
            return ReadItems(workbook, 1);
        }

        // ── 전환테이블만 읽기 ──────────────────────
        public Dictionary<(string, string), int>
            ReadTransferTableOnly(string filePath)
        {
            AppLogger.Section("전환테이블 파일 읽기");
            using var workbook = new XLWorkbook(filePath);
            return ReadTransferTable(workbook, 1);
        }

        // ── 타임펜스 결과 파일 읽기 (시퀀싱용) ─────
        public (int[,] plan, Dictionary<string, Item> items)
            ReadPlanFile(string filePath)
        {
            AppLogger.Section("타임펜스 결과 파일 읽기");
            AppLogger.Info($"파일: {filePath}");

            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet(1);

            var items = new Dictionary<string, Item>();
            var planData = new List<int[]>();

            int row = 2;
            while (true)
            {
                var codeCell = ws.Cell(row, 1);
                if (codeCell.IsEmpty()) break;

                string code = codeCell.GetString().Trim();
                if (string.IsNullOrWhiteSpace(code)) break;

                if (code.ToUpper() == "TOTAL")
                {
                    row++;
                    continue;
                }

                var item = new Item(code);
                for (int c = 0; c < 7; c++)
                {
                    var cell = ws.Cell(row, c + 2);
                    item.Classifications[c] = cell.IsEmpty()
                        ? "" : cell.GetString().Trim();
                }
                items[code] = item;

                var fenceData = new int[_settings.FenceCount];
                for (int f = 0; f < _settings.FenceCount; f++)
                    fenceData[f] = Math.Max(0,
                        ParseInt(ws.Cell(row, f + 9)));

                planData.Add(fenceData);
                AppLogger.Log($"{code}: 합계={fenceData.Sum()}");
                row++;
            }

            var plan = new int[planData.Count,
                _settings.FenceCount];
            for (int i = 0; i < planData.Count; i++)
                for (int f = 0; f < _settings.FenceCount; f++)
                    plan[i, f] = planData[i][f];

            AppLogger.Info($"읽기 완료: {items.Count}개 품목");
            return (plan, items);
        }

        // ── 기준정보 읽기 ──────────────────────────
        private Dictionary<string, Item> ReadItems(
            XLWorkbook workbook, int sheetIndex)
        {
            var items = new Dictionary<string, Item>();

            if (workbook.Worksheets.Count < sheetIndex)
            {
                AppLogger.Warn($"Sheet{sheetIndex} 없음");
                return items;
            }

            var ws = workbook.Worksheet(sheetIndex);
            AppLogger.Section("기준정보 읽기");

            int row = 2;
            while (true)
            {
                var codeCell = ws.Cell(row, 1);
                if (codeCell.IsEmpty()) break;

                string code = codeCell.GetString().Trim();
                if (string.IsNullOrWhiteSpace(code)) break;

                if (code.ToUpper() == "TOTAL")
                {
                    row++;
                    continue;
                }

                var item = new Item(code);

                // 분류A~G (컬럼 2~8)
                for (int c = 0; c < 7; c++)
                {
                    var cell = ws.Cell(row, c + 2);
                    item.Classifications[c] = cell.IsEmpty()
                        ? "" : cell.GetString().Trim();
                }

                // 안전재고 (컬럼 9)
                item.SafetyStock = ParseInt(ws.Cell(row, 9));

                items[code] = item;
                AppLogger.Log($"품목: {code} " +
                    $"안전재고:{item.SafetyStock}");
                row++;
            }

            AppLogger.Info(
                $"기준정보 읽기 완료: {items.Count}개 품목");
            return items;
        }

        // ── 소요량 읽기 ────────────────────────────
        private (Dictionary<string, int[]> demand,
                 Dictionary<string, int> initialStock)
            ReadDemandSheet(XLWorkbook workbook,
                            Dictionary<string, Item> items,
                            int sheetIndex)
        {
            var demand = new Dictionary<string, int[]>();
            var initialStock = new Dictionary<string, int>();

            if (workbook.Worksheets.Count < sheetIndex)
            {
                AppLogger.Warn($"Sheet{sheetIndex} 없음");
                return (demand, initialStock);
            }

            var ws = workbook.Worksheet(sheetIndex);
            AppLogger.Section("소요량 읽기");

            // 가동여부 초기화 (기본값 1)
            int[] fenceActive = new int[_settings.FenceCount];
            for (int i = 0; i < _settings.FenceCount; i++)
                fenceActive[i] = 1;

            int row = 2;
            while (true)
            {
                var codeCell = ws.Cell(row, 1);
                if (codeCell.IsEmpty()) break;

                string code = codeCell.GetString().Trim();
                if (string.IsNullOrWhiteSpace(code)) break;

                if (code.ToUpper() == "TOTAL")
                {
                    row++;
                    continue;
                }

                // 가동여부 행 읽기
                if (code.ToUpper() == "가동여부" ||
                    code.ToUpper() == "ACTIVE")
                {
                    for (int f = 0; f < _settings.FenceCount; f++)
                    {
                        int val = ParseInt(ws.Cell(row, f + 3));
                        fenceActive[f] = val == 0 ? 0 : 1;
                    }
                    AppLogger.Info($"가동여부: " +
                        $"[{string.Join(",", fenceActive)}]");
                    row++;
                    continue;
                }

                // 기초재고
                initialStock[code] = ParseInt(ws.Cell(row, 2));

                // 원본 소요량 그대로 보관
                // (비가동 누적 처리 안함 → NetDemandCalc에서 실시간)
                var fenceDemand = new int[_settings.FenceCount];
                for (int f = 0; f < _settings.FenceCount; f++)
                    fenceDemand[f] = Math.Max(0,
                        ParseInt(ws.Cell(row, f + 3)));

                demand[code] = fenceDemand;
                AppLogger.Log($"소요량: {code} " +
                    $"기초:{initialStock[code]} " +
                    $"합계:{fenceDemand.Sum()}");
                row++;
            }

            // 가동여부 설정 반영
            _settings.FenceActive = fenceActive;

            foreach (var code in demand.Keys)
                if (items.Count > 0 && !items.ContainsKey(code))
                    AppLogger.Warn($"기준정보 없는 품목: {code}");

            AppLogger.Info(
                $"소요량 읽기 완료: {demand.Count}개 품목");
            return (demand, initialStock);
        }

        // ── 전환테이블 읽기 ────────────────────────
        private Dictionary<(string, string), int>
            ReadTransferTable(XLWorkbook workbook, int sheetIndex)
        {
            var table = new Dictionary<(string, string), int>();

            if (workbook.Worksheets.Count < sheetIndex)
            {
                AppLogger.Warn(
                    $"Sheet{sheetIndex} 없음 → 빈 테이블");
                return table;
            }

            var ws = workbook.Worksheet(sheetIndex);
            AppLogger.Section("전환테이블 읽기");

            int row = 2;
            while (true)
            {
                var fromCell = ws.Cell(row, 1);
                if (fromCell.IsEmpty()) break;

                string from = fromCell.GetString().Trim();
                string to = ws.Cell(row, 2).GetString().Trim();
                int weight = ParseInt(ws.Cell(row, 3));

                if (!string.IsNullOrWhiteSpace(from) &&
                    !string.IsNullOrWhiteSpace(to))
                {
                    table[(from, to)] = weight;
                    AppLogger.Log(
                        $"전환규칙: {from} → {to} = {weight}");
                }

                row++;
            }

            AppLogger.Info(
                $"전환테이블 읽기 완료: {table.Count}개 규칙");
            return table;
        }

        // ── 헬퍼 ──────────────────────────────────
        private int ParseInt(IXLCell cell)
        {
            if (cell.IsEmpty()) return 0;
            return int.TryParse(
                cell.GetString().Trim(), out int val)
                ? val : 0;
        }
    }
}