using System;
using System.Collections.Generic;
using System.Linq;
using heijunka.Models;

namespace heijunka.Planning
{
    public class Planner
    {
        private readonly PlanSettings _settings;
        private readonly Dictionary<string, Item> _items;
        private readonly Dictionary<string, int[]> _demand;
        private readonly Dictionary<string, int> _initialStock;

        private List<Item> _itemList = new();
        public int[,] Plan { get; private set; } = new int[0, 0];

        private NetDemandCalc _netDemandCalc = null!;
        private Allocator _allocator = null!;
        private FenceTrimmer _fenceTrimmer = null!;

        public Planner(
            PlanSettings settings,
            Dictionary<string, Item> items,
            Dictionary<string, int[]> demand,
            Dictionary<string, int> initialStock)
        {
            _settings = settings;
            _items = items;
            _demand = demand;
            _initialStock = initialStock;
        }

        public void Run()
        {
            // LP 모드
            if (_settings.PlanMode == "LP")
            {
                var lpPlanner = new LpPlanner(
                    _settings, _items, _demand, _initialStock);
                lpPlanner.Run();
                Plan = lpPlanner.Plan;
                return;
            }

            // 휴리스틱 모드
            AppLogger.Section("타임펜스 계획 시작");

            _itemList = _items.Values.ToList();
            Plan = new int[_itemList.Count, _settings.FenceCount];

            AppLogger.Info($"TARGET: {_settings.Target}대 / " +
                $"시프트수: {_settings.ShiftCount} / " +
                $"펜스수: {_settings.FenceCount} / " +
                $"품목수: {_itemList.Count}");

            _netDemandCalc = new NetDemandCalc(
                _settings, _itemList, _demand, _initialStock);
            _allocator = new Allocator(_settings, _itemList);
            _fenceTrimmer = new FenceTrimmer(
                _settings, _itemList, _demand, _initialStock);

            // 초기 NetDemand 테이블 출력
            _netDemandCalc.PrintNetDemandTable();

            // remaining 초기화 (펜스별 소요량)
            var remainingByFence =
                new Dictionary<string, int[]>();
            foreach (var item in _items.Values)
                remainingByFence[item.Code] =
                    (int[])_demand[item.Code].Clone();

            var cumPlan = new Dictionary<string, int>();
            foreach (var item in _itemList)
                cumPlan[item.Code] = 0;

            for (int fence = 0;
                 fence < _settings.FenceCount; fence++)
            {
                int shiftIdx = _settings.GetShiftIndex(fence);
                bool isLastOfShift =
                    _settings.IsLastFenceOfShift(fence);
                int fenceTarget =
                    _settings.GetFenceTarget(fence);

                AppLogger.Section(
                    $"시프트{shiftIdx + 1} / " +
                    $"펜스{fence + 1} 계획" +
                    (isLastOfShift ? " [시프트 마지막]" : "") +
                    (fenceTarget == 0 ? " [비가동]" : ""));

                // ── 비가동 펜스 ───────────────────────
                if (fenceTarget == 0)
                {
                    AppLogger.Info(
                        $"펜스{fence + 1} 비가동 → 생산 없음");
                    for (int i = 0; i < _itemList.Count; i++)
                        Plan[i, fence] = 0;
                    continue;
                }

                // ── 시프트 NetDemand 계산 ──────────────
                int shiftProduced = CalcShiftProduced(fence, cumPlan);
                var shiftNet = _netDemandCalc
    .CalcShiftNet(fence, cumPlan,
        shiftProduced, remainingByFence);


                // 시프트 Net 잔량 (전체)
                // = shiftNet 합계 - 시프트 누적생산
                int totalShiftNetRemaining =
                    shiftNet.Values.Sum() - shiftProduced;

                AppLogger.Info(
                    $"시프트 NetDemand 합계: " +
                    $"{shiftNet.Values.Sum()}대 / " +
                    $"시프트 생산: {shiftProduced}대 / " +
                    $"잔여: {totalShiftNetRemaining}대");

                // ── Shortage 경고 ─────────────────────
                if (totalShiftNetRemaining > fenceTarget * 1.2)
                {
                    AppLogger.Warn(
                        $"펜스{fence + 1} Shortage 경고: " +
                        $"잔여 {totalShiftNetRemaining}대 > " +
                        $"TARGET {fenceTarget}대 × 1.2");
                }

                // ── 마지막 펜스 아닌 경우 잔여 확인 ────
                if (!isLastOfShift &&
                    totalShiftNetRemaining <= 0)
                {
                    AppLogger.Info(
                        $"펜스{fence + 1} 시프트 소요 충족 " +
                        $"→ 생산 없음");
                    for (int i = 0; i < _itemList.Count; i++)
                        Plan[i, fence] = 0;
                    continue;
                }

                // ── Step1,2: 배분 ─────────────────────
                var netDemand =
                    _netDemandCalc.Calc(cumPlan, fence);

                var allocated = _allocator.Allocate(
                    netDemand, shiftNet, fence);

                // ── Step3~5: QPC 채우기 ───────────────
                if (!isLastOfShift)
                {
                    FillByQPC(
                        allocated, shiftNet,
                        cumPlan, remainingByFence,
                        fence, fenceTarget,
                        totalShiftNetRemaining);
                }

                // ── 마지막 펜스 최소생산 ──────────────
                if (isLastOfShift)
                    _fenceTrimmer.Trim(
                        allocated, cumPlan, fence);

                // ── 품목별 상태 로그 ──────────────────
                LogShiftStatus(
                    shiftNet, cumPlan,
                    allocated, fence);

                // ── 결과 저장 ─────────────────────────
                for (int i = 0; i < _itemList.Count; i++)
                {
                    Plan[i, fence] =
                        allocated[_itemList[i].Code];
                    cumPlan[_itemList[i].Code] +=
                        allocated[_itemList[i].Code];

                    remainingByFence[
                        _itemList[i].Code][fence] =
                        Math.Max(0,
                        remainingByFence[
                            _itemList[i].Code][fence]
                        - allocated[_itemList[i].Code]);
                }

                int fenceTotal = allocated.Values.Sum();
                AppLogger.Info(
                    $"펜스{fence + 1} 합계: {fenceTotal}대 " +
                    $"(목표: {fenceTarget}대)" +
                    (isLastOfShift ? " [최소생산]" : ""));
            }

            AppLogger.Section("타임펜스 계획 완료");
        }

