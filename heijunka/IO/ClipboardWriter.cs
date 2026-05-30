using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using heijunka.Models;

namespace heijunka.IO
{
    public class ClipboardWriter
    {
        private readonly PlanSettings _settings;
        private readonly List<Item> _itemList;

        public ClipboardWriter(PlanSettings settings, List<Item> itemList)
        {
            _settings = settings;
            _itemList = itemList;
        }

        // ── 버킷별 수량 출력 ───────────────────────────
        public string WritePlan(int[,] plan)
        {
            var sb = new System.Text.StringBuilder();

            // 헤더
            sb.Append("품목코드");
            for (int c = 0; c < 7; c++)
                sb.Append($"\t{_settings.ClassLabels[c]}");
            for (int f = 0; f < _settings.FenceCount; f++)
                sb.Append($"\t펜스{f + 1}");
            sb.AppendLine();

            // 데이터
            for (int i = 0; i < _itemList.Count; i++)
            {
                var item = _itemList[i];
                sb.Append(item.Code);
                for (int c = 0; c < 7; c++)
                    sb.Append($"\t{item.Classifications[c]}");
                for (int f = 0; f < _settings.FenceCount; f++)
                    sb.Append($"\t{plan[i, f]}");
                sb.AppendLine();
            }

            // 합계행
            sb.Append("TOTAL");
            for (int c = 0; c < 7; c++) sb.Append("\t");
            for (int f = 0; f < _settings.FenceCount; f++)
            {
                int sum = 0;
                for (int i = 0; i < _itemList.Count; i++)
                    sum += plan[i, f];
                sb.Append($"\t{sum}");
            }
            sb.AppendLine();

            AppLogger.Info("버킷별 수량 클립보드 준비 완료");
            return sb.ToString();
        }
    }
}