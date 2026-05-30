using System;
using System.Windows;
using System.Windows.Controls;
using heijunka.Models;

namespace heijunka.Views
{
    public partial class SettingsValueView : UserControl
    {
        public SettingsValueView()
        {
            InitializeComponent();
            UpdateTarget();
        }

        // ── 이벤트 ────────────────────────────────────
        private void ProductionSettings_Changed(object sender,
            TextChangedEventArgs e)
        {
            UpdateTarget();
            RoundUPHToQPC();
        }

        private void ChkQPC_Changed(object sender,
            RoutedEventArgs e)
        {
            if (TxtQPC != null)
                TxtQPC.IsEnabled = ChkQPC.IsChecked == true;
            RoundUPHToQPC();
            UpdateTarget();
        }

        private void ChkMaxOrderQty_Changed(object sender,
            RoutedEventArgs e)
        {
            if (TxtMaxOrderQty != null)
                TxtMaxOrderQty.IsEnabled =
                    ChkMaxOrderQty.IsChecked == true;
        }

        // ── TARGET 자동계산 ───────────────────────────
        private void UpdateTarget()
        {
            if (TxtUPH == null || TxtFenceHours == null ||
                TxtTarget == null) return;

            if (!int.TryParse(TxtUPH.Text, out int uph)) return;
            if (!int.TryParse(TxtFenceHours.Text,
                out int hours)) return;

            // QPC 적용 시 Round
            if (ChkQPC != null &&
                ChkQPC.IsChecked == true &&
                TxtQPC != null &&
                int.TryParse(TxtQPC.Text, out int qpc) &&
                qpc > 0)
            {
                int roundedUPH =
                    (int)Math.Round((double)uph / qpc) * qpc;
                int target = roundedUPH * hours;
                TxtTarget.Text = $"{target}대";
            }
            else
            {
                TxtTarget.Text = $"{uph * hours}대";
            }
        }

        // ── UPH 자동 Round ────────────────────────────
        private void RoundUPHToQPC()
        {
            if (TxtUPH == null || TxtQPC == null) return;
            if (ChkQPC == null ||
                ChkQPC.IsChecked != true) return;

            if (!int.TryParse(TxtUPH.Text, out int uph)) return;
            if (!int.TryParse(TxtQPC.Text, out int qpc)) return;
            if (qpc <= 0) return;

            int rounded =
                (int)Math.Round((double)uph / qpc) * qpc;
            if (rounded != uph)
            {
                TxtUPH.TextChanged -= ProductionSettings_Changed;
                TxtUPH.Text = rounded.ToString();
                TxtUPH.TextChanged += ProductionSettings_Changed;
            }
        }

        // ── GetSettings ───────────────────────────────
        public PlanSettings GetSettings()
        {
            return new PlanSettings
            {
                UPH = int.TryParse(TxtUPH.Text,
                    out int uph) ? uph : 40,
                FenceHours = int.TryParse(TxtFenceHours.Text,
                    out int fh) ? fh : 2,
                ShiftCount = int.TryParse(TxtShiftCount.Text,
                    out int sc) ? sc : 3,
                FencesPerShift = int.TryParse(
                    TxtFencesPerShift.Text,
                    out int fps) ? fps : 4,

                UseQPC = ChkQPC.IsChecked == true,
                QPC = int.TryParse(TxtQPC.Text,
                    out int qpc) ? qpc : 1,

                UseMaxOrderQty =
                    ChkMaxOrderQty.IsChecked == true,
                MaxOrderQty = int.TryParse(TxtMaxOrderQty.Text,
                    out int moq) ? moq : 20,

                PlanMode = RbLP.IsChecked == true
                    ? "LP" : "Heuristic",

                TransferRuleWeight = int.TryParse(
                    TxtTransferRuleWeight.Text,
                    out int trw) ? trw : 10,
                ClassMixWeight = int.TryParse(
                    TxtClassMixWeight.Text,
                    out int cmw) ? cmw : 1,

                Algorithm =
                    RbTabu.IsChecked == true ? "Tabu" :
                    RbSwap.IsChecked == true ? "Swap" : "Greedy",
                MaxIterations = int.TryParse(
                    TxtMaxIterations.Text,
                    out int mi) ? mi : 1000,
                NoImproveLimit = int.TryParse(
                    TxtNoImproveLimit.Text,
                    out int nil) ? nil : 50,

                Shift1Start = TimeSpan.TryParse(
                    TxtShift1Start.Text, out var s1)
                    ? s1 : new TimeSpan(8, 0, 0),
                Shift2Start = TimeSpan.TryParse(
                    TxtShift2Start.Text, out var s2)
                    ? s2 : new TimeSpan(16, 0, 0),
                Shift3Start = TimeSpan.TryParse(
                    TxtShift3Start.Text, out var s3)
                    ? s3 : new TimeSpan(0, 0, 0),
            };
        }

        // ── SetSettings ───────────────────────────────
        public void SetSettings(PlanSettings settings)
        {
            TxtUPH.Text = settings.UPH.ToString();
            TxtFenceHours.Text = settings.FenceHours.ToString();
            TxtShiftCount.Text = settings.ShiftCount.ToString();
            TxtFencesPerShift.Text =
                settings.FencesPerShift.ToString();

            ChkQPC.IsChecked = settings.UseQPC;
            TxtQPC.Text = settings.QPC.ToString();
            TxtQPC.IsEnabled = settings.UseQPC;

            ChkMaxOrderQty.IsChecked = settings.UseMaxOrderQty;
            TxtMaxOrderQty.Text = settings.MaxOrderQty.ToString();
            TxtMaxOrderQty.IsEnabled = settings.UseMaxOrderQty;

            RbHeuristic.IsChecked =
                settings.PlanMode == "Heuristic";
            RbLP.IsChecked = settings.PlanMode == "LP";

            TxtTransferRuleWeight.Text =
                settings.TransferRuleWeight.ToString();
            TxtClassMixWeight.Text =
                settings.ClassMixWeight.ToString();

            RbGreedy.IsChecked = settings.Algorithm == "Greedy";
            RbSwap.IsChecked = settings.Algorithm == "Swap";
            RbTabu.IsChecked = settings.Algorithm == "Tabu";

            TxtMaxIterations.Text =
                settings.MaxIterations.ToString();
            TxtNoImproveLimit.Text =
                settings.NoImproveLimit.ToString();

            TxtShift1Start.Text =
                settings.Shift1Start.ToString(@"hh\:mm");
            TxtShift2Start.Text =
                settings.Shift2Start.ToString(@"hh\:mm");
            TxtShift3Start.Text =
                settings.Shift3Start.ToString(@"hh\:mm");

            UpdateTarget();
        }

        private void BtnSaveSettings_Click(
            object sender, RoutedEventArgs e)
        {
            MessageBox.Show("설정이 저장되었습니다.",
                "완료", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}