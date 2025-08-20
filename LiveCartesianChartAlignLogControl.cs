using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommonLibrary.Interfaces;
using CommonLibrary.Helpers;
using System.IO;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using LiveCharts;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using LiveCharts.Wpf.Charts.Base;


namespace LiveCartesianChartAlignLogControl
{
    public partial class LiveCartesianChartAlignLogControl: UserControl, IContextMenuSupport
    {
        private int? m_dataCount = 20;
        public int DataCount { get => (int)(m_dataCount ?? 0); set => m_dataCount = value; }

        private List<LiveCharts.WinForms.CartesianChart> _charts = null;

        public class AlignDataGroup
        {
            public List<double> X { get; set; } = new List<double>();
            public List<double> Y { get; set; } = new List<double>();
            public List<double> T { get; set; } = new List<double>();
        }
        public AlignDataGroup AlignData = new AlignDataGroup();

        public LiveCartesianChartAlignLogControl()
        {
            InitializeComponent();
            _charts = new List<LiveCharts.WinForms.CartesianChart> { CS_AlignX, CS_AlignY, CS_AlignT }; // 먼저 초기화
            InitializeContextMenu();
            AddLabels();
            InitAllChart();
        }

        public void SetDataMaxCount(int DataCountMax)
        {
            DataCount = DataCountMax;
            InitAllChart();
        }

        private void InitAllChart()
        {
            for (int i = 0; i < _charts.Count; i++)
            {
                _charts[i].Series = new SeriesCollection
            {
                new GLineSeries
                {
                    Title = "Center" + _charts[i].Name.Substring(_charts[i].Name.Length - 1),
                    Values = new GearedValues<ObservablePoint>(),
                    StrokeThickness = 2,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 15,
                    LineSmoothness = 0,
                    Fill = null,
                    DataLabels = true,
                    LabelPoint = point => point.Y.ToString("F2"),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Thin,
                }
            };

                _charts[i].AxisX.Clear();
                _charts[i].AxisY.Clear();

                _charts[i].AxisX.Add(new Axis
                {
                    Title = "Align" + _charts[i].Name.Substring(_charts[i].Name.Length - 1),
                    Labels = Enumerable.Range(0, DataCount + 1).Select(j => j.ToString()).ToArray(),
                    Separator = new Separator
                    {
                        Step = 1,
                        Stroke = System.Windows.Media.Brushes.Gray,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 3, 3 }
                    },
                    FontSize = 18,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    MinValue = 0,
                    MaxValue = DataCount + 1,
                    Unit = 1,
                });

                _charts[i].AxisY.Add(new Axis
                {
                    Title = "Y Axis",
                    Separator = new Separator
                    {
                        Stroke = System.Windows.Media.Brushes.Gray,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 3, 3 }
                    },
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.LightGray
                });

                _charts[i].Zoom = ZoomingOptions.Xy;
                _charts[i].Pan = PanningOptions.Xy;

