using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using heijunka.Models;

namespace heijunka
{
    public partial class MainWindow : Window
    {
        private AppConfig _config = new AppConfig();

        public MainWindow()
        {
            InitializeComponent();
            MainTab.SelectionChanged += MainTab_SelectionChanged;
            Closing += MainWindow_Closing;
            LoadConfig();
        }

        // ── 종료 시 저장 ───────────────────────────────
        private void MainWindow_Closing(object? sender,
            CancelEventArgs e)
        {
            SaveConfig();
        }

        // ── 설정 로드 ──────────────────────────────────
        private void LoadConfig()
        {
            _config = AppConfig.Load();

            // 설정값 → UI 반영
            SettingsView.SetConfig(_config);

            // 경로 복원
            if (!string.IsNullOrEmpty(_config.TimeFenceDemandPath))
                TimeFenceView.SetDemandPath(
                    _config.TimeFenceDemandPath);
            if (!string.IsNullOrEmpty(_config.TimeFenceOutputFolder))
                TimeFenceView.SetOutputFolder(
                    _config.TimeFenceOutputFolder);
            if (!string.IsNullOrEmpty(_config.SequencingInputPath))
                SequencingView.SetInputPath(
                    _config.SequencingInputPath);
            if (!string.IsNullOrEmpty(_config.SequencingOutputFolder))
                SequencingView.SetOutputFolder(
                    _config.SequencingOutputFolder);
        }

        // ── 설정 저장 ──────────────────────────────────
        public void SaveConfig()
        {
            // UI → config 반영
            SettingsView.GetConfig(_config);

            // 파일 경로 저장
            _config.TimeFenceDemandPath =
                TimeFenceView.GetDemandPath();
            _config.TimeFenceOutputFolder =
                TimeFenceView.GetOutputFolder();
            _config.SequencingInputPath =
                SequencingView.GetInputPath();
            _config.SequencingOutputFolder =
                SequencingView.GetOutputFolder();

            _config.Save();
        }

        // ── 탭 전환 시 설정값 전달 ─────────────────────
        private void MainTab_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (MainTab.SelectedIndex == 0) return;

            var settings = SettingsView.GetSettings();
            var items = SettingsView.GetItems();
            var transferTable = SettingsView.GetTransferTable();

            if (MainTab.SelectedIndex == 1)
            {
                TimeFenceView.SetSettings(settings);
                if (items != null)
                    TimeFenceView.SetItems(items);
            }

            if (MainTab.SelectedIndex == 2)
            {
                SequencingView.SetSettings(settings);
                if (items != null)
                    SequencingView.SetItems(items);
                if (transferTable != null)
                    SequencingView.SetTransferTable(transferTable);
            }
        }
    }
}