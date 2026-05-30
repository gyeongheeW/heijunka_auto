using System;
using System.Collections.Generic;
using System.Linq;
using heijunka.Models;

namespace heijunka.Sequencing
{
    public class Sequencer
    {
        private readonly PlanSettings _settings;
        private readonly Dictionary<string, Item> _items;
        private readonly Dictionary<(string, string), int> _transferTable;

        private readonly ScoreCalculator _scoreCalc;
        private readonly QualityScorer _qualityScorer;

        public List<List<Order>> Sequences { get; private set; } = new();
        public List<double[]> QualityScores { get; private set; } = new();

        public Sequencer(
            PlanSettings settings,
            Dictionary<string, Item> items,
            Dictionary<(string, string), int> transferTable)
        {
            _settings = settings;
            _items = items;
            _transferTable = transferTable;

            _scoreCalc = new ScoreCalculator(settings, transferTable);
            _qualityScorer = new QualityScorer(settings);
        }

        public void Run(
            int[,] plan,
            List<string> previousSequence)
        {
            AppLogger.Section("시퀀싱 시작");

            // items가 비어있으면 plan 기준으로 items 재구성
            if (_items == null || _items.Count == 0)
            {
                AppLogger.Warn("기준정보 없음 → 시퀀싱 중단");
                return;
            }

            // 오더 생성
            var generator = new OrderGenerator(_settings, _items);
            var allOrders = generator.GenerateAll(plan);

            // 이전 시퀀스 컨텍스트 초기화
            var initialContext = BuildInitialContext(previousSequence);

            // 버킷별 시퀀싱
            Sequences = new List<List<Order>>();
            var context = new List<Order>(initialContext);

            for (int fence = 0; fence < _settings.FenceCount; fence++)
            {
                AppLogger.Section($"펜스{fence + 1} 시퀀싱");

                var sequence = SequenceFence(
                    allOrders[fence], context, fence);

                Sequences.Add(sequence);

                // 다음 버킷 컨텍스트 = 이번 버킷 마지막 5개
                context = sequence.Count >= 5
                    ? sequence.GetRange(sequence.Count - 5, 5)
                    : new List<Order>(sequence);

                AppLogger.Info($"펜스{fence + 1} 시퀀싱 완료: " +
                              $"{sequence.Count}개 오더");
            }

            // 품질점수 계산
            QualityScores = _qualityScorer.CalcAllScores(
                Sequences, initialContext);

            // 개선 알고리즘 적용
            if (_settings.Algorithm == "Swap")
                ImproveBySwap();
            else if (_settings.Algorithm == "Tabu")
                ImproveByTabu();

            AppLogger.Section("시퀀싱 완료");
            AppLogger.Info($"전체 품질점수: " +
                $"{_qualityScorer.CalcTotalAverage(QualityScores):F1}");
        }

        // ── 그리디 시퀀싱 ─────────────────────────────
        private List<Order> SequenceFence(
            List<Order> orders,
            List<Order> context,
            int fence)
        {
            var sequence = new List<Order>();
            var remaining = new List<Order>(orders);

            // 위치 추적 (컨텍스트 포함)
            var positions = new Dictionary<string, List<int>>();
            for (int i = 0; i < context.Count; i++)
            {
                int pos = i - context.Count;
                var code = context[i].ItemCode;
                if (!positions.ContainsKey(code))
                    positions[code] = new List<int>();
                positions[code].Add(pos);
            }

            var currentContext = new List<Order>(context);
            int seq = 1;

            while (remaining.Count > 0)
            {
                Order? best = null;
                int bestScore = int.MinValue;

                foreach (var candidate in remaining)
                {
                    int score = _scoreCalc.CalcCandidateScore(
                        currentContext, candidate, positions, seq);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }

                if (best == null) break;

                sequence.Add(best);
                remaining.Remove(best);

                if (!positions.ContainsKey(best.ItemCode))
                    positions[best.ItemCode] = new List<int>();
                positions[best.ItemCode].Add(seq);

                currentContext.Add(best);
                if (currentContext.Count > 5)
                    currentContext.RemoveAt(0);

                AppLogger.Log($"  {seq}: {best.ItemCode} " +
                             $"({best.Qty}대) 점수:{bestScore}");
                seq++;
            }

            return sequence;
        }

