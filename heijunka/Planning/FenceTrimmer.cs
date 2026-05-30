using System;
using System.Collections.Generic;
using System.Linq;
using heijunka.Models;

namespace heijunka.Planning
{
    public class FenceTrimmer
    {
        private readonly PlanSettings _settings;
        private readonly List<Item> _itemList;
        private readonly Dictionary<string, int[]> _demand;
        private readonly Dictionary<string, int> _initialStock;

        public FenceTrimmer(
            PlanSettings settings,
            List<Item> itemList,
            Dictionary<string, int[]> demand,
            Dictionary<string, int> initialStock)
        {
            _settings = settings;
            _itemList = itemList;
            _demand = demand;
            _initialStock = initialStock;
        }

        public void Trim(
            Dictionary<string, int> allocated,
            Dictionary<string, int> cumPlan,
            int fence)
        {
            int shiftStart = _settings.GetShiftStart(fence);
            int shiftEnd = _settings.GetShiftEnd(fence);

            AppLogger.Section($"시프트 마지막 펜스 최소생산 적용 " +
                $"(펜스{shiftStart + 1}~{shiftEnd + 1})");

            foreach (var item in _itemList)
            {
                // 시프트 내 누적소요 (원본 소요량 기준)
                int shiftCumDemand = 0;
                for (int f = 0; f <= shiftEnd; f++)
                    shiftCumDemand += _demand[item.Code][f];

                int stock = _initialStock.ContainsKey(item.Code)
                    ? _initialStock[item.Code] : 0;

                // 최소 필요 생산량
                int minNeeded = Math.Max(0,
                    (shiftCumDemand - stock)
                    - cumPlan[item.Code]);

                // QPC 올림 (0이면 0 유지)
                int finalQty = (_settings.UseQPC && minNeeded > 0)
                    ? _settings.ApplyQPC(minNeeded)
                    : minNeeded;

                if (allocated[item.Code] > finalQty)
                {
                    AppLogger.Log($"{item.Code} 초과 제거: " +
                        $"{allocated[item.Code]} → {finalQty}");
                    allocated[item.Code] = finalQty;
                }
                else if (allocated[item.Code] < finalQty)
                {
                    AppLogger.Log($"{item.Code} 부족 추가: " +
                        $"{allocated[item.Code]} → {finalQty}");
                    allocated[item.Code] = finalQty;
                }
                else
                {
                    AppLogger.Log($"{item.Code} 유지: {finalQty}대");
                }
            }

            int trimTotal = allocated.Values.Sum();
            int fenceTarget = _settings.GetFenceTarget(fence);
            AppLogger.Info($"시프트 마지막 펜스 합계: {trimTotal}대 " +
                $"(절약: {fenceTarget - trimTotal}대)");
        }
    }
}