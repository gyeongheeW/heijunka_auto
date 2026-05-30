using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using heijunka.Models;

namespace heijunka.IO
{
    public class ClipboardParser
    {
        private readonly PlanSettings _settings;

        public ClipboardParser(PlanSettings settings)
        {
            _settings = settings;
        }

        // ── Sheet1: 기준정보 파싱 ──────────────────────
        // 컬럼: 품목코드 | 분류A | 분류B | ... | 분류G
        public Dictionary<string, Item> ParseItems(string clipboardText)
        {
            var items = new Dictionary<string, Item>();
            var lines = SplitLines(clipboardText);

            AppLogger.Section("기준정보 파싱");

            foreach (var line in lines)
            {
                var cols = line.Split('\t');
                if (cols.Length < 1 || string.IsNullOrWhiteSpace(cols[0]))
                    continue;

                string code = cols[0].Trim();
                var item = new Item(code);

                for (int i = 0; i < 7; i++)
                {
                    item.Classifications[i] =
                        (i + 1 < cols.Length) ? cols[i + 1].Trim() : "";
                }

                items[code] = item;
                AppLogger.Log($"품목 읽기: {code} " +
                             $"[{string.Join(", ", item.Classifications)}]");
            }

            AppLogger.Info($"기준정보 파싱 완료: {items.Count}개 품목");
            return items;
        }

        // ── Sheet2: 소요량 파싱 ────────────────────────
        // 컬럼: 품목코드 | 기초재고 | 펜스1 | 펜스2 | ... | 펜스N
        public (Dictionary<string, int[]> demand,
                Dictionary<string, int> initialStock)
            ParseDemand(string clipboardText)
        {
            var demand = new Dictionary<string, int[]>();
            var initialStock = new Dictionary<string, int>();
            var lines = SplitLines(clipboardText);

            AppLogger.Section("소요량 파싱");

            foreach (var line in lines)
            {
                var cols = line.Split('\t');
                if (cols.Length < 2 || string.IsNullOrWhiteSpace(cols[0]))
                    continue;

                string code = cols[0].Trim();

                // 기초재고
                initialStock[code] = ParseInt(cols[1]);

                // 펜스별 소요량
                var fenceDemand = new int[_settings.FenceCount];
                for (int f = 0; f < _settings.FenceCount; f++)
                {
                    int colIdx = f + 2; // 기초재고가 1번
                    fenceDemand[f] = (colIdx < cols.Length)
                        ? ParseInt(cols[colIdx]) : 0;
                }

                demand[code] = fenceDemand;
                AppLogger.Log($"소요량 읽기: {code} " +
                             $"기초:{initialStock[code]} " +
                             $"소요합계:{fenceDemand.Sum()}");
            }

            AppLogger.Info($"소요량 파싱 완료: {demand.Count}개 품목");
            return (demand, initialStock);
        }

        // ── Sheet3: 전환테이블 파싱 ───────────────────
        // 컬럼: 직전품목 | 다음품목 | 가중치
        public Dictionary<(string, string), int>
            ParseTransferTable(string clipboardText)
        {
            var table = new Dictionary<(string, string), int>();
            var lines = SplitLines(clipboardText);

            AppLogger.Section("전환테이블 파싱");

            foreach (var line in lines)
            {
                var cols = line.Split('\t');
                if (cols.Length < 3 || string.IsNullOrWhiteSpace(cols[0]))
                    continue;

                string from = cols[0].Trim();
                string to = cols[1].Trim();
                int weight = ParseInt(cols[2]);

                table[(from, to)] = weight;
                AppLogger.Log($"전환규칙: {from} → {to} = {weight}");
            }

            AppLogger.Info($"전환테이블 파싱 완료: {table.Count}개 규칙");
            return table;
        }

        // ── 헬퍼 ──────────────────────────────────────
        private List<string> SplitLines(string text)
        {
            return text
                .Split(new[] { "\r\n", "\r", "\n" },
                       StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private int ParseInt(string s)
        {
            return int.TryParse(s.Trim(), out int val) ? val : 0;
        }
    }
}
