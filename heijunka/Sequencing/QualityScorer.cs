using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;
using heijunka.Models;

namespace heijunka.Sequencing
{
    public class QualityScorer
    {
        private readonly PlanSettings _settings;

        public QualityScorer(PlanSettings settings)
        {
            _settings = settings;
        }

        // ── 버킷별 품질점수 계산 ───────────────────────
        public double[] CalcFenceScore(
            List<Order> sequence,
            List<Order> context)
        {
            var scores = new double[7];

            // 분류별로 계산
            for (int c = 0; c < 7; c++)
            {
                // 분류값별 등장위치 수집 (컨텍스트 포함)
                var positions = new Dictionary<string, List<int>>();

                // 컨텍스트 위치 (-5 ~ -1)
                for (int i = 0; i < context.Count; i++)
                {
                    int pos = i - context.Count;
                    string val = context[i].Classifications[c] ?? "";
                    if (string.IsNullOrEmpty(val)) continue;

                    if (!positions.ContainsKey(val))
                        positions[val] = new List<int>();
                    positions[val].Add(pos);
                }

                // 시퀀스 위치 (1 ~ N)
                for (int i = 0; i < sequence.Count; i++)
                {
                    string val = sequence[i].Classifications[c] ?? "";
                    if (string.IsNullOrEmpty(val)) continue;

                    if (!positions.ContainsKey(val))
                        positions[val] = new List<int>();
                    positions[val].Add(i + 1);
                }

                // 분류값별 간격 max-min 계산
                double classScore = 0;
                foreach (var kv in positions)
                {
                    var posList = kv.Value;
                    if (posList.Count < 2) continue;

                    var gaps = posList
                        .Zip(posList.Skip(1), (a, b) => b - a)
                        .ToList();

                    classScore += gaps.Max() - gaps.Min();
                }

                // 가중치 적용
                scores[c] = classScore * _settings.Weights[c];
            }

            return scores;
        }

        // ── 전체 품질점수 계산 ─────────────────────────
        public List<double[]> CalcAllScores(
            List<List<Order>> sequences,
            List<Order> initialContext)
        {
            var allScores = new List<double[]>();
            var context = new List<Order>(initialContext);

            AppLogger.Section("품질점수 계산 시작");

            for (int f = 0; f < sequences.Count; f++)
            {
                var scores = CalcFenceScore(sequences[f], context);
                allScores.Add(scores);

                double total = scores.Sum();
                AppLogger.Info($"펜스{f + 1} 품질점수: {total:F1} " +
                    $"[{string.Join(", ", Array.ConvertAll(scores, s => s.ToString("F1")))}]");

                // 다음 버킷 컨텍스트 = 이번 버킷 마지막 5개
                context = sequences[f].Count >= 5
                    ? sequences[f].GetRange(
                        sequences[f].Count - 5, 5)
                    : new List<Order>(sequences[f]);
            }

            AppLogger.Section("품질점수 계산 완료");
            return allScores;
        }

        // ── 전체 평균 점수 ─────────────────────────────
        public double CalcTotalAverage(List<double[]> allScores)
        {
            if (allScores.Count == 0) return 0;
            return allScores.Average(s => s.Sum());
        }
    }
}