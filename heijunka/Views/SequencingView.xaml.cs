using System.Collections.Generic;
using System.Windows.Controls;
using heijunka.Models;

namespace heijunka.Views
{
    public partial class SequencingView : UserControl
    {
        public SequencingView()
        {
            InitializeComponent();

            FileView.OnSequencingCompleted += OnSequencingCompleted;

            ResultView.OnRerunRequested += () =>
                SequencingSubTab.SelectedIndex = 0;

            ResultView.OnCancelRequested += () =>
                SequencingSubTab.SelectedIndex = 0;
        }

        public void SetSettings(PlanSettings settings)
            => FileView.SetSettings(settings);

        public void SetItems(Dictionary<string, Item> items)
            => FileView.SetItems(items);

        public void SetTransferTable(
            Dictionary<(string, string), int> table)
            => FileView.SetTransferTable(table);

        // ── 경로 저장/로드 ─────────────────────────────
        public string GetInputPath()
            => FileView.GetInputPath();
        public string GetOutputFolder()
            => FileView.GetOutputFolder();
        public void SetInputPath(string path)
            => FileView.SetInputPath(path);
        public void SetOutputFolder(string folder)
            => FileView.SetOutputFolder(folder);

        private void OnSequencingCompleted(
            List<List<Order>> sequences,
            List<double[]> qualityScores,
            PlanSettings settings,
            string outputFolder)
        {
            SequencingSubTab.SelectedIndex = 1;

            ResultView.ShowResult(
                sequences, qualityScores,
                settings, outputFolder);

            QualityView.ShowQuality(qualityScores, settings);

            LogView.SetLog(AppLogBuffer.GetLog());
        }
    }
}