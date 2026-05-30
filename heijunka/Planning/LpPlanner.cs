using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.LinearSolver;
using heijunka.Models;

namespace heijunka.Planning
{
    public class LpPlanner
    {
        private readonly PlanSettings _settings;
        private readonly Dictionary<string, Item> _items;
        private readonly Dictionary<string, int[]> _demand;
        private readonly Dictionary<string, int> _initialStock;

        private List<Item> _itemList = new();
        public int[,] Plan { get; private set; } = new int[0, 0];

        public LpPlanner(
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
            AppLogger.Section("LP 타임펜스 계획 시작");

            _itemList = _items.Values.ToList();
            int itemCount = _itemList.Count;
            int fenceCount = _settings.FenceCount;

            Plan = new int[itemCount, fenceCount];

            // ── 전체 소요량 비율 계산 ──────────────────
            var totalDemand = new Dictionary<string, int>();
            foreach (var item in _itemList)
                totalDemand[item.Code] =
                    _demand[item.Code].Sum();

            int grandTotal = totalDemand.Values.Sum();
            if (grandTotal == 0)
            {
                AppLogger.Warn("전체 소요량 0 → LP 중단");
                return;
            }

            var targetRatio = new Dictionary<string, double>();
            foreach (var item in _itemList)
                targetRatio[item.Code] =
                    (double)totalDemand[item.Code] / grandTotal;

            AppLogger.Info($"품목수: {itemCount} / " +
                $"펜스수: {fenceCount}");

            // ── OR-Tools MIP 솔버 ──────────────────────
            var solver = Solver.CreateSolver("SCIP");
            if (solver == null)
            {
                AppLogger.Warn("솔버 생성 실패");
                return;
            }

            // ── 변수 생성 ──────────────────────────────
            // plan[i,f] = 품목i 펜스f 생산량
            var planVar = new Variable[itemCount, fenceCount];
            for (int i = 0; i < itemCount; i++)
            {
                for (int f = 0; f < fenceCount; f++)
                {
                    int fenceTarget =
                        _settings.GetFenceTarget(f);

                    if (fenceTarget == 0)
                    {
                        // 비가동 펜스 → 0으로 고정
                        planVar[i, f] = solver.MakeIntVar(
                            0, 0,
                            $"plan_{i}_{f}");
                    }
                    else
                    {
                        // QPC 단위로 변수 생성
                        int maxVal = _settings.UseQPC
                            ? fenceTarget / _settings.QPC
                            : fenceTarget;

                        planVar[i, f] = solver.MakeIntVar(
                            0, maxVal,
                            $"plan_{i}_{f}");
                    }
                }
            }

            // ── 제약조건 1: 펜스별 합계 ≤ TARGET ────────
            for (int f = 0; f < fenceCount; f++)
            {
                int fenceTarget = _settings.GetFenceTarget(f);
                if (fenceTarget == 0) continue;

                var ct = solver.MakeConstraint(
                    0, _settings.UseQPC
                        ? fenceTarget / _settings.QPC
                        : fenceTarget,
                    $"fence_target_{f}");

                for (int i = 0; i < itemCount; i++)
                    ct.SetCoefficient(planVar[i, f], 1);
            }

            // ── 제약조건 2: 누적생산 ≥ 누적소요 - 기초재고
            for (int i = 0; i < itemCount; i++)
            {
                var item = _itemList[i];
                int stock = _initialStock.ContainsKey(item.Code)
                    ? _initialStock[item.Code] : 0;

                int cumDemand = 0;
                for (int f = 0; f < fenceCount; f++)
                {
                    cumDemand += _demand[item.Code][f];

                    // 비가동 펜스 스킵
                    if (_settings.GetFenceTarget(f) == 0)
                        continue;

                    int minProd = Math.Max(0,
                        cumDemand - stock);

                    int minQPC = _settings.UseQPC
                        ? (int)Math.Ceiling(
                            (double)minProd / _settings.QPC)
                        : minProd;

                    var ct = solver.MakeConstraint(
                        minQPC,
                        double.PositiveInfinity,
                        $"cum_demand_{i}_{f}");

                    for (int f2 = 0; f2 <= f; f2++)
                        ct.SetCoefficient(planVar[i, f2], 1);
                }
            }

            // ── 제약조건 3: 안전재고 ≥ SafetyStock ──────
            for (int i = 0; i < itemCount; i++)
            {
                var item = _itemList[i];
                if (item.SafetyStock <= 0) continue;

                int stock = _initialStock.ContainsKey(item.Code)
                    ? _initialStock[item.Code] : 0;

                int totalItemDemand =
                    _demand[item.Code].Sum();

                int minTotal = Math.Max(0,
                    totalItemDemand - stock
                    + item.SafetyStock);

                int minQPC = _settings.UseQPC
                    ? (int)Math.Ceiling(
                        (double)minTotal / _settings.QPC)
                    : minTotal;

                var ct = solver.MakeConstraint(
                    minQPC,
                    double.PositiveInfinity,
                    $"safety_{i}");

                for (int f = 0; f < fenceCount; f++)
                    ct.SetCoefficient(planVar[i, f], 1);
            }

            // ── 목적함수: 비율 편차 최소화 ───────────────
            // min Σ_f Σ_i (plan[i,f] - TARGET × ratio[i])²
            // OR-Tools Linear는 선형만 지원
            // → 편차의 절대값 최소화 (L1 근사)

            // 편차 변수 추가
            var devVar = new Variable[itemCount, fenceCount];
            for (int i = 0; i < itemCount; i++)
            {
                for (int f = 0; f < fenceCount; f++)
                {
                    int fenceTarget =
                        _settings.GetFenceTarget(f);
                    if (fenceTarget == 0) continue;

                    devVar[i, f] = solver.MakeNumVar(
                        0, double.PositiveInfinity,
                        $"dev_{i}_{f}");

                    double idealRatio = targetRatio[
                        _itemList[i].Code];
                    double idealQty = _settings.UseQPC
                        ? idealRatio * fenceTarget / _settings.QPC
                        : idealRatio * fenceTarget;

                    // dev >= plan - ideal
                    var ct1 = solver.MakeConstraint(
                        -idealQty,
                        double.PositiveInfinity,
                        $"dev_pos_{i}_{f}");
                    ct1.SetCoefficient(devVar[i, f], 1);
                    ct1.SetCoefficient(planVar[i, f], -1);

                    // dev >= ideal - plan
                    var ct2 = solver.MakeConstraint(
                        idealQty,
                        double.PositiveInfinity,
                        $"dev_neg_{i}_{f}");
                    ct2.SetCoefficient(devVar[i, f], 1);
                    ct2.SetCoefficient(planVar[i, f], 1);
                }
            }

            // 목적함수: 편차 합 최소화
            var objective = solver.Objective();
            for (int i = 0; i < itemCount; i++)
                for (int f = 0; f < fenceCount; f++)
                {
                    if (_settings.GetFenceTarget(f) == 0)
                        continue;
                    if (devVar[i, f] == null) continue;
                    objective.SetCoefficient(devVar[i, f], 1);
                }
            objective.SetMinimization();

            // ── 솔버 실행 ──────────────────────────────
            AppLogger.Info("LP 솔버 실행 중...");
            var status = solver.Solve();

            if (status != Solver.ResultStatus.OPTIMAL &&
                status != Solver.ResultStatus.FEASIBLE)
            {
                AppLogger.Warn($"LP 솔버 실패: {status}");
                return;
            }

            AppLogger.Info($"LP 솔버 완료: {status}");

            // ── 결과 저장 ──────────────────────────────
            for (int i = 0; i < itemCount; i++)
            {
                for (int f = 0; f < fenceCount; f++)
                {
                    int val = _settings.UseQPC
                        ? (int)planVar[i, f].SolutionValue()
                          * _settings.QPC
                        : (int)planVar[i, f].SolutionValue();

                    Plan[i, f] = val;
                }

                // 로그
                var item = _itemList[i];
                int total = 0;
                for (int f = 0; f < fenceCount; f++)
                    total += Plan[i, f];
                AppLogger.Log($"{item.Code}: 총 {total}대 " +
                    $"(목표비율: " +
                    $"{targetRatio[item.Code]:P1})");
            }

            // 펜스별 합계 로그
            for (int f = 0; f < fenceCount; f++)
            {
                int sum = 0;
                for (int i = 0; i < itemCount; i++)
                    sum += Plan[i, f];
                AppLogger.Info($"펜스{f + 1} 합계: {sum}대 " +
                    $"(목표: {_settings.GetFenceTarget(f)}대)");
            }

            AppLogger.Section("LP 타임펜스 계획 완료");
        }
    }
}