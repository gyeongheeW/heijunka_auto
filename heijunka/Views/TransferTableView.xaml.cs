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
using Microsoft.Win32;
using heijunka.Models;
using heijunka.IO;

namespace heijunka.Views
{
    public partial class TransferTableView : UserControl
    {
        private Dictionary<(string, string), int>? _transferTable;
        public Dictionary<(string, string), int>? TransferTable
            => _transferTable;

        public TransferTableView()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel 파일|*.xlsx;*.xlsm|모든 파일|*.*",
                Title = "전환테이블 파일 선택"
            };
            if (dlg.ShowDialog() == true)
                TxtTransferPath.Text = dlg.FileName;
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtTransferPath.Text))
            {
                MessageBox.Show("파일을 선택해주세요.",
                    "알림", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var settings = new PlanSettings();
                var reader = new ExcelReader(settings);
                _transferTable = reader.ReadTransferTableOnly(
                    TxtTransferPath.Text);

                ShowTransferTable();
                AppLogger.Info($"전환테이블 로드 완료: " +
                              $"{_transferTable.Count}개 규칙");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로드 오류:\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowTransferTable()
        {
            if (_transferTable == null) return;

            var dt = new DataTable();
            dt.Columns.Add("직전 품목");
            dt.Columns.Add("다음 품목");
            dt.Columns.Add("가중치");

            foreach (var kv in _transferTable)
            {
                var row = dt.NewRow();
                row["직전 품목"] = kv.Key.Item1;
                row["다음 품목"] = kv.Key.Item2;
                row["가중치"] = kv.Value;
                dt.Rows.Add(row);
            }

            DgTransfer.ItemsSource = dt.DefaultView;
        }

        public void SetPath(string path)
        {
            TxtTransferPath.Text = path;
        }

        public string GetPath() => TxtTransferPath.Text;
    }
}