        // ── QPC 채우기 ────────────────────────────────
        private void FillByQPC(
            Dictionary<string, int> allocated,
            Dictionary<string, int> shiftNet,
            Dictionary<string, int> cumPlan,
            Dictionary<string, int[]> remainingByFence,
            int fence,
            int fenceTarget,
            int totalShiftNetRemaining)
        {
            int current = allocated.Values.Sum();
            if (current >= fenceTarget) return;
            if (current >= totalShiftNetRemaining) return;

            int shiftEnd = _settings.GetShiftEnd(fence);
            int add = _settings.UseQPC ? _settings.QPC : 1;

            bool anyAdded = true;
            while (current < fenceTarget &&
                   current < totalShiftNetRemaining &&
                   anyAdded)
            {
                anyAdded = false;

                // 품목별 시프트Net잔량 기준 정렬
                // 잔량 = shiftNet - cumPlan - allocated
                var candidates = _itemList
                    .Where(i =>
                    {
                        int itemRemaining =
                            shiftNet[i.Code]
                            - cumPlan[i.Code]
                            - allocated[i.Code];
                        return itemRemaining > 0;
                    })
                    .OrderByDescending(i =>
                        shiftNet[i.Code]
                        - cumPlan[i.Code]
                        - allocated[i.Code])
                    .ToList();

                if (candidates.Count == 0) break;

                foreach (var item in candidates)
                {
                    if (current >= fenceTarget) break;
                    if (current >= totalShiftNetRemaining)
                        break;

                    // 다음~이후 펜스에 수량 있는지 확인
                    int nextFenceWithDemand = -1;
                    for (int f = fence + 1;
                         f <= shiftEnd; f++)
                    {
                        if (remainingByFence[item.Code][f] > 0)
                        {
                            nextFenceWithDemand = f;
                            break;
                        }
                    }

                    if (nextFenceWithDemand < 0) continue;

                    // UPH 초과 체크
                    if (current + add > fenceTarget)
                    {
                        AppLogger.Warn(
                            $"펜스{fence + 1} " +
                            $"UPH 초과 → 채우기 중단");
                        return;
                    }

                    // 1 QPC 추가
                    allocated[item.Code] += add;
                    current += add;
                    anyAdded = true;

                    // 다음 펜스 remaining 차감
                    remainingByFence[item.Code][
                        nextFenceWithDemand] =
                        Math.Max(0,
                        remainingByFence[item.Code][
                            nextFenceWithDemand] - add);

                    AppLogger.Log(
                        $"{item.Code} QPC 채움: +{add}대 " +
                        $"(펜스{nextFenceWithDemand + 1}" +
                        $"에서 당김) " +
                        $"(합계: {current}/{fenceTarget})");
                }
            }
        }

