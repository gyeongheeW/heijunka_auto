using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace heijunka.Views
{
    public partial class TimeFenceLogView : UserControl
    {
        public TimeFenceLogView()
        {
            InitializeComponent();
        }

        // 외부에서 로그 추가
        public void AppendLog(string message)
        {
            TxtLog.AppendText(message + "\n");
            TxtLog.ScrollToEnd();
        }

        // 로그 전체 설정
        public void SetLog(string log)
        {
            TxtLog.Text = log;
            TxtLog.ScrollToEnd();
        }

        public void Clear() => TxtLog.Clear();

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
        }
    }
}