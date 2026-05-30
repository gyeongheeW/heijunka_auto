using System;
using System.Collections.Generic;
using System.Linq;
using heijunka.Models;

namespace heijunka.Planning
{
    public class PullForwarder
    {
        private readonly PlanSettings _settings;
        private readonly List<Item> _itemList;
        private readonly Dictionary<string, int[]> _remainingByFence;

        public PullForwarder(
            PlanSettings settings,
            List<Item> itemList,
            Dictionary<string, int[]> remainingByFence)
        {
            _settings = settings;
            _itemList = itemList;
            _remainingByFence = remainingByFence;
        }

        public void Pull(
            Dictionary<string, int> allocated,
            int fence)
        {
            int shiftEnd = _settings.GetShiftEnd(fence);
            int slack = _settings.GetFenceTarget(fence)
                       - allocated.Values.Sum();

            AppLogger.Info($"펜스{fence + 1} 선행생산 여유: {slack}대 " +
                $"(시프트 내 펜스{fence + 2}~{shiftEnd + 1}까지)");

            if (slack <= 0) return;

            for (int nextFence = fence + 1;
                 nextFence <= shiftEnd && slack > 0;
                 nextFence++)
            {
                // 비가동 펜스 스킵
                if (_settings.GetFenceTarget(nextFence) == 0)
                    continue;

                var pullOrder = _itemList
                    .OrderByDescending(i =>
                        _remainingByFence[i.Code][nextFence])
                    .ToList();

                foreach (var item in pullOrder)
                {
                    if (slack <= 0) break;

                    int remaining =
                        _remainingByFence[item.Code][nextFence];

                    if (remaining <= 0) continue;

                    int canPull = Math.Min(slack, remaining);

                    // QPC 내림 적용
                    int pull = _settings.UseQPC
                        ? _settings.ApplyQPCDown(canPull)
                        : canPull;

                    if (pull <= 0) continue;

                    allocated[item.Code] += pull;
                    _remainingByFence[item.Code][nextFence]
                        -= pull;
                    slack -= pull;

                    AppLogger.Log($"{item.Code} 선행생산 " +
                        $"펜스{nextFence + 1}→{fence + 1}: " +
                        $"+{pull}대 " +
                        $"(잔여:" +
                        $"{_remainingByFence[item.Code][nextFence]})");
                }
            }

            if (slack > 0)
                AppLogger.Warn($"펜스{fence + 1} 선행생산 후 " +
                    $"잔여 여유: {slack}대 " +
                    $"(시프트 내 소요량 소진)");
        }
    }
}