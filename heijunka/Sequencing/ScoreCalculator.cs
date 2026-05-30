using System;
using System.Collections.Generic;
using heijunka.Models;

namespace heijunka.Sequencing
{
    public class ScoreCalculator
    {
        private readonly PlanSettings _settings;
        private readonly Dictionary<(string, string), int> _transferTable;

        public ScoreCalculator(
            PlanSettings settings,
            Dictionary<(string, string), int> transferTable)
        {
            _settings = settings;
            _transferTable = transferTable;
        }

        public int CalcTransitionScore(Order? prev, Order next)
        {
            if (prev == null) return 0;

            // 같은 품목 → 패널티
            if (prev.ItemCode == next.ItemCode)
                return _settings.SamePenalty;

            // 전환규칙 점수 (우선순위 가중치 적용)
            int transferScore = 0;
            if (_transferTable.TryGetValue(
                (prev.ItemCode, next.ItemCode), out int tableScore))
            {
                transferScore = tableScore * _settings.TransferRuleWeight;
            }

            // 분류별 섞기 점수 (우선순위 가중치 적용)
            int classMixScore = 0;
            for (int c = 0; c < 7; c++)
            {
                string prevClass = prev.Classifications[c] ?? "";
                string nextClass = next.Classifications[c] ?? "";

                if (!string.IsNullOrEmpty(prevClass) &&
                    !string.IsNullOrEmpty(nextClass) &&
                    prevClass != nextClass)
                {
                    classMixScore += _settings.Weights[c];
                }
            }
            classMixScore *= _settings.ClassMixWeight;

            return transferScore + classMixScore;
        }

        public int CalcCandidateScore(
            List<Order> context,
            Order candidate,
            Dictionary<string, List<int>> positions,
            int currentSeq)
        {
            Order? prev = context.Count > 0
                ? context[context.Count - 1] : null;

            int score = CalcTransitionScore(prev, candidate);

            if (prev != null && prev.ItemCode == candidate.ItemCode)
                return score;

            // 간격 보너스
            if (positions.TryGetValue(candidate.ItemCode,
                out var posList) && posList.Count > 0)
            {
                int lastPos = posList[posList.Count - 1];
                int gap = currentSeq - lastPos;
                score += gap;
            }
            else
            {
                score += 10;
            }

            return score;
        }
    }
}