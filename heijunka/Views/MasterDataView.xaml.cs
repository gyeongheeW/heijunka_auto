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
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using heijunka.Models;
using heijunka.IO;

namespace heijunka.Views
{
    public partial class MasterDataView : UserControl
    {
        private Dictionary<string, Item>? _items;
        public Dictionary<string, Item>? Items => _items;

        public MasterDataView()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel 파일|*.xlsx;*.xlsm|모든 파일|*.*",
                Title = "기준정보 파일 선택"
            };
            if (dlg.ShowDialog() == true)
                TxtMasterPath.Text = dlg.FileName;
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtMasterPath.Text))
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
                _items = reader.ReadItemsOnly(TxtMasterPath.Text);

                ShowItems();
                AppLogger.Info($"기준정보 로드 완료: {_items.Count}개 품목");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로드 오류:\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowItems()
        {
            if (_items == null) return;

            var dt = new DataTable();
            dt.Columns.Add("품목코드");
            for (int c = 0; c < 7; c++)
                dt.Columns.Add($"분류{(char)('A' + c)}");

            // 추가
            dt.Columns.Add("안전재고");

            foreach (var item in _items.Values)
            {
                var row = dt.NewRow();
                row["품목코드"] = item.Code;
                for (int c = 0; c < 7; c++)
                    row[$"분류{(char)('A' + c)}"] =
                        item.Classifications[c];

                // 안전재고만
                row["안전재고"] = item.SafetyStock;

                dt.Rows.Add(row);
            }

            DgMaster.ItemsSource = dt.DefaultView;
        }

        // 시프트 시작시간 읽기
        public TimeSpan[] GetShiftStartTimes()
        {
            return new[]
            {
                ParseTime(TxtShift1Start.Text),
                ParseTime(TxtShift2Start.Text),
                ParseTime(TxtShift3Start.Text)
            };
        }

        private TimeSpan ParseTime(string text)
            => TimeSpan.TryParse(text, out TimeSpan t) ? t : TimeSpan.Zero;

        public void SetPath(string path)
            => TxtMasterPath.Text = path;

        public string GetPath() => TxtMasterPath.Text;
    }
}