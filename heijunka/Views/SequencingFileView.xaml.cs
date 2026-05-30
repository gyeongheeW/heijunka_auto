using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using heijunka.Models;
using heijunka.IO;
using heijunka.Sequencing;

namespace heijunka.Views
{
    public partial class SequencingFileView : UserControl
    {
        public event Action<List<List<Order>>, List<double[]>,
            PlanSettings, string>? OnSequencingCompleted;

        private PlanSettings? _settings;
        private Dictionary<string, Item>? _items;
        private Dictionary<(string, string), int>? _transferTable;

        public SequencingFileView()
        {
            InitializeComponent();
        }

        public void SetSettings(PlanSettings settings)
            => _settings = settings;

        public void SetItems(Dictionary<string, Item> items)
            => _items = items;

        public void SetTransferTable(
            Dictionary<(string, string), int> table)
            => _transferTable = table;

        private void BtnInputBrowse_Click(
            object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel 파일|*.xlsx;*.xlsm|모든 파일|*.*",
                Title = "타임펜스 결과 파일 선택"
            };
            if (dlg.ShowDialog() == true)
                TxtInputPath.Text = dlg.FileName;
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

        public List<string> GetPreviousSequence()
        {
            var prev = new List<string>();
            foreach (var txt in new[]
            {
                TxtPrev1, TxtPrev2, TxtPrev3,
                TxtPrev4, TxtPrev5
            })
            {
                if (!string.IsNullOrWhiteSpace(txt.Text))
                    prev.Add(txt.Text.Trim());
            }
            return prev;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtInputPath.Text))
            {
                MessageBox.Show(
                    "타임펜스 결과 파일을 선택해주세요.",
                    "알림", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_settings == null)
            {
                MessageBox.Show(
                    "기본설정을 먼저 확인해주세요.",
                    "알림", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                TxtStatus.Text = "실행 중...";
                AppLogBuffer.Clear();

                // 타임펜스 결과 파일 읽기
                var reader = new ExcelReader(_settings);
                var (plan, itemsFromFile) =
                    reader.ReadPlanFile(TxtInputPath.Text);

                // 기준정보 우선순위:
                // 기본설정 탭 로드 > 타임펜스 결과 파일
                var items = (_items != null && _items.Count > 0)
                    ? _items : itemsFromFile;

                AppLogger.Info($"품목수: {items.Count}개");

                // 시퀀싱
                var sequencer = new Sequencer(
                    _settings, items,
                    _transferTable ??
                        new Dictionary<(string, string), int>());

                sequencer.Run(plan, GetPreviousSequence());

                TxtStatus.Text = "완료!";

                OnSequencingCompleted?.Invoke(
                    sequencer.Sequences,
                    sequencer.QualityScores,
                    _settings,
                    TxtOutputFolder.Text);
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
        public string GetInputPath() => TxtInputPath.Text;
        public void SetInputPath(string path) => TxtInputPath.Text = path;
        public void SetOutputFolder(string folder) => TxtOutputFolder.Text = folder;
    }
}