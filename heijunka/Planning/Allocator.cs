using System;
using System.Collections.Generic;
using System.Linq;
using heijunka.Models;

namespace heijunka.Planning
{
    public class Allocator
    {
        private readonly PlanSettings _settings;
        private readonly List<Item> _itemList;

        public Allocator(
            PlanSettings settings,
            List<Item> itemList)
        {
            _settings = settings;
            _itemList = itemList;
        }

        public Dictionary<string, int> Allocate(
            Dictionary<string, int> netDemand,
            Dictionary<string, int> shiftNet,
            int fence)
        {
            var allocated = new Dictionary<string, int>();
            foreach (var item in _itemList)
                allocated[item.Code] = 0;

            int fenceTarget = _settings.GetFenceTarget(fence);
            int current = 0;

            // Step1: shiftNet + SS 없는 품목 스킵
            // Step2: NetDemand 큰 순서로 정렬 후 배분
            var candidates = _itemList
                .Where(i =>
                {
                    int shiftRemaining =
                        shiftNet[i.Code] + i.SafetyStock;
                    return shiftRemaining > 0;
                })
                .OrderByDescending(i => netDemand[i.Code])
                .ToList();

            if (candidates.Count == 0)
            {
                AppLogger.Warn(
                    $"펜스{fence + 1} 생산 가능 품목 없음");
                return allocated;
            }

            // Step2: 품목별 QPC 내림 배분
            // 배분 중 UPH 초과 시 Shortage 에러
            foreach (var item in candidates)
            {
                if (netDemand[item.Code] <= 0) continue;

                // QPC 내림 배분
                int qty = _settings.UseQPC
                    ? _settings.ApplyQPCDown(
                        netDemand[item.Code])
                    : netDemand[item.Code];

                if (qty <= 0) continue;

                // 배분 후 합계 확인
                if (current + qty > fenceTarget)
                {
                    // TARGET 초과 → Shortage 에러
                    AppLogger.Error(
                        $"펜스{fence + 1} Shortage 발생: " +
                        $"{item.Code} 배분 시 합계 " +
                        $"{current + qty}대 > " +
                        $"TARGET {fenceTarget}대 → 중단");
                    break;
                }

                allocated[item.Code] = qty;
                current += qty;

                AppLogger.Log(
                    $"{item.Code} 배분: {qty}대 " +
                    $"(합계: {current}/{fenceTarget})");

                // TARGET 도달 시 중단
                if (current >= fenceTarget) break;
            }

            return allocated;
        }
    }
}