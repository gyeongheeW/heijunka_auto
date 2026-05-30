using System.Collections.Generic;
using System.Windows.Controls;
using heijunka.Models;

namespace heijunka.Views
{
    public partial class TimeFenceView : UserControl
    {
        public TimeFenceView()
        {
            InitializeComponent();

            FileView.OnPlanCompleted += OnPlanCompleted;

            ResultView.OnRerunRequested += () =>
                TimeFenceSubTab.SelectedIndex = 0;
            ResultView.OnCancelRequested += () =>
                TimeFenceSubTab.SelectedIndex = 0;
        }

        public void SetSettings(PlanSettings settings)
            => FileView.SetSettings(settings);

        public void SetItems(Dictionary<string, Item> items)
            => FileView.SetItems(items);

        public string GetDemandPath()
            => FileView.GetDemandPath();
        public string GetOutputFolder()
            => FileView.GetOutputFolder();
        public void SetDemandPath(string path)
            => FileView.SetDemandPath(path);
        public void SetOutputFolder(string folder)
            => FileView.SetOutputFolder(folder);

        private void OnPlanCompleted(
            int[,] plan,
            Dictionary<string, Item> items,
            PlanSettings settings,
            Dictionary<string, int> initialStock,
            Dictionary<string, int[]> demand)
        {
            TimeFenceSubTab.SelectedIndex = 1;

            ResultView.ShowResult(
                plan, items, settings,
                FileView.GetOutputFolder());

            EndingView.ShowResult(
                plan, items, settings,
                initialStock, demand);

            LogView.SetLog(AppLogBuffer.GetLog());
        }
    }
}