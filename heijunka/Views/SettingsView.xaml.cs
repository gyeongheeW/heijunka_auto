using System;
using System.Collections.Generic;
using System.Windows.Controls;
using heijunka.Models;

namespace heijunka.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        // ── 설정값 가져오기 ────────────────────────────
        public PlanSettings GetSettings()
        {
            var settings = SettingsValue.GetSettings();
            settings.ClassLabels = ClassSettings.GetLabels();
            settings.Weights = ClassSettings.GetWeights();
            settings.SamePenalty = ClassSettings.GetSamePenalty();
            return settings;
        }

        // ── 기준정보 ───────────────────────────────────
        public Dictionary<string, Item>? GetItems()
            => MasterData.Items;

        // ── 전환테이블 ─────────────────────────────────
        public Dictionary<(string, string), int>?
            GetTransferTable()
            => TransferTable.TransferTable;

        // ── 파일 경로 ──────────────────────────────────
        public string MasterFilePath => MasterData.GetPath();
        public string TransferFilePath => TransferTable.GetPath();

        public void SetMasterPath(string path)
            => MasterData.SetPath(path);
        public void SetTransferPath(string path)
            => TransferTable.SetPath(path);

        // ── Config 로드 ────────────────────────────────
        public void SetConfig(AppConfig config)
        {
            SettingsValue.SetSettings(new PlanSettings
            {
                UPH = config.UPH,
                FenceHours = config.FenceHours,
                ShiftCount = config.ShiftCount,
                FencesPerShift = config.FencesPerShift,

                UseQPC = config.UseQPC,
                QPC = config.QPC,
                UseMaxOrderQty = config.UseMaxOrderQty,
                MaxOrderQty = config.MaxOrderQty,

                PlanMode = config.PlanMode,

                TransferRuleWeight = config.TransferRuleWeight,
                ClassMixWeight = config.ClassMixWeight,

                Algorithm = config.Algorithm,
                MaxIterations = config.MaxIterations,
                NoImproveLimit = config.NoImproveLimit,

                ClassLabels = config.ClassLabels,
                Weights = config.Weights,
                SamePenalty = config.SamePenalty,

                Shift1Start = TimeSpan.TryParse(
                    config.Shift1Start, out var s1)
                    ? s1 : new TimeSpan(8, 0, 0),
                Shift2Start = TimeSpan.TryParse(
                    config.Shift2Start, out var s2)
                    ? s2 : new TimeSpan(16, 0, 0),
                Shift3Start = TimeSpan.TryParse(
                    config.Shift3Start, out var s3)
                    ? s3 : new TimeSpan(0, 0, 0)
            });

            ClassSettings.SetLabels(config.ClassLabels);
            ClassSettings.SetWeights(config.Weights);
            ClassSettings.SetSamePenalty(config.SamePenalty);

            if (!string.IsNullOrEmpty(config.MasterFilePath))
                MasterData.SetPath(config.MasterFilePath);
            if (!string.IsNullOrEmpty(config.TransferFilePath))
                TransferTable.SetPath(config.TransferFilePath);
        }

        // ── Config 저장 ────────────────────────────────
        public void GetConfig(AppConfig config)
        {
            var settings = GetSettings();

            config.UPH = settings.UPH;
            config.FenceHours = settings.FenceHours;
            config.ShiftCount = settings.ShiftCount;
            config.FencesPerShift = settings.FencesPerShift;

            config.UseQPC = settings.UseQPC;
            config.QPC = settings.QPC;
            config.UseMaxOrderQty = settings.UseMaxOrderQty;
            config.MaxOrderQty = settings.MaxOrderQty;

            config.PlanMode = settings.PlanMode;

            config.TransferRuleWeight =
                settings.TransferRuleWeight;
            config.ClassMixWeight = settings.ClassMixWeight;

            config.Algorithm = settings.Algorithm;
            config.MaxIterations = settings.MaxIterations;
            config.NoImproveLimit = settings.NoImproveLimit;

            config.ClassLabels = settings.ClassLabels;
            config.Weights = settings.Weights;
            config.SamePenalty = settings.SamePenalty;

            config.Shift1Start =
                settings.Shift1Start.ToString(@"hh\:mm");
            config.Shift2Start =
                settings.Shift2Start.ToString(@"hh\:mm");
            config.Shift3Start =
                settings.Shift3Start.ToString(@"hh\:mm");

            config.MasterFilePath = MasterData.GetPath();
            config.TransferFilePath = TransferTable.GetPath();
        }
    }
}