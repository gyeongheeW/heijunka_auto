using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using heijunka.Models;

namespace heijunka.Views
{
    public partial class QualityView : UserControl
    {
        public QualityView()
        {
            InitializeComponent();
        }

        public void ShowQuality(
            List<double[]> qualityScores,
            PlanSettings settings)
        {
            ShowSummary(qualityScores, settings);
            ShowQualityTable(qualityScores, settings);
            ShowJudgement(qualityScores, settings);
        }

        // ── 전체 요약 ──────────────────────────────────
        private void ShowSummary(
            List<double[]> qualityScores,
            PlanSettings settings)
        {
            PanelSummary.Children.Clear();

            // 분류별 평균 점수
            for (int c = 0; c < 7; c++)
            {
                double avg = 0;
                foreach (var scores in qualityScores)
                    avg += c < scores.Length ? scores[c] : 0;
                avg = qualityScores.Count > 0
                    ? avg / qualityScores.Count : 0;

                var panel = new StackPanel
                {
                    Margin = new Thickness(0, 0, 24, 0)
                };
                panel.Children.Add(new TextBlock
                {
                    Text = settings.ClassLabels[c],
                    FontSize = 11,
                    Foreground = Brushes.Gray
                });
                panel.Children.Add(new TextBlock
                {
                    Text = avg.ToString("F1"),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = avg < 2
                        ? Brushes.Green
                        : avg < 5
                            ? Brushes.Orange
                            : Brushes.Red
                });
                PanelSummary.Children.Add(panel);
            }

            // 전체 평균
            double total = 0;
            foreach (var scores in qualityScores)
                foreach (var s in scores) total += s;
            double totalAvg = qualityScores.Count > 0
                ? total / qualityScores.Count : 0;

            var totalPanel = new StackPanel
            {
                Margin = new Thickness(16, 0, 0, 0)
            };
            totalPanel.Children.Add(new TextBlock
            {
                Text = "전체",
                FontSize = 11,
                Foreground = Brushes.Gray
            });
            totalPanel.Children.Add(new TextBlock
            {
                Text = totalAvg.ToString("F1"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = totalAvg < 5
                    ? Brushes.Green
                    : totalAvg < 10
                        ? Brushes.Orange
                        : Brushes.Red
            });
            PanelSummary.Children.Add(totalPanel);
        }

        // ── 버킷별 테이블 ──────────────────────────────
        private void ShowQualityTable(
            List<double[]> qualityScores,
            PlanSettings settings)
        {
            var dt = new DataTable();
            dt.Columns.Add("버킷");
            for (int c = 0; c < 7; c++)
                dt.Columns.Add(settings.ClassLabels[c]);
            dt.Columns.Add("합계");

            for (int f = 0; f < qualityScores.Count; f++)
            {
                var row = dt.NewRow();
                row["버킷"] = f + 1;
                double sum = 0;
                for (int c = 0; c < 7; c++)
                {
                    double score = c < qualityScores[f].Length
                        ? qualityScores[f][c] : 0;
                    row[settings.ClassLabels[c]] =
                        score.ToString("F1");
                    sum += score;
                }
                row["합계"] = sum.ToString("F1");
                dt.Rows.Add(row);
            }

            // 평균행
            var avgRow = dt.NewRow();
            avgRow["버킷"] = "평균";
            double grandSum = 0;
            for (int c = 0; c < 7; c++)
            {
                double avg = 0;
                foreach (var scores in qualityScores)
                    avg += c < scores.Length ? scores[c] : 0;
                avg = qualityScores.Count > 0
                    ? avg / qualityScores.Count : 0;
                avgRow[settings.ClassLabels[c]] =
                    avg.ToString("F1");
                grandSum += avg;
            }
            avgRow["합계"] = grandSum.ToString("F1");
            dt.Rows.Add(avgRow);

            DgQuality.ItemsSource = dt.DefaultView;
        }

        // ── 판정 ──────────────────────────────────────
        private void ShowJudgement(
            List<double[]> qualityScores,
            PlanSettings settings)
        {
            PanelJudge.Children.Clear();

            for (int c = 0; c < 7; c++)
            {
                double avg = 0;
                foreach (var scores in qualityScores)
                    avg += c < scores.Length ? scores[c] : 0;
                avg = qualityScores.Count > 0
                    ? avg / qualityScores.Count : 0;

                bool isGood = avg < 2;
                var tb = new TextBlock
                {
                    Text = $"{settings.ClassLabels[c]}: " +
                           $"{(isGood ? "✓ 양호" : "⚠ 개선 필요")} " +
                           $"({avg:F1})",
                    Foreground = isGood ? Brushes.Green : Brushes.Orange,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                PanelJudge.Children.Add(tb);
            }
        }
    }
}
