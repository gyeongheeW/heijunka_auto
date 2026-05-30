using System;
using System.Collections.Generic;
using heijunka.Models;

namespace heijunka.Sequencing
{
    public class OrderGenerator
    {
        private readonly PlanSettings _settings;
        private readonly Dictionary<string, Item> _items;

        public OrderGenerator(
            PlanSettings settings,
            Dictionary<string, Item> items)
        {
            _settings = settings;
            _items = items;
        }

        // ── 전체 펜스 오더 생성 ────────────────────────
        public List<List<Order>> GenerateAll(int[,] plan)
        {
            var itemList = new List<Item>(_items.Values);
            var result = new List<List<Order>>();

            AppLogger.Section("오더 생성 시작");

            for (int fence = 0; fence < _settings.FenceCount; fence++)
            {
                var orders = GenerateFence(plan, itemList, fence);
                result.Add(orders);
                AppLogger.Info($"펜스{fence + 1} 오더수: {orders.Count}개");
            }

            AppLogger.Section("오더 생성 완료");
            return result;
        }

        // ── 펜스별 오더 생성 ───────────────────────────
        private List<Order> GenerateFence(
            int[,] plan,
            List<Item> itemList,
            int fence)
        {
            var orders = new List<Order>();

            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];

                // 범위 체크
                if (i >= plan.GetLength(0) ||
                    fence >= plan.GetLength(1)) continue;

                int qty = plan[i, fence];
                if (qty <= 0) continue;

                // 기준정보 없으면 스킵
                if (!_items.ContainsKey(item.Code))
                {
                    AppLogger.Warn($"{item.Code} 기준정보 없음 → 스킵");
                    continue;
                }

                // Max Order Qty 적용 (시퀀싱에서만)
                if (_settings.UseMaxOrderQty &&
                    qty > _settings.MaxOrderQty)
                {
                    // 쪼개기
                    int remaining = qty;
                    while (remaining > 0)
                    {
                        int orderQty = Math.Min(
                            remaining, _settings.MaxOrderQty);
                        orders.Add(new Order(
                            item.Code, orderQty,
                            item.Classifications));
                        remaining -= orderQty;
                        AppLogger.Log($"{item.Code} 오더분할: " +
                            $"{orderQty}대");
                    }
                }
                else
                {
                    orders.Add(new Order(
                        item.Code, qty,
                        item.Classifications));
                    AppLogger.Log($"{item.Code} 오더생성: {qty}대");
                }
            }

            return orders;
        }
    }
}