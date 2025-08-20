using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LiveCharts;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using CommonLibrary.Helpers;
using CommonLibrary.Interfaces;

namespace LiveCartesianChartControl
{
    public partial class LiveCartesianChartMontlyControl : UserControl, IContextMenuSupport
    {
        private string _logDirectory;

        private List<string> _dates = new List<string>(); 
        private readonly object _logLock = new object();
        private Dictionary<string, GColumnSeries> _seriesDict = new Dictionary<string, GColumnSeries>();
        private Dictionary<string, GearedValues<int>> _valueDict = new Dictionary<string, GearedValues<int>>();

        public LiveCartesianChartMontlyControl()
        {
            InitializeComponent();
            InitChart(
               new Tuple<string, Color>("OK", Color.LimeGreen),
               new Tuple<string, Color>("NG", Color.Red)
            );
        }

        public LiveCartesianChartMontlyControl(params Tuple<string, Color>[] seriesInfo)
        {
            InitializeComponent();
            InitChart(seriesInfo);
        }

        private void InitChart(params Tuple<string, Color>[] seriesInfo)
        {
            cartesianChart1.Dock = DockStyle.Fill;
            cartesianChart1.DisableAnimations = true;
            cartesianChart1.BackColor = Color.FromArgb(51, 51, 51);

            cartesianChart1.DataTooltip = new DefaultTooltip
            {
                SelectionMode = TooltipSelectionMode.SharedYValues,
                Background = System.Windows.Media.Brushes.DimGray,
                Foreground = System.Windows.Media.Brushes.White,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 14
            };

            cartesianChart1.AxisX.Clear();
            cartesianChart1.AxisY.Clear();

            cartesianChart1.AxisX.Add(new Axis
            {
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.WhiteSmoke,
                Labels = _dates,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            });

            cartesianChart1.AxisY.Add(new Axis
            {
                Title = "갯수",
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.WhiteSmoke,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                LabelFormatter = value => ((int)value).ToString(),
                MinValue = 0,
            });

            var seriesCollection = new SeriesCollection();
            _seriesDict.Clear();
            _valueDict.Clear();

            foreach (var item in seriesInfo)
            {
                string label = item.Item1;
                Color color = item.Item2;

                var brush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));

                var values = new GearedValues<int>();

                var series = new GColumnSeries
                {
                    Title = label,
                    Values = values,
                    DataLabels = true,
                    LabelPoint = point => $"{point.Y}",
                    Fill = brush,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.WhiteSmoke,
                    ColumnPadding = 9
                };

                seriesCollection.Add(series);
                _seriesDict[label] = series;
                _valueDict[label] = values;
            }

