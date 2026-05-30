using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using heijunka.Models;

namespace heijunka.IO
{
    public class ExcelWriter
    {
        private readonly PlanSettings _settings;
        private readonly List<Item> _itemList;

        public ExcelWriter(PlanSettings settings, List<Item> itemList)
        {
            _settings = settings;
            _itemList = itemList;
        }

        public void Write(string filePath, int[,] plan)
        {
            AppLogger.Section("결과 엑셀 저장");

            using var workbook = new XLWorkbook();
            WritePlanSheet(workbook, plan);
            workbook.SaveAs(filePath);

            AppLogger.Info($"저장 완료: {filePath}");
        }
        public void WriteSequencing(
    string filePath,
    List<List<Order>> sequences,
    List<double[]> qualityScores,
    PlanSettings settings)
        {
            AppLogger.Section("시퀀싱 결과 저장");

            using var workbook = new XLWorkbook();
            WriteSequenceSheet(workbook, sequences, qualityScores, settings);
            workbook.SaveAs(filePath);

            AppLogger.Info($"저장 완료: {filePath}");
        }

        private void WriteSequenceSheet(
            XLWorkbook workbook,
            List<List<Order>> sequences,
            List<double[]> qualityScores,
            PlanSettings settings)
        {
            var ws = workbook.Worksheets.Add("버킷별시퀀스");

            // 헤더
            ws.Cell(1, 1).Value = "버킷";
            ws.Cell(1, 2).Value = "순서";
            ws.Cell(1, 3).Value = "품목코드";
            ws.Cell(1, 4).Value = "수량";
            for (int c = 0; c < 7; c++)
                ws.Cell(1, c + 5).Value = settings.ClassLabels[c];

            var headerRange = ws.Range(1, 1, 1, 11);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#2D3748");
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            for (int f = 0; f < sequences.Count; f++)
            {
                if (sequences[f].Count == 0) continue;

                int seq = 1;
                foreach (var order in sequences[f])
                {
                    ws.Cell(row, 1).Value = f + 1;
                    ws.Cell(row, 2).Value = seq++;
                    ws.Cell(row, 3).Value = order.ItemCode;
                    ws.Cell(row, 4).Value = order.Qty;
                    for (int c = 0; c < 7; c++)
                        ws.Cell(row, c + 5).Value =
                            order.Classifications[c] ?? "";
                    row++;
                }

                // 펜스 구분선 + 품질점수
                double total = f < qualityScores.Count
                    ? qualityScores[f].Sum() : 0;
                var summaryCell = ws.Cell(row, 1);
                summaryCell.Value =
                    $"── 펜스{f + 1} 품질점수: {total:F1} ──";
                ws.Range(row, 1, row, 11).Merge();
                summaryCell.Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#E2E8F0");
                summaryCell.Style.Font.Bold = true;
                row++;
            }

            ws.Columns().AdjustToContents();
            AppLogger.Info("버킷별시퀀스 시트 작성 완료");
        }
        // ── Sheet1: 펜스별 수량 ────────────────────────
        private void WritePlanSheet(XLWorkbook workbook, int[,] plan)
        {
            var ws = workbook.Worksheets.Add("펜스별수량");

            // 헤더
            ws.Cell(1, 1).Value = "품목코드";
            for (int c = 0; c < 7; c++)
                ws.Cell(1, c + 2).Value = _settings.ClassLabels[c];
            for (int f = 0; f < _settings.FenceCount; f++)
                ws.Cell(1, f + 9).Value = $"펜스{f + 1}";

            // 헤더 스타일
            var headerRange = ws.Range(1, 1, 1, 8 + _settings.FenceCount);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#2D3748");
            headerRange.Style.Font.FontColor = XLColor.White;

            // 데이터
            for (int i = 0; i < _itemList.Count; i++)
            {
                var item = _itemList[i];
                int row = i + 2;

                ws.Cell(row, 1).Value = item.Code;
                for (int c = 0; c < 7; c++)
                    ws.Cell(row, c + 2).Value = item.Classifications[c];
                for (int f = 0; f < _settings.FenceCount; f++)
                {
                    var cell = ws.Cell(row, f + 9);
                    cell.Value = plan[i, f];

                    // 0이면 회색
                    if (plan[i, f] == 0)
                        cell.Style.Font.FontColor = XLColor.LightGray;
                }
            }

            // 합계행
            int totalRow = _itemList.Count + 2;
            ws.Cell(totalRow, 1).Value = "TOTAL";
            ws.Cell(totalRow, 1).Style.Font.Bold = true;

            for (int f = 0; f < _settings.FenceCount; f++)
            {
                int sum = 0;
                for (int i = 0; i < _itemList.Count; i++)
                    sum += plan[i, f];

                var cell = ws.Cell(totalRow, f + 9);
                cell.Value = sum;
                cell.Style.Font.Bold = true;

                // 목표 달성 여부
                bool isLastOfShift = _settings.IsLastFenceOfShift(f);
                if (isLastOfShift)
                    cell.Style.Font.FontColor = XLColor.Blue;
                else if (sum == _settings.Target)
                    cell.Style.Font.FontColor = XLColor.Green;
                else
                    cell.Style.Font.FontColor = XLColor.Red;
            }

            // 컬럼 너비 자동조정
            ws.Columns().AdjustToContents();

            AppLogger.Info("펜스별수량 시트 작성 완료");
        }
    }
}