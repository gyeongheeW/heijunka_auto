using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using heijunka.Models;
using heijunka.IO;
using IOPath = System.IO.Path;

namespace heijunka.Views
{
    public partial class SequencingResultView : UserControl
    {
        private List<List<Order>>? _sequences;
        private List<double[]>? _qualityScores;
        private PlanSettings? _settings;
        private string _outputFolder = "";

        public event Action? OnRerunRequested;
        public event Action? OnCancelRequested;

        public SequencingResultView()
        {
            InitializeComponent();
        }

        public void ShowResult(
            List<List<Order>> sequences,
            List<double[]> qualityScores,
            PlanSettings settings,
            string outputFolder = "")
        {
            _sequences = sequences;
            _qualityScores = qualityScores;
            _settings = settings;
            _outputFolder = outputFolder;

            ShowResultTable(sequences, qualityScores, settings);

            BtnSave.IsEnabled = true;
            BtnRerun.IsEnabled = true;
            BtnCancel.IsEnabled = true;
        }

        private void ShowResultTable(
            List<List<Order>> sequences,
            List<double[]> qualityScores,
            PlanSettings settings)
        {
            var dt = new DataTable();

            dt.Columns.Add("펜스");
            dt.Columns.Add("오더수");
            for (int c = 0; c < 7; c++)
                dt.Columns.Add($"{settings.ClassLabels[c]} 점수");
            dt.Columns.Add("전체 점수");

            double[] totalScores = new double[7];
            for (int f = 0; f < sequences.Count; f++)
            {
                var row = dt.NewRow();
                row["펜스"] = f + 1;
                row["오더수"] = sequences[f].Count;

                double fenceTotal = 0;
                for (int c = 0; c < 7; c++)
                {
                    double score = f < qualityScores.Count
                        ? qualityScores[f][c] : 0;
                    row[$"{settings.ClassLabels[c]} 점수"] =
                        score.ToString("F1");
                    totalScores[c] += score;
                    fenceTotal += score;
                }
                row["전체 점수"] = fenceTotal.ToString("F1");
                dt.Rows.Add(row);
            }

            var avgRow = dt.NewRow();
            avgRow["펜스"] = "평균";
            avgRow["오더수"] = "-";
            double grandTotal = 0;
            for (int c = 0; c < 7; c++)
            {
                double avg = sequences.Count > 0
                    ? totalScores[c] / sequences.Count : 0;
                avgRow[$"{settings.ClassLabels[c]} 점수"] =
                    avg.ToString("F1");
                grandTotal += avg;
            }
            avgRow["전체 점수"] = grandTotal.ToString("F1");
            dt.Rows.Add(avgRow);

            DgSequencingResult.ItemsSource = dt.DefaultView;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_sequences == null || _settings == null) return;

            try
            {
                string folder = string.IsNullOrEmpty(_outputFolder)
                    ? Environment.GetFolderPath(
                        Environment.SpecialFolder.Desktop)
                    : _outputFolder;

                string fileName =
                    $"시퀀싱결과_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                string filePath = IOPath.Combine(folder, fileName);

                var writer = new ExcelWriter(
                    _settings, new List<Item>());
                writer.WriteSequencing(
                    filePath, _sequences,
                    _qualityScores ?? new List<double[]>(),
                    _settings);

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

        private void BtnRerun_Click(object sender, RoutedEventArgs e)
        {
            OnRerunRequested?.Invoke();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            OnCancelRequested?.Invoke();
        }
    }
}