            cartesianChart1.Series = seriesCollection;
            cartesianChart1.Zoom = ZoomingOptions.Xy;
            cartesianChart1.Pan = PanningOptions.Xy;
            InitializeContextMenu();
        }

        public void InitializeContextMenu()
        {
            ChartContextMenuHelper.ApplyDefaultContextMenu(cartesianChart1, ResetZoom, SaveAsCsv, SaveAsImage, ResetAllValues);
        }

        public void InitializeLogPath(string customPath = null)
        {
            string basePath;

            if (!string.IsNullOrEmpty(customPath))
            {
                basePath = customPath;
            }
            else
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            _logDirectory = Path.Combine(basePath, "LiveChartsLogs(30Days)");

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            LoadLogsFromFolder(_logDirectory);
        }

        /// <summary>
        /// 기본 생성자는 OK(초록색), NG(빨간색) 시리즈가 자동으로 생성됩니다.
        /// SetSeries 사용 시 Series의 이름과 색깔 선택 시 자동으로 시리즈가 추가 됩니다.
        /////////////////////// ForExample ///////////////////////
        ///  liveCartesianChartControl1.SetSeries(
        /// Tuple.Create("OK", Color.LimeGreen),
        /// Tuple.Create("NG", Color.Red),
        /// Tuple.Create("RETRY", Color.Orange)
        /// );
        /// </summary>
        /// 
        public void SetSeries(params Tuple<string, Color>[] seriesInfo)
        {
            _dates.Clear();
            _seriesDict.Clear();
            _valueDict.Clear();
            cartesianChart1.Series.Clear();
            cartesianChart1.AxisX.Clear();
            cartesianChart1.AxisY.Clear();

            InitChart(seriesInfo);
        }

        public void SaveLogsToFile()
        {
            lock (_logLock)
            {
                if (string.IsNullOrEmpty(_logDirectory)) return;

                string today = DateTime.Now.ToString("yyyyMMdd");
                string filePath = Path.Combine(_logDirectory, $"{today}.log");

                using (StreamWriter sw = new StreamWriter(filePath, false)) // overwrite
                {
                    int index = _dates.IndexOf(today);
                    if (index < 0) return;

                    foreach (var kv in _valueDict)
                    {
                        string label = kv.Key;
                        int value = kv.Value.Count > index ? kv.Value[index] : 0;
                        sw.WriteLine($"{label}:{value}");
                    }
                }
            }
        }

        public void LoadLogsFromFolder(string folderPath)
        {
            var files = Directory.GetFiles(folderPath, "*.log")
                                 .OrderBy(f => f)
                                 .ToList();

            _dates.Clear();
            foreach (var v in _valueDict.Values)
                v.Clear();

            foreach (var file in files)
            {
                string date = Path.GetFileNameWithoutExtension(file); // yyyyMMdd
                _dates.Add(date);

                var lines = File.ReadAllLines(file);
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length != 2) continue;

                    string label = parts[0];
                    if (!_valueDict.ContainsKey(label)) continue;

                    if (!int.TryParse(parts[1], out int value)) continue;

                    _valueDict[label].Add(value);
                }

                // 누락된 label을 0으로 채움
                foreach (var kv in _valueDict)
                {
                    if (kv.Value.Count < _dates.Count)
                    {
                        while (kv.Value.Count < _dates.Count)
                            kv.Value.Add(0);
                    }
                }
            }

            cartesianChart1.AxisX[0].Labels = _dates;

            // 시리즈에 기존 _valueDict의 참조를 유지
            foreach (var kv in _seriesDict)
            {
                kv.Value.Values = _valueDict[kv.Key];
            }

            cartesianChart1.Update(true, true);
        }

        public void IncrementValue(string label, int count = 1, bool saveflag = true)
        {
            if (!_valueDict.ContainsKey(label))
                return;

            string today = DateTime.Now.ToString("yyyyMMdd");

            if (!_dates.Contains(today))
            {
                _dates.Add(today);

                foreach (var _values in _valueDict.Values)
                {
                    _values.Add(0); // 새로운 날짜에 대해 모든 시리즈에 0 추가
                }

                cartesianChart1.AxisX[0].Labels = _dates;
            }

            int index = _dates.IndexOf(today);

            // 값 누적
            var values = _valueDict[label];

            // 인덱스가 범위를 벗어나지 않도록 확장
            while (values.Count <= index)
                values.Add(0);

            // 값 증가
            values[index] += count;

            // 시리즈의 Values가 _valueDict[label]를 참조하도록 보장
            _seriesDict[label].Values = values;

            // 차트 강제 갱신
            cartesianChart1.Update(true, true); // 데이터와 UI 모두 갱신

            if (saveflag)
            {
                SaveLogsToFile();
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            SaveLogsToFile();
        }

        #region ContextMenu
        private void ResetZoom()
        {
            cartesianChart1.AxisX[0].MinValue = double.NaN;
            cartesianChart1.AxisX[0].MaxValue = double.NaN;
            cartesianChart1.AxisY[0].MinValue = double.NaN;
            cartesianChart1.AxisY[0].MaxValue = double.NaN;
        }
        private void SaveAsCsv()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV file|*.csv";
                sfd.FileName = $"CartesianChart_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter writer = new StreamWriter(sfd.FileName))
                    {
                        // 헤더 작성: "Date,OK,NG,..." 형태
                        writer.Write("Date");
                        foreach (var label in _seriesDict.Keys)
                        {
                            writer.Write($",{label}");
                        }
                        writer.WriteLine();

                        // 데이터 작성
                        for (int i = 0; i < _dates.Count; i++)
                        {
                            writer.Write(_dates[i]);
                            foreach (var kv in _valueDict)
                            {
                                int value = kv.Value.Count > i ? kv.Value[i] : 0;
                                writer.Write($",{value}");
                            }
                            writer.WriteLine();
                        }

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
                sfd.FileName = $"CartesianChart_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // CartesianChart를 이미지로 렌더링
                    using (Bitmap bmp = new Bitmap(cartesianChart1.Width, cartesianChart1.Height))
                    {
                        cartesianChart1.DrawToBitmap(bmp, new Rectangle(0, 0, cartesianChart1.Width, cartesianChart1.Height));
                        bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    MessageBox.Show("차트 이미지 저장 완료!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        public void ResetAllValues()
        {
            foreach (var values in _valueDict.Values)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    values[i] = 0; // 모든 날짜의 값을 0으로 초기화
                }
            }

            // 시리즈와 차트 갱신
            foreach (var kv in _seriesDict)
            {
                kv.Value.Values = _valueDict[kv.Key];
            }
            cartesianChart1.Update(true, true);

            SaveLogsToFile(); // 변경된 값을 로그에 저장
        }
        #endregion
    }
}
