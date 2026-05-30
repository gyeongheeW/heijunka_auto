using System;
using System.IO;
using System.Text.Json;
using heijunka.Models;

namespace heijunka.Models
{
    public class AppConfig
    {
        // ── 파일 경로 ──────────────────────────────────
        public string MasterFilePath { get; set; } = "";
        public string TransferFilePath { get; set; } = "";
        public string TimeFenceDemandPath { get; set; } = "";
        public string TimeFenceOutputFolder { get; set; } = "";
        public string SequencingInputPath { get; set; } = "";
        public string SequencingOutputFolder { get; set; } = "";

        // ── 생산 설정 ──────────────────────────────────
        public int UPH { get; set; } = 40;
        public int FenceHours { get; set; } = 2;
        public int ShiftCount { get; set; } = 3;
        public int FencesPerShift { get; set; } = 4;

        // ── QPC / Max Order Qty ────────────────────────
        public bool UseQPC { get; set; } = false;
        public int QPC { get; set; } = 1;
        public bool UseMaxOrderQty { get; set; } = false;
        public int MaxOrderQty { get; set; } = 20;

        // ── 계획 모드 ──────────────────────────────────
        public string PlanMode { get; set; } = "Heuristic";

        // ── 시프트 시작시간 ─────────────────────────────
        public string Shift1Start { get; set; } = "08:00";
        public string Shift2Start { get; set; } = "16:00";
        public string Shift3Start { get; set; } = "00:00";

        // ── 분류 설정 ───────────────────────────────────
        public string[] ClassLabels { get; set; }
            = { "분류A","분류B","분류C","분류D",
                "분류E","분류F","분류G" };
        public int[] Weights { get; set; }
            = { 2, 4, 1, 1, 1, 1, 1 };
        public int SamePenalty { get; set; } = -99;

        // ── 시퀀싱 우선순위 ─────────────────────────────
        public int TransferRuleWeight { get; set; } = 10;
        public int ClassMixWeight { get; set; } = 1;

        // ── 알고리즘 설정 ───────────────────────────────
        public string Algorithm { get; set; } = "Greedy";
        public int MaxIterations { get; set; } = 1000;
        public int NoImproveLimit { get; set; } = 50;

        // ── 저장/로드 ──────────────────────────────────
        private static string ConfigPath =>
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfig>(
                        json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Config 로드 실패: {ex.Message}");
            }
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                File.WriteAllText(ConfigPath, json);
                System.Diagnostics.Debug.WriteLine(
                    $"Config 저장 완료: {ConfigPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Config 저장 실패: {ex.Message}");
            }
        }
    }
}