        // ── 시프트 누적생산 ───────────────────────────
        private int CalcShiftProduced(
            int fence,
            Dictionary<string, int> cumPlan)
        {
            int shiftStart = _settings.GetShiftStart(fence);

            // 시프트 시작 전 누적생산
            int prevShiftCumPlan = 0;
            if (shiftStart > 0)
            {
                // 이전 시프트 마지막 펜스까지 생산량
                for (int i = 0; i < _itemList.Count; i++)
                    for (int f = 0; f < shiftStart; f++)
                        prevShiftCumPlan += Plan[i, f];
            }

            // 전체 누적생산 - 이전 시프트 생산
            int totalCumPlan = cumPlan.Values.Sum();
            return totalCumPlan - prevShiftCumPlan;
        }

        // ── 시프트 품목별 상태 로그 ───────────────────
        private void LogShiftStatus(
            Dictionary<string, int> shiftNet,
            Dictionary<string, int> cumPlan,
            Dictionary<string, int> allocated,
            int fence)
        {
            AppLogger.Section(
                $"시프트 품목별 상태 (펜스{fence + 1})");
            AppLogger.Info(
                $"{"품목",-8} " +
                $"{"펜스기초",8} " +
                $"{"펜스Net",8} " +
                $"{"펜스생산",8} " +
                $"{"시프트Net잔량",12} " +
                $"{"누적생산",8}");

            foreach (var item in _itemList)
            {
                // 펜스 기초재고
                int fenceStock = _netDemandCalc.GetFenceStock(
                    item.Code, fence, cumPlan);

                // 펜스 NetDemand
                int fenceNet = _netDemandCalc.CalcFenceNet(
                    item.Code, fence, cumPlan);

                // 펜스 생산량
                int fencePlan =
                    allocated.ContainsKey(item.Code)
                    ? allocated[item.Code] : 0;

                // 시프트Net잔량
                // = shiftNet - cumPlan - allocated
                int shiftNetRemaining =
                    shiftNet[item.Code]
                    - cumPlan[item.Code]
                    - fencePlan;

                // 누적생산 (저장 전)
                int cum = cumPlan[item.Code] + fencePlan;

                AppLogger.Info(
                    $"{item.Code,-8} " +
                    $"{fenceStock,8} " +
                    $"{fenceNet,8} " +
                    $"{fencePlan,8} " +
                    $"{shiftNetRemaining,12} " +
                    $"{cum,8}");
            }
        }
    }
}