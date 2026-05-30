using System;

namespace heijunka.Models
{
    public class PlanSettings
    {
        // ── 기본 설정 ────────────────────────────────
        public int UPH { get; set; } = 40;
        public int FenceHours { get; set; } = 2;
        public int ShiftCount { get; set; } = 3;
        public int FencesPerShift { get; set; } = 4;
        public int FenceCount => ShiftCount * FencesPerShift;

        // QPC 적용 시 자동 Round된 TARGET
        public int Target
        {
            get
            {
                if (UseQPC && QPC > 0)
                    return (int)Math.Round(
                        (double)UPH / QPC) * QPC * FenceHours;
                return UPH * FenceHours;
            }
        }

        // ── 펜스별 가동여부 ──────────────────────────
        public int[] FenceActive { get; set; }
            = Array.Empty<int>();

        public int GetFenceTarget(int fence)
        {
            if (FenceActive.Length > fence &&
                FenceActive[fence] == 0)
                return 0;
            return Target;
        }

        // ── 시프트 시작시간 ──────────────────────────
        public TimeSpan Shift1Start { get; set; }
            = new TimeSpan(8, 0, 0);
        public TimeSpan Shift2Start { get; set; }
            = new TimeSpan(16, 0, 0);
        public TimeSpan Shift3Start { get; set; }
            = new TimeSpan(0, 0, 0);

        // ── 시프트 헬퍼 ─────────────────────────────
        public int GetShiftIndex(int fence)
            => fence / FencesPerShift;

        public int GetShiftStart(int fence)
            => GetShiftIndex(fence) * FencesPerShift;

        public int GetShiftEnd(int fence)
            => GetShiftStart(fence) + FencesPerShift - 1;

        public bool IsLastFenceOfShift(int fence)
            => (fence + 1) % FencesPerShift == 0;

        public bool IsLastFence(int fence)
            => fence == FenceCount - 1;

        // ── QPC ─────────────────────────────────────
        public bool UseQPC { get; set; } = false;
        public int QPC { get; set; } = 1;

        public int ApplyQPC(int qty)
        {
            if (!UseQPC || QPC <= 1) return qty;
            return (int)Math.Ceiling((double)qty / QPC) * QPC;
        }

        public int ApplyQPCDown(int qty)
        {
            if (!UseQPC || QPC <= 1) return qty;
            return (qty / QPC) * QPC;
        }

        // ── Max Order Qty (시퀀싱용) ─────────────────
        public bool UseMaxOrderQty { get; set; } = false;
        public int MaxOrderQty { get; set; } = 20;

        // ── 계획 모드 ────────────────────────────────
        public string PlanMode { get; set; } = "Heuristic";
        // "Heuristic" or "LP"

        // ── 분류 설정 ────────────────────────────────
        public string[] ClassLabels { get; set; }
            = { "분류A", "분류B", "분류C", "분류D",
                "분류E", "분류F", "분류G" };
        public int[] Weights { get; set; }
            = { 2, 4, 1, 1, 1, 1, 1 };
        public int SamePenalty { get; set; } = -99;

        // ── 시퀀싱 우선순위 ──────────────────────────
        public int TransferRuleWeight { get; set; } = 10;
        public int ClassMixWeight { get; set; } = 1;

        // ── 알고리즘 설정 ────────────────────────────
        public string Algorithm { get; set; } = "Greedy";
        public int MaxIterations { get; set; } = 1000;
        public int NoImproveLimit { get; set; } = 50;
    }
}