                _charts[i].AxisX[0].MinValue = 1; // MinData
                _charts[i].AxisX[0].MaxValue = DataCount;
            }
        }

        private void AddLabels()
        {
            System.Windows.Forms.Label lblMax = new System.Windows.Forms.Label
            {
                Text = "MAX",
                ForeColor = System.Drawing.Color.Red,
                Location = new System.Drawing.Point(tableLayoutPanel1.Right + 10, 60), // 차트 오른쪽 기준 위치 조정
                AutoSize = true,
                Font = new Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };
            System.Windows.Forms.Label lblMin = new System.Windows.Forms.Label
            {
                Text = "MIN",
                ForeColor = System.Drawing.Color.LimeGreen,
                Location = new System.Drawing.Point(tableLayoutPanel1.Right + 10, 80), // 차트 오른쪽 기준 위치 조정
                AutoSize = true,
                Font = new Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };
            System.Windows.Forms.Label lblAvg = new System.Windows.Forms.Label
            {
                Text = "AVG",
                ForeColor = System.Drawing.Color.Orange,
                Location = new System.Drawing.Point(tableLayoutPanel1.Right + 10, 100), // 차트 오른쪽 기준 위치 조정
                AutoSize = true,
                Font = new Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };
            System.Windows.Forms.Label lblStdev = new System.Windows.Forms.Label
            {
                Text = "STDEV",
                ForeColor = System.Drawing.Color.Blue,
                Location = new System.Drawing.Point(tableLayoutPanel1.Right + 10, 120), // 차트 오른쪽 기준 위치 조정
                AutoSize = true,
                Font = new Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };

            this.Controls.Add(lblMax);
            this.Controls.Add(lblMin);
            this.Controls.Add(lblAvg);
            this.Controls.Add(lblStdev);
        }

        public void InitializeContextMenu()
        {
            foreach (var chart in _charts)
            {
                ChartContextMenuHelper.ApplyDefaultContextMenu(chart, () => ResetZoom(chart), () => SaveAsCsv(chart), () => SaveAsImage(chart), () => ResetAllValues(chart));
            }
        }

        private void BTN_CSV_SAVE_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV file|*.csv";
                sfd.FileName = $"ChartData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new StreamWriter(sfd.FileName))
                    {
                        // 데이터 헤더
                        writer.WriteLine("Index,X,Y,T");

                        int rowCount = Math.Max(AlignData.X.Count, Math.Max(AlignData.Y.Count, AlignData.T.Count));
                        for (int i = 0; i < rowCount; i++)
                        {
                            string x = i < AlignData.X.Count ? AlignData.X[i].ToString("F4") : "";
                            string y = i < AlignData.Y.Count ? AlignData.Y[i].ToString("F4") : "";
                            string t = i < AlignData.T.Count ? AlignData.T[i].ToString("F4") : "";
                            writer.WriteLine($"{i + 1},{x},{y},{t}");
                        }

                        writer.WriteLine(); // 빈 줄 구분
                        writer.WriteLine("Summary");

                        // ✅ 축 전체를 순회하면서 WriteStats 호출
                        var axisMap = new Dictionary<string, List<double>>
                        {
                            { "X", AlignData.X },
                            { "Y", AlignData.Y },
                            { "T", AlignData.T }
                        };

                        foreach (var kvp in axisMap)
                        {
                            WriteStats(writer, kvp.Key, kvp.Value);
                        }
                    }

                    System.Windows.Forms.MessageBox.Show("CSV 저장 완료!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }



        private void WriteStats(StreamWriter writer, string label, List<double> data)
        {
            var nonZeroData = data.Where(d => d != 0).ToList();

            double avg = nonZeroData.Count > 0 ? Math.Round(nonZeroData.Average(), 2) : 0;
            double max = nonZeroData.Count > 0 ? Math.Round(nonZeroData.Max(), 2) : 0;
            double min = nonZeroData.Count > 0 ? Math.Round(nonZeroData.Min(), 2) : 0;
            double stdev = nonZeroData.Count > 1 ? Math.Round(Math.Sqrt(nonZeroData.Sum(d => Math.Pow(d - avg, 2)) / (nonZeroData.Count - 1)), 2) : 0;

            writer.WriteLine($"{label}_AVG,{avg}");
            writer.WriteLine($"{label}_MAX,{max}");
            writer.WriteLine($"{label}_MIN,{min}");
            writer.WriteLine($"{label}_STDEV,{stdev}");
            writer.WriteLine(); // 축별 구분 줄
        }

        public void SetValue(params List<double>[] dataSets)
        {
            if (_charts == null || _charts.Count == 0 || dataSets == null || dataSets.Length == 0)
                return;

            int count = Math.Min(_charts.Count, dataSets.Length);

            // AlignData와 dataSets 동기화 및 DataCount 초과 시 첫 번째 데이터 삭제
            if (dataSets.Length > 0 && dataSets[0] != null)
            {
                AlignData.X = new List<double>(dataSets[0]);
                while (AlignData.X.Count > DataCount) AlignData.X.RemoveAt(0); // DataCount 초과 시 첫 번째 삭제
            }
            if (dataSets.Length > 1 && dataSets[1] != null)
            {
                AlignData.Y = new List<double>(dataSets[1]);
                while (AlignData.Y.Count > DataCount) AlignData.Y.RemoveAt(0);
            }
            if (dataSets.Length > 2 && dataSets[2] != null)
            {
                AlignData.T = new List<double>(dataSets[2]);
                while (AlignData.T.Count > DataCount) AlignData.T.RemoveAt(0);
            }

            for (int i = 0; i < count; i++)
            {
                var data = dataSets[i];
                if (data == null || data.Count == 0)
                    continue;

                // 데이터 크기 조정 (이미 위에서 처리했지만, 안전을 위해 여기서도 확인)
                while (data.Count > DataCount) data.RemoveAt(0);

                var nonZeroData = data.Where(d => d != 0).ToList();
                double avg = nonZeroData.Count > 0 ? Math.Round(nonZeroData.Average(), 2) : 0;
                double max = nonZeroData.Count > 0 ? Math.Round(nonZeroData.Max(), 2) : 0;
                double min = nonZeroData.Count > 0 ? Math.Round(nonZeroData.Min(), 2) : 0;
                double stdev = nonZeroData.Count > 1 ? Math.Round(Math.Sqrt(nonZeroData.Sum(d => Math.Pow(d - avg, 2)) / (nonZeroData.Count - 1)), 2) : 0;

                var mainPoints = new GearedValues<ObservablePoint>();
                for (int j = 0; j < data.Count; j++)
                {
                    mainPoints.Add(new ObservablePoint(j + 1, data[j]));
                }

                var mainSeries = new GLineSeries
                {
                    Values = mainPoints,
                    StrokeThickness = 2,
                    PointGeometry = DefaultGeometries.Circle,
                    DataLabels = true,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Foreground = System.Windows.Media.Brushes.White
                };

                _charts[i].Series.Clear();
                _charts[i].Series.Add(mainSeries);

                SetaxisY(i, avg, max, min, stdev);
                _charts[i].Update(true, true);
            }
        }

        private void SetaxisY(int num, params double[] values)
        {
            if (values.Length < 4)
                return;

            // Y축 섹션 표시
            var axisY = _charts[num].AxisY[0];
            if (axisY.Sections == null || axisY.Sections.Count != 4)
            {
                axisY.Sections = new SectionsCollection
                {
                    new AxisSection
                    {
                        DataContext = values[0], // avg
                        DataLabel = true,
                        Stroke = System.Windows.Media.Brushes.Orange,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4 }
                    },
                    new AxisSection
                    {
                        DataContext = values[1], // max
                        DataLabel = true,
                        Stroke = System.Windows.Media.Brushes.Red,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 2 }
                    },
                    new AxisSection
                    {
                        DataContext = values[2], // min
                        DataLabel = true,
                        Stroke = System.Windows.Media.Brushes.LimeGreen,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 2 }
                    },
                    new AxisSection
                    {
                        DataContext = values[3], // stdev
                        DataLabel = true,
                        Stroke = System.Windows.Media.Brushes.Blue,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 2 }
                    }
                };
            }

            axisY.Sections[0].Value = values[0];
            axisY.Sections[1].Value = values[1];
            axisY.Sections[2].Value = values[2];
            axisY.Sections[3].Value = values[3];
        }

        #region ContextMenu
        private void ResetZoom(LiveCharts.WinForms.CartesianChart chart)
        {
            chart.AxisX[0].MinValue = 1;
            chart.AxisX[0].MaxValue = DataCount;
            chart.AxisY[0].MinValue = double.NaN;
            chart.AxisY[0].MaxValue = double.NaN;
        }
        private void SaveAsCsv(LiveCharts.WinForms.CartesianChart chart)
        {
            string chartName = chart.Name;

            List<double> data = null;

            switch (chartName)
            {
                case "CS_AlignX":
                    data = AlignData.X;
                    break;
                case "CS_AlignY":
                    data = AlignData.Y;
                    break;
                case "CS_AlignT":
                    data = AlignData.T;
                    break;
                default:
                    return; // 잘못된 차트 이름이면 조용히 반환
            }

            if (data == null || data.Count == 0) return;

            // 통계값 계산 (0 제외)
            var nonZeroData = data.Where(d => d != 0).ToList();
            double avg = nonZeroData.Count > 0 ? Math.Round(nonZeroData.Average(), 4) : 0;
            double max = nonZeroData.Count > 0 ? Math.Round(nonZeroData.Max(), 4) : 0;
            double min = nonZeroData.Count > 0 ? Math.Round(nonZeroData.Min(), 4) : 0;
            double stdev = nonZeroData.Count > 1 ? Math.Round(Math.Sqrt(nonZeroData.Sum(d => Math.Pow(d - avg, 2)) / (nonZeroData.Count - 1)), 4) : 0;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV file|*.csv";
                sfd.FileName = $"{chartName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new StreamWriter(sfd.FileName))
                    {
                        writer.WriteLine("Index,Value");
                        for (int i = 0; i < data.Count; i++)
                            writer.WriteLine($"{i + 1},{data[i]:F4}");

                        writer.WriteLine();
                        writer.WriteLine("Summary");
                        writer.WriteLine($"AVG,{avg:F4}");
                        writer.WriteLine($"MAX,{max:F4}");
                        writer.WriteLine($"MIN,{min:F4}");
                        writer.WriteLine($"STDEV,{stdev:F4}");
                    }

                    System.Windows.Forms.MessageBox.Show("CSV 저장 완료!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ResetAllValues(LiveCharts.WinForms.CartesianChart chart)
        {
            AlignData.X = Enumerable.Repeat(0.0, DataCount).ToList();
            AlignData.Y = Enumerable.Repeat(0.0, DataCount).ToList();
            AlignData.T = Enumerable.Repeat(0.0, DataCount).ToList();

            // UpdateChart 필요
        }

        private void BTN_IMAGE_SAVE_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png";
                sfd.FileName = $"LiveChart_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    CaptureFormImage(this, sfd.FileName);
                }
            }
        }
        private void CaptureFormImage(UserControl userControl, string filePath)
        {
            using (Bitmap bmp = new Bitmap(userControl.Width, userControl.Height))
            {
                userControl.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void SaveAsImage(LiveCharts.WinForms.CartesianChart chart)
        {
            if (chart.Series.Count == 0 || chart.Series[0].Values.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("데이터가 없는 차트는 저장할 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png";
                sfd.FileName = $"{chart.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    SaveControlAsImage(chart, sfd.FileName);
                    System.Windows.Forms.MessageBox.Show("차트 이미지 저장 완료!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private void SaveControlAsImage(Control control, string filePath)
        {
            using (Bitmap bmp = new Bitmap(control.Width, control.Height))
            {
                control.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        #endregion

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
            var panel = sender as TableLayoutPanel;
            var g = e.Graphics;
            var pen = new System.Drawing.Pen(System.Drawing.Color.Gray, 2); // 경계 색과 두께 설정

            // 가로줄 (Row 경계)
            for (int i = 1; i < panel.RowCount; i++)
            {
                int y = panel.GetRowHeights().Take(i).Sum();
                g.DrawLine(pen, 0, y, panel.Width, y);
            }

            // 세로줄 (Column 경계)
            for (int j = 1; j < panel.ColumnCount; j++)
            {
                int x = panel.GetColumnWidths().Take(j).Sum();
                g.DrawLine(pen, x, 0, x, panel.Height);
            }

            pen.Dispose();
        }
    }
}
