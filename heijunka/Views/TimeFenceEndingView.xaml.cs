using System.Collections.Generic;
using System.Data;
using System.Windows.Controls;
using heijunka.Models;

namespace heijunka.Views
{
    public partial class TimeFenceEndingView : UserControl
    {
        public TimeFenceEndingView()
        {
            InitializeComponent();
        }

        public void ShowResult(
            int[,] plan,
            Dictionary<string, Item> items,
            PlanSettings settings,
            Dictionary<string, int> initialStock,
            Dictionary<string, int[]> demand)
        {
            var itemList = new List<Item>(items.Values);
            var dt = new DataTable();

            // ── 헤더 ──────────────────────────────────
            dt.Columns.Add("품목코드");
            dt.Columns.Add("기초재고");
            for (int f = 0; f < settings.FenceCount; f++)
                dt.Columns.Add($"펜스{f + 1}");

            // ── 데이터 ────────────────────────────────
            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                var row = dt.NewRow();
                row["품목코드"] = item.Code;

                int stock = initialStock.ContainsKey(item.Code)
                    ? initialStock[item.Code] : 0;
                row["기초재고"] = stock;

                // 펜스별 기말재고 계산
                // 기초재고 + 펜스1생산 - 원본소요량1 = 펜스1 기말
                // 펜스1기말 + 펜스2생산 - 원본소요량2 = 펜스2 기말
                int prevEnding = stock;

                for (int f = 0; f < settings.FenceCount; f++)
                {
                    int produced = plan[i, f];
                    int consumed = demand.ContainsKey(item.Code)
                        ? demand[item.Code][f] : 0;

                    int ending = prevEnding + produced - consumed;
                    row[$"펜스{f + 1}"] = ending;

                    prevEnding = ending;
                }

                dt.Rows.Add(row);
            }

            // ── 합계행 ────────────────────────────────
            var totalRow = dt.NewRow();
            totalRow["품목코드"] = "TOTAL";

            // 기초재고 합계
            int totalStock = 0;
            foreach (var item in itemList)
                totalStock += initialStock.ContainsKey(item.Code)
                    ? initialStock[item.Code] : 0;
            totalRow["기초재고"] = totalStock;

            // 펜스별 기말재고 합계
            for (int f = 0; f < settings.FenceCount; f++)
            {
                int sum = 0;
                for (int i = 0; i < itemList.Count; i++)
                {
                    var item = itemList[i];
                    int stock = initialStock.ContainsKey(item.Code)
                        ? initialStock[item.Code] : 0;

                    int prevEnding = stock;
                    for (int f2 = 0; f2 <= f; f2++)
                    {
                        int produced = plan[i, f2];
                        int consumed =
                            demand.ContainsKey(item.Code)
                            ? demand[item.Code][f2] : 0;
                        prevEnding = prevEnding
                            + produced - consumed;
                    }
                    sum += prevEnding;
                }
                totalRow[$"펜스{f + 1}"] = sum;
            }
            dt.Rows.Add(totalRow);

            DgEndingInventory.ItemsSource = dt.DefaultView;
        }
    }
}