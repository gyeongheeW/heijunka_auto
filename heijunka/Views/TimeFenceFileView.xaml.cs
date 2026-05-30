using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using heijunka.Models;
using heijunka.IO;
using heijunka.Planning;

namespace heijunka.Views
{
    public partial class TimeFenceFileView : UserControl
    {
        public event Action<int[,], Dictionary<string, Item>,
            PlanSettings, Dictionary<string, int>,
            Dictionary<string, int[]>>? OnPlanCompleted;

        private PlanSettings? _settings;
        private Dictionary<string, Item>? _items;

        public TimeFenceFileView()
        {
            InitializeComponent();
        }

        public void SetSettings(PlanSettings settings)
            => _settings = settings;

        public void SetItems(Dictionary<string, Item> items)
            => _items = items;

        public string GetDemandPath() => TxtDemandPath.Text;
        public void SetDemandPath(string path)
            => TxtDemandPath.Text = path;
        public void SetOutputFolder(string folder)
            => TxtOutputFolder.Text = folder;

        private void BtnDemandBrowse_Click(
            object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel 파일|*.xlsx;*.xlsm|모든 파일|*.*",
                Title = "소요량 파일 선택"
            };
            if (dlg.ShowDialog() == true)
            {
                TxtDemandPath.Text = dlg.FileName;
                var fi = new FileInfo(dlg.FileName);
                TxtFileInfo.Text =
                    $"파일명: {fi.Name}\n" +
                    $"크기: {fi.Length / 1024}KB\n" +
                    $"수정일: {fi.LastWriteTime:yyyy-MM-dd HH:mm}";
            }
        }

        private void BtnOutputBrowse_Click(
            object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog
            {
                Title = "출력 폴더 선택"
            };
            if (dlg.ShowDialog() == true)
                TxtOutputFolder.Text = dlg.FolderName;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtDemandPath.Text))
            {
                MessageBox.Show("소요량 파일을 선택해주세요.",
                    "알림", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_settings == null)
            {
                MessageBox.Show("기본설정을 먼저 확인해주세요.",
                    "알림", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_items == null || _items.Count == 0)
            {
                MessageBox.Show(
                    "기준정보를 먼저 로드해주세요.\n" +
                    "(기본설정 → 기준정보 탭)",
                    "알림", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                TxtStatus.Text = "실행 중...";
                AppLogBuffer.Clear();

                var reader = new ExcelReader(_settings);

                var (demand, initialStock) =
                    reader.ReadDemandFile(
                        TxtDemandPath.Text, _items);

                var planner = new Planner(
                    _settings, _items, demand, initialStock);
                planner.Run();

                TxtStatus.Text = "완료!";

                OnPlanCompleted?.Invoke(
                    planner.Plan, _items, _settings,
                    initialStock, demand);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생:\n{ex.Message}",
                    "오류", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                TxtStatus.Text = "오류 발생";
            }
        }

        public string GetOutputFolder() => TxtOutputFolder.Text;
    }
}