        // ── Swap 개선 ──────────────────────────────────
        private void ImproveBySwap()
        {
            AppLogger.Section("Swap 개선 시작");
            var random = new Random();

            for (int fence = 0; fence < Sequences.Count; fence++)
            {
                var sequence = Sequences[fence];
                var context = GetContext(fence);

                double bestScore = _qualityScorer
                    .CalcFenceScore(sequence, context).Sum();
                int noImprove = 0;

                for (int iter = 0;
                     iter < _settings.MaxIterations; iter++)
                {
                    if (noImprove >= _settings.NoImproveLimit) break;

                    int i = random.Next(sequence.Count);
                    int j = random.Next(sequence.Count);
                    if (i == j) continue;

                    (sequence[i], sequence[j]) =
                        (sequence[j], sequence[i]);

                    if (context.Count > 0 &&
                        sequence[0].ItemCode ==
                        context.Last().ItemCode)
                    {
                        (sequence[i], sequence[j]) =
                            (sequence[j], sequence[i]);
                        continue;
                    }

                    double newScore = _qualityScorer
                        .CalcFenceScore(sequence, context).Sum();

                    if (newScore < bestScore)
                    {
                        bestScore = newScore;
                        noImprove = 0;
                        AppLogger.Log($"펜스{fence + 1} Swap 개선: " +
                                     $"{newScore:F1}");
                    }
                    else
                    {
                        (sequence[i], sequence[j]) =
                            (sequence[j], sequence[i]);
                        noImprove++;
                    }
                }

                QualityScores[fence] = _qualityScorer
                    .CalcFenceScore(sequence, context);
            }
        }

        // ── Tabu Search 개선 ───────────────────────────
        private void ImproveByTabu()
        {
            AppLogger.Section("Tabu Search 개선 시작");
            int tabuSize = 20;

            for (int fence = 0; fence < Sequences.Count; fence++)
            {
                var sequence = Sequences[fence];
                var context = GetContext(fence);
                var tabuList = new Queue<(int, int)>();

                double bestScore = _qualityScorer
                    .CalcFenceScore(sequence, context).Sum();
                var bestSeq = new List<Order>(sequence);
                var currentSeq = new List<Order>(sequence);
                int noImprove = 0;

                for (int iter = 0;
                     iter < _settings.MaxIterations; iter++)
                {
                    if (noImprove >= _settings.NoImproveLimit) break;

                    double iterBest = double.MaxValue;
                    (int, int) iterBestSwap = (-1, -1);
                    List<Order>? iterBestSeq = null;

                    for (int i = 0; i < currentSeq.Count - 1; i++)
                    {
                        for (int j = i + 1; j < currentSeq.Count; j++)
                        {
                            if (tabuList.Contains((i, j))) continue;

                            var candidate = new List<Order>(currentSeq);
                            (candidate[i], candidate[j]) =
                                (candidate[j], candidate[i]);

                            if (context.Count > 0 &&
                                candidate[0].ItemCode ==
                                context.Last().ItemCode) continue;

                            double score = _qualityScorer
                                .CalcFenceScore(candidate, context)
                                .Sum();

                            if (score < iterBest)
                            {
                                iterBest = score;
                                iterBestSwap = (i, j);
                                iterBestSeq = candidate;
                            }
                        }
                    }

                    if (iterBestSeq == null) break;

                    tabuList.Enqueue(iterBestSwap);
                    if (tabuList.Count > tabuSize)
                        tabuList.Dequeue();

                    currentSeq = iterBestSeq;

                    if (iterBest < bestScore)
                    {
                        bestScore = iterBest;
                        bestSeq = new List<Order>(iterBestSeq);
                        noImprove = 0;
                        AppLogger.Log($"펜스{fence + 1} Tabu 개선: " +
                                     $"{bestScore:F1}");
                    }
                    else
                    {
                        noImprove++;
                    }
                }

                Sequences[fence] = bestSeq;
                QualityScores[fence] = _qualityScorer
                    .CalcFenceScore(bestSeq, context);
            }
        }

        // ── 컨텍스트 가져오기 ──────────────────────────
        private List<Order> GetContext(int fence)
        {
            if (fence == 0) return new List<Order>();
            var prev = Sequences[fence - 1];
            return prev.Count >= 5
                ? prev.GetRange(prev.Count - 5, 5)
                : new List<Order>(prev);
        }

        // ── 이전 시퀀스 컨텍스트 초기화 ───────────────
        private List<Order> BuildInitialContext(
            List<string> previousSequence)
        {
            var context = new List<Order>();

            foreach (var code in previousSequence)
            {
                if (_items.TryGetValue(code, out var item))
                {
                    context.Add(new Order(
                        code, 0, item.Classifications));
                }
                else
                {
                    context.Add(new Order(
                        code, 0, new string[7]));
                }
            }

            if (previousSequence.Count > 0)
                AppLogger.Info($"초기 컨텍스트: " +
                    $"{string.Join(" → ", previousSequence)}");

            return context;
        }
    }
}