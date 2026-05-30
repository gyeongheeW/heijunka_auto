using System;
using System.Collections.Generic;
using System.Linq;
using heijunka.Models;

namespace heijunka.Planning
{
    public class FulfillmentChecker
    {
        private readonly PlanSettings _settings;
        private readonly List<Item> _itemList;
        private readonly Dictionary<string, int[]> _demand;
        private readonly Dictionary<string, int> _initialStock;

        public FulfillmentChecker(
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

        public void Check(
            Dictionary<string, int> allocated,
            Dictionary<string, int> cumPlan,
            int fence)
        {
            int fenceTarget = _settings.GetFenceTarget(fence);

            foreach (var item in _itemList)
            {
                int stock = _initialStock.ContainsKey(item.Code)
                    ? _initialStock[item.Code] : 0;

                int safetyStock = item.SafetyStock;

                // ── 펜스별 기말재고 계산 ───────────────
                // 기초재고 + 누적생산 - 누적소요
                int cumDemand = 0;
                for (int f = 0; f <= fence; f++)
                    cumDemand += _demand[item.Code][f];

                int cumProduced =
                    cumPlan[item.Code] + allocated[item.Code];

                int endingInventory =
                    stock + cumProduced - cumDemand;

                // ── 기말재고 음수 방지 ─────────────────
                if (endingInventory < 0)
                {
                    int shortage = -endingInventory;

                    // QPC 올림
                    int addQty = _settings.UseQPC
                        ? _settings.ApplyQPC(shortage)
                        : shortage;

                    AppLogger.Warn($"{item.Code} 기말재고 음수: " +
                        $"{endingInventory} → 강제 증산 {addQty}대");

                    allocated[item.Code] += addQty;

                    // TARGET 초과 시 다른 품목 차감
                    int overflow =
                        allocated.Values.Sum() - fenceTarget;
                    if (overflow > 0)
                        ReduceOverflow(
                            allocated, cumPlan, fence,
                            item.Code, overflow);
                }
                // ── 안전재고 미충족 (소프트) ───────────
                else if (endingInventory < safetyStock)
                {
                    AppLogger.Log($"{item.Code} 안전재고 미충족: " +
                        $"기말{endingInventory} < SS{safetyStock} " +
                        $"→ 허용");
                }
                else
                {
                    AppLogger.Log($"{item.Code} ✓ " +
                        $"기말:{endingInventory} " +
                        $"(SS:{safetyStock})");
                }
            }
        }

        // ── 기말재고 계산 헬퍼 ────────────────────────
        private int CalcEndingInventory(
            string code,
            int fence,
            Dictionary<string, int> cumPlan,
            Dictionary<string, int> allocated)
        {
            int stock = _initialStock.ContainsKey(code)
                ? _initialStock[code] : 0;

            int cumDemand = 0;
            for (int f = 0; f <= fence; f++)
                cumDemand += _demand[code][f];

            int cumProduced =
                cumPlan[code] + allocated[code];

            return stock + cumProduced - cumDemand;
        }

        // ── 초과분 차감 ───────────────────────────────
        private void ReduceOverflow(
            Dictionary<string, int> allocated,
            Dictionary<string, int> cumPlan,
            int fence,
            string skipCode,
            int overflow)
        {
            var reduceOrder = _itemList
                .Where(i => i.Code != skipCode
                    && allocated[i.Code] > 0)
                .OrderByDescending(i =>
                {
                    int cumD = 0;
                    for (int f = 0; f <= fence; f++)
                        cumD += _demand[i.Code][f];
                    int stock =
                        _initialStock.ContainsKey(i.Code)
                        ? _initialStock[i.Code] : 0;
                    return CalcEndingInventory(
                        i.Code, fence, cumPlan, allocated);
                })
                .ToList();

            int toReduce = overflow;
            foreach (var item in reduceOrder)
            {
                if (toReduce <= 0) break;

                // 기말재고가 0 이상 유지되는 범위에서 차감
                int ending = CalcEndingInventory(
                    item.Code, fence, cumPlan, allocated);

                // 안전재고 이상 유지
                int canReduceMax = Math.Max(0,
                    ending - item.SafetyStock);

                // QPC 배수로 차감
                int canReduce = _settings.UseQPC
                    ? _settings.ApplyQPCDown(
                        Math.Min(toReduce, canReduceMax))
                    : Math.Min(toReduce, canReduceMax);

                if (canReduce > 0)
                {
                    allocated[item.Code] -= canReduce;
                    toReduce -= canReduce;
                    AppLogger.Log($"{item.Code} 초과 차감: " +
                        $"-{canReduce}대");
                }
            }

            if (toReduce > 0)
                AppLogger.Warn($"TARGET 초과 해소 불가: " +
                    $"{toReduce}대 초과");
        }
    }
}