using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LiveCharts;
using LiveCharts.Wpf;
using CommonLibrary.Helpers;
using CommonLibrary.Interfaces;

namespace PieChartControl
{
    public partial class LivePieChartControl : UserControl, IContextMenuSupport
    {
        private DateTime lastResetDate = DateTime.Today;
        private LiveCharts.WinForms.PieChart pieChart;
        private ContextMenuStrip contextMenu; 
        private Dictionary<string, PieSeries> seriesDict = new Dictionary<string, PieSeries>(); 
        private Label labelTotal;

        /// <summary>
        /// 기본 생성자 - OK(초록색), NG(빨간색) 시리즈가 자동으로 생성됩니다.
        /// </summary>
        public LivePieChartControl()
        {
            InitializeComponent();

            InitializePieChart(new List<Tuple<string, Color>>
            {
                Tuple.Create("OK", Color.LimeGreen),
                Tuple.Create("NG", Color.Red)
            });
        }
        public LivePieChartControl(params Tuple<string, Color>[] seriesInfo)
        {
            InitializeComponent();
            InitializePieChart(seriesInfo);
        }

        #region Setting
        /// <summary>
        /// 기본 생성자는 OK(초록색), NG(빨간색) 시리즈가 자동으로 생성됩니다.
        /// SetSeries 사용 시 Series의 이름과 색깔 선택 시 자동으로 시리즈가 추가 됩니다.
        /////////////////////// ForExample ///////////////////////
        /// livePieChartControl1.SetSeries(
        /// Tuple.Create("OK", Color.LimeGreen),
        /// Tuple.Create("NG", Color.Red),
        /// Tuple.Create("RETRY", Color.Orange)
        /// );
        /// </summary>
        public void SetSeries(params Tuple<string, Color>[] seriesInfo)
        {
            InitializePieChart(seriesInfo);
        }
        public void InitializePieChart(IEnumerable<Tuple<string, Color>> seriesInfo)
        {
            pieChart = this.pieChart1;

            pieChart.InnerRadius = 110;
            pieChart.Zoom = ZoomingOptions.Xy;
            pieChart.HoverPushOut = 10;
            pieChart.Dock = DockStyle.None;

            var seriesCollection = new SeriesCollection();
            seriesDict.Clear();

            foreach (var item in seriesInfo)
            {
                var label = item.Item1;
                var color = item.Item2;
                var brush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));

                var series = new PieSeries
                {
                    Title = label,
                    Values = new ChartValues<double> { 0 },
                    Fill = brush,
                    DataLabels = true,
                    LabelPoint = point => string.Format("{0} {1}", point.Y, label),
                    Foreground = System.Windows.Media.Brushes.WhiteSmoke,
                };

                seriesCollection.Add(series);
                seriesDict[label] = series;
            }

            pieChart.Series = seriesCollection;

            if (labelTotal == null)
            {
                labelTotal = new Label
                {
                    Text = "Total: 0",
                    AutoSize = true,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                };

                this.Controls.Add(labelTotal);
                labelTotal.BringToFront(); // 차트 위로!
            }

            CenterLabelOverPie(); // 첫 위치 설정

            InitializeContextMenu();
        }
        public void InitializeContextMenu()
        {
            ChartContextMenuHelper.ApplyDefaultContextMenu(pieChart, ResetZoom, SaveAsCsv, SaveAsImage, ResetAllValues);
        }
        #endregion

        #region Setting TotalLabel
        private void CenterLabelOverPie()
        {
            if (labelTotal != null && pieChart != null)
            {
                labelTotal.Left = pieChart.Left + pieChart.Width / 2 - labelTotal.Width / 2;
                labelTotal.Top = pieChart.Top + pieChart.Height / 2 - labelTotal.Height / 2;
            }
        }
        private void UpdateTotalLabel()
        {
            double total = seriesDict.Values
                .Sum(series => ((ChartValues<double>)series.Values)[0]);

            if (labelTotal != null)
            {
                labelTotal.Text = $"Total: {total}";
                CenterLabelOverPie();
            }
        }
        #endregion

        #region SetValue
        public void SetValue(string label, int value)
        {
            if (seriesDict.TryGetValue(label, out var series))
            {
                ((ChartValues<double>)series.Values)[0] = value;
                UpdateTotalLabel();
            }
        }
        public void IncrementValue(string label, int amount = 1)
        {
            if (seriesDict.ContainsKey(label))
            {
                var values = (ChartValues<double>)seriesDict[label].Values;
                values[0] += amount;
                UpdateTotalLabel();
            }
        }
        #endregion

        #region ContextMenu
        private void ResetZoom()
        {
            // pieChart.Zoom = ZoomingOptions.None;
        }

        private void SaveAsCsv()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV file|*.csv";
                sfd.FileName = string.Format("PieChart_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter writer = new StreamWriter(sfd.FileName))
                    {
                        writer.WriteLine("Label,Value");

                        foreach (var kv in seriesDict)
                        {
                            var value = (kv.Value.Values[0] as double?) ?? 0;
                            writer.WriteLine(string.Format("{0},{1:F2}", kv.Key, value));
                        }

                        double total = seriesDict.Values.Sum(s => ((ChartValues<double>)s.Values)[0]);
                        writer.WriteLine($"Total,{total:F2}");

                        MessageBox.Show("CSV 저장 완료!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void SaveAsImage()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png";
                sfd.FileName = $"PieChart_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (Bitmap bmp = new Bitmap(this.Width, this.Height))
                    {
                        this.DrawToBitmap(bmp, new Rectangle(0, 0, this.Width, this.Height));
                        bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    MessageBox.Show("차트 이미지 저장 완료!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public void ResetAllValues()
        {
            foreach (var s in seriesDict.Values)
            {
                ((ChartValues<double>)s.Values)[0] = 0;
                UpdateTotalLabel();
            }
        }
        #endregion

        private void midnightTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Today > lastResetDate)
            {
                ResetAllValues();
                lastResetDate = DateTime.Today;
            }
        }
    }
}
