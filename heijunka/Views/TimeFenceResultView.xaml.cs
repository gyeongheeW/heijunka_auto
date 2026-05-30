using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using heijunka.Models;
using heijunka.IO;
using IOPath = System.IO.Path;

namespace heijunka.Views
{
    public partial class TimeFenceResultView : UserControl
    {
        private int[,]? _plan;
        private Dictionary<string, Item>? _items;
        private PlanSettings? _settings;
        private string _outputFolder = "";

        public event Action? OnRerunRequested;
        public event Action? OnCancelRequested;

        public TimeFenceResultView()
        {
            InitializeComponent();
        }

        public void ShowResult(
            int[,] plan,
            Dictionary<string, Item> items,
            PlanSettings settings,
            string outputFolder = "")
        {
            _plan = plan;
            _items = items;
            _settings = settings;
            _outputFolder = outputFolder;

            ShowPlanTable();
            ShowShiftTimes();

            BtnSave.IsEnabled = true;
            BtnRerun.IsEnabled = true;
            BtnCancel.IsEnabled = true;
        }

        // ── 생산 결과 테이블 ───────────────────────────
        private void ShowPlanTable()
        {
            if (_plan == null || _items == null ||
                _settings == null) return;

            var itemList = new List<Item>(_items.Values);
            var dt = new DataTable();

            dt.Columns.Add("품목코드");
            for (int c = 0; c < 7; c++)
                dt.Columns.Add(_settings.ClassLabels[c]);
            for (int f = 0; f < _settings.FenceCount; f++)
                dt.Columns.Add($"펜스{f + 1}");

            for (int i = 0; i < itemList.Count; i++)
            {
                var row = dt.NewRow();
                row["품목코드"] = itemList[i].Code;
                for (int c = 0; c < 7; c++)
                    row[_settings.ClassLabels[c]] =
                        itemList[i].Classifications[c];
                for (int f = 0; f < _settings.FenceCount; f++)
                    row[$"펜스{f + 1}"] = _plan[i, f];
                dt.Rows.Add(row);
            }

            // 합계행
            var totalRow = dt.NewRow();
            totalRow["품목코드"] = "TOTAL";
            for (int f = 0; f < _settings.FenceCount; f++)
            {
                int sum = 0;
                for (int i = 0; i < itemList.Count; i++)
                    sum += _plan[i, f];
                totalRow[$"펜스{f + 1}"] = sum;
            }
            dt.Rows.Add(totalRow);

            DgPlanResult.ItemsSource = dt.DefaultView;
        }

        // ── 시프트별 예상 종료시간 ─────────────────────
        private void ShowShiftTimes()
        {
            if (_settings == null) return;

            PanelShiftTimes.Children.Clear();

            var shiftStarts = new[]
            {
                _settings.Shift1Start,
                _settings.Shift2Start,
                _settings.Shift3Start
            };

            for (int s = 0; s < _settings.ShiftCount; s++)
            {
                int lastFence =
                    (s + 1) * _settings.FencesPerShift - 1;
                int lastFenceTotal = 0;
                if (_plan != null && _items != null)
                {
                    for (int i = 0; i < _items.Count; i++)
                        lastFenceTotal += _plan[i, lastFence];
                }

                int saved = _settings.Target - lastFenceTotal;
                double lastFenceHours =
                    (double)lastFenceTotal / _settings.UPH;

                TimeSpan shiftStart = s < shiftStarts.Length
                    ? shiftStarts[s] : TimeSpan.Zero;

                TimeSpan endTime = shiftStart
                    + TimeSpan.FromHours(
                        (_settings.FencesPerShift - 1)
                        * _settings.FenceHours)
                    + TimeSpan.FromHours(lastFenceHours);

                string endStr = endTime.TotalHours >= 24
                    ? $"{(int)(endTime.TotalHours % 24):D2}" +
                      $":{endTime.Minutes:D2} (+1일)"
                    : $"{(int)endTime.TotalHours:D2}" +
                      $":{endTime.Minutes:D2}";

                var tb = new TextBlock
                {
                    Text = $"시프트{s + 1} 예상 종료: {endStr} " +
                           $"(절약 {saved}대)",
                    Margin = new Thickness(0, 0, 16, 4),
                    Foreground = saved > 0
                        ? Brushes.Green : Brushes.Gray
                };
                PanelShiftTimes.Children.Add(tb);
            }
        }

        // ── 버튼 ──────────────────────────────────────
        private void BtnSave_Click(
            object sender, RoutedEventArgs e)
        {
            if (_plan == null || _items == null ||
                _settings == null) return;

            try
            {
                string folder = string.IsNullOrEmpty(_outputFolder)
                    ? Environment.GetFolderPath(
                        Environment.SpecialFolder.Desktop)
                    : _outputFolder;

                string fileName =
                    $"타임펜스결과_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                string filePath =
                    IOPath.Combine(folder, fileName);

                var writer = new ExcelWriter(
                    _settings, new List<Item>(_items.Values));
                writer.Write(filePath, _plan);

                MessageBox.Show($"저장되었습니다.\n{filePath}",
                    "완료", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 오류:\n{ex.Message}",
                    "오류", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnRerun_Click(
            object sender, RoutedEventArgs e)
        {
            OnRerunRequested?.Invoke();
        }

        private void BtnCancel_Click(
            object sender, RoutedEventArgs e)
        {
            OnCancelRequested?.Invoke();
        }
    }
}