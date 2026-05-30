using System;
using System.Collections.Generic;
using System.Linq;
using heijunka.Models;

namespace heijunka.Planning
{
    public class NetDemandCalc
    {
        private readonly PlanSettings _settings;
        private readonly List<Item> _itemList;
        private readonly Dictionary<string, int[]> _demand;
        private readonly Dictionary<string, int> _initialStock;

        public NetDemandCalc(
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

        // ── 펜스 소요량 ───────────────────────────────
        // 현재 펜스 + 다음 비가동 펜스들 소요 합산
        public int GetFenceDemand(string code, int fence)
        {
            int sum = _demand[code][fence];
            for (int f = fence + 1;
                 f < _settings.FenceCount; f++)
            {
                if (_settings.FenceActive.Length > f &&
                    _settings.FenceActive[f] == 1)
                    break;
                sum += _demand[code][f];
            }
            return sum;
        }

        // ── 펜스 기초재고 ─────────────────────────────
        // 전 펜스 기말재고 (펜스0은 기초재고)
        public int GetFenceStock(
            string code,
            int fence,
            Dictionary<string, int> cumPlan)
        {
            if (fence == 0)
                return _initialStock.ContainsKey(code)
                    ? _initialStock[code] : 0;

            // 전 펜스 기말
            // = 기초재고 + 이전누적생산 - 이전누적소요
            int stock = _initialStock.ContainsKey(code)
                ? _initialStock[code] : 0;

            int cumDemand = 0;
            for (int f = 0; f < fence; f++)
                cumDemand += _demand[code][f];

            return Math.Max(0,
                stock + cumPlan[code] - cumDemand);
        }

        // ── 펜스 Net ──────────────────────────────────
        // 펜스 기초 - 펜스 소요량
        public int GetFenceNet(
            string code,
            int fence,
            Dictionary<string, int> cumPlan)
        {
            int fenceStock = GetFenceStock(
                code, fence, cumPlan);
            int fenceDemand = GetFenceDemand(code, fence);
            return Math.Max(0, fenceStock - fenceDemand);
        }

        // ── 시프트 기초재고 ───────────────────────────
        // 시프트1 = 기초재고
        // 시프트N = 이전 시프트 기말재고
        public int GetShiftStock(
            string code,
            int fence,
            Dictionary<string, int> cumPlan)
        {
            int shiftIdx = _settings.GetShiftIndex(fence);

            if (shiftIdx == 0)
                return _initialStock.ContainsKey(code)
                    ? _initialStock[code] : 0;

            // 이전 시프트 마지막 펜스 기말
            int prevShiftEnd =
                _settings.GetShiftStart(fence) - 1;

            int stock = _initialStock.ContainsKey(code)
                ? _initialStock[code] : 0;

            int cumDemand = 0;
            for (int f = 0; f <= prevShiftEnd; f++)
                cumDemand += _demand[code][f];

            return Math.Max(0,
                stock + cumPlan[code] - cumDemand);
        }

        // ── 시프트 Net 잔량 (품목별) ──────────────────
        // 시프트 소요량 - 시프트 기초재고
        // + 안전재고 - 시프트 누적생산
        public int GetShiftNetRemaining(
            string code,
            int fence,
            Dictionary<string, int> cumPlan,
            Dictionary<string, int> allocated)
        {
            int shiftStart = _settings.GetShiftStart(fence);
            int shiftEnd = _settings.GetShiftEnd(fence);

            // 시프트 기초재고
            int shiftStock = GetShiftStock(
                code, fence, cumPlan);

            // 시프트 전체 소요량
            int shiftDemand = 0;
            for (int f = shiftStart; f <= shiftEnd; f++)
                shiftDemand += _demand[code][f];

            // 시프트 누적생산
            // = cumPlan에서 시프트 시작 이전 제외
            int prevShiftCum = 0;
            for (int f = 0; f < shiftStart; f++)
                prevShiftCum += 0; // 별도 추적 필요

            // 현재 시프트 내 누적생산
            // cumPlan은 전체 누적이라
            // 시프트 기초재고에 이미 반영됨
            // → 시프트Net잔량 = 시프트소요 - 시프트기초
            //                  + SS - 현재할당
            var item = _itemList
                .FirstOrDefault(i => i.Code == code);
            int ss = item?.SafetyStock ?? 0;

            int remaining =
                shiftDemand
                - shiftStock
                + ss
                - (allocated.ContainsKey(code)
                    ? allocated[code] : 0);

            return Math.Max(0, remaining);
        }

        // ── 펜스별 NetDemand 계산 ─────────────────────
        public Dictionary<string, int> Calc(
            Dictionary<string, int> cumPlan,
            int fence)
        {
            var netDemand = new Dictionary<string, int>();

            foreach (var item in _itemList)
            {
                int fenceDemand =
                    GetFenceDemand(item.Code, fence);
                int fenceStock =
                    GetFenceStock(item.Code, fence, cumPlan);

                // 펜스 Net = 소요량 - 기초재고
                // (기초재고가 소요량보다 많으면 0)
                int net = Math.Max(0,
                    fenceDemand - fenceStock);

                netDemand[item.Code] = net;

                AppLogger.Log(
                    $"{item.Code} 펜스Net: {net} " +
                    $"(소요:{fenceDemand} " +
                    $"기초:{fenceStock})");
            }

            int totalNet = netDemand.Values.Sum();
            if (totalNet == 0)
                AppLogger.Warn(
                    $"펜스{fence + 1} 전체 실소요 0");

            return netDemand;
        }

        // ── 시프트 NetDemand 계산 ─────────────────────
        public Dictionary<string, int> CalcShiftNet(
    int fence,
    Dictionary<string, int> cumPlan,
    int shiftProduced,
    Dictionary<string, int[]> remainingByFence)
        {
            var shiftNet = new Dictionary<string, int>();
            int shiftStart = _settings.GetShiftStart(fence);
            int shiftEnd = _settings.GetShiftEnd(fence);

            AppLogger.Section("시프트 NetDemand 계산");

            foreach (var item in _itemList)
            {
                int shiftStock = GetShiftStock(
                    item.Code, fence, cumPlan);

                // 원본 소요량 기준 (GetFenceDemand)
                int shiftDemand = 0;
                for (int f = shiftStart; f <= shiftEnd; f++)
                {
                    if (_settings.FenceActive.Length > f &&
                        _settings.FenceActive[f] == 0) continue;
                    shiftDemand += GetFenceDemand(item.Code, f);
                }

                // 이전 시프트 누적소요
                int prevDemand = 0;
                for (int f = 0; f < shiftStart; f++)
                    prevDemand += _demand[item.Code][f];

                int initialStock = _initialStock.ContainsKey(item.Code)
                    ? _initialStock[item.Code] : 0;

                // 이전 시프트 누적생산
                int prevCumPlan = Math.Max(0,
                    shiftStock - initialStock + prevDemand);

                // 현재 시프트 생산량
                int itemShiftProduced = Math.Max(0,
                    cumPlan[item.Code] - prevCumPlan);

                int net = Math.Max(0,
                    shiftDemand
                    - shiftStock
                    + item.SafetyStock
                    - itemShiftProduced);

                shiftNet[item.Code] = net;

                AppLogger.Log(
                    $"{item.Code} 시프트Net: {net} " +
                    $"(시프트NetDemand:{shiftDemand} " +
                    $"시프트기초:{shiftStock} " +
                    $"SS:{item.SafetyStock} " +
                    $"시프트생산:{itemShiftProduced})");
            }

            AppLogger.Info($"시프트 NetDemand 합계: " +
                $"{shiftNet.Values.Sum()}대");

            return shiftNet;
        }

        // ── 초기 NetDemand 테이블 출력 ────────────────
        public void PrintNetDemandTable()
        {
            AppLogger.Section("펜스-품목별 NetDemand 테이블");

            var header = $"{"품목",-8}";
            for (int f = 0; f < _settings.FenceCount; f++)
                header += $"\t펜스{f + 1}";
            AppLogger.Info(header);

            var zeroCumPlan = new Dictionary<string, int>();
            foreach (var item in _itemList)
                zeroCumPlan[item.Code] = 0;

            foreach (var item in _itemList)
            {
                var line = $"{item.Code,-8}";
                for (int f = 0;
                     f < _settings.FenceCount; f++)
                {
                    if (_settings.FenceActive.Length > f &&
                        _settings.FenceActive[f] == 0)
                    {
                        line += "\t0(비가동)";
                        continue;
                    }

                    int fenceDemand = GetFenceDemand(
                        item.Code, f);
                    int fenceStock = GetFenceStock(
                        item.Code, f, zeroCumPlan);
                    int net = Math.Max(0,
                        fenceDemand - fenceStock);
                    line += $"\t{net}";
                }
                AppLogger.Info(line);
            }
        }

        // ── 외부 조회용 ───────────────────────────────
        public int CalcFenceNet(
            string code,
            int fence,
            Dictionary<string, int> cumPlan)
        {
            return GetFenceNet(code, fence, cumPlan);
        }
    }
}