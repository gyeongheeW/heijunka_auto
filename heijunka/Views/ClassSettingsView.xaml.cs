using System.Windows;
using System.Windows.Controls;
using heijunka.Models;

namespace heijunka.Views
{
    public partial class ClassSettingsView : UserControl
    {
        public ClassSettingsView()
        {
            InitializeComponent();
        }

        public string[] GetLabels()
        {
            return new[]
            {
                TxtLabelA.Text, TxtLabelB.Text, TxtLabelC.Text,
                TxtLabelD.Text, TxtLabelE.Text, TxtLabelF.Text,
                TxtLabelG.Text
            };
        }

        public int[] GetWeights()
        {
            return new[]
            {
                ParseInt(TxtWeightA.Text),
                ParseInt(TxtWeightB.Text),
                ParseInt(TxtWeightC.Text),
                ParseInt(TxtWeightD.Text),
                ParseInt(TxtWeightE.Text),
                ParseInt(TxtWeightF.Text),
                ParseInt(TxtWeightG.Text)
            };
        }

        public int GetSamePenalty()
            => ParseInt(TxtSamePenalty.Text);

        public void SetLabels(string[] labels)
        {
            if (labels.Length < 7) return;
            TxtLabelA.Text = labels[0];
            TxtLabelB.Text = labels[1];
            TxtLabelC.Text = labels[2];
            TxtLabelD.Text = labels[3];
            TxtLabelE.Text = labels[4];
            TxtLabelF.Text = labels[5];
            TxtLabelG.Text = labels[6];
        }

        public void SetWeights(int[] weights)
        {
            if (weights.Length < 7) return;
            TxtWeightA.Text = weights[0].ToString();
            TxtWeightB.Text = weights[1].ToString();
            TxtWeightC.Text = weights[2].ToString();
            TxtWeightD.Text = weights[3].ToString();
            TxtWeightE.Text = weights[4].ToString();
            TxtWeightF.Text = weights[5].ToString();
            TxtWeightG.Text = weights[6].ToString();
        }

        public void SetSamePenalty(int penalty)
            => TxtSamePenalty.Text = penalty.ToString();

        private int ParseInt(string s)
            => int.TryParse(s, out int v) ? v : 0;

        private void BtnSaveClassSettings_Click(
            object sender, RoutedEventArgs e)
        {
            MessageBox.Show("분류설정이 저장되었습니다.",
                "완료", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}