using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using System.Windows.Media;
using System.ComponentModel;
using LiveCharts.WinForms;

namespace LiveSolidGaugeControl
{
    public partial class LiveSolidControl : UserControl
    {
        private float totalMemoryMB;
        private float maxNetworkSpeedBps = 125_000f; // ✅ 1Mbps 기준

        // Gauge 정보 리스트
        private List<GaugeInfo> _gaugeInfos;

        public LiveSolidControl()
        {
            InitializeComponent();
            InitPerformanceCounters();
            InitGaugeInfos();
            InitGaugeTheme();
        }

        private void InitPerformanceCounters()
        {
            totalMemoryMB = new ComputerInfo().TotalPhysicalMemory / (1024f * 1024f);
        }


        private void InitGaugeInfos()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var memAvailableCounter = new PerformanceCounter("Memory", "Available MBytes");
            var diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

            string netInterface = new PerformanceCounterCategory("Network Interface")
                .GetInstanceNames()
                .FirstOrDefault(name => name.Contains("Ethernet") || name.Contains("Wi-Fi") || name.Contains("무선"));

            var netCounter = netInterface != null
                ? new PerformanceCounter("Network Interface", "Bytes Total/sec", netInterface)
                : null;

            // 미리 NextValue 초기화
            _ = cpuCounter.NextValue();
            _ = diskCounter.NextValue();
            _ = netCounter?.NextValue();

            _gaugeInfos = new List<GaugeInfo>
            {
                new GaugeInfo
                {
                    Gauge = solidGauge1,
                    Label = "CPU",
                    GetValue = () => cpuCounter.NextValue()
                },
                new GaugeInfo
                {
                    Gauge = solidGauge2,
                    Label = "MEM",
                    GetValue = () =>
                    {
                        float available = memAvailableCounter.NextValue();
                        return ((totalMemoryMB - available) / totalMemoryMB) * 100;
                    }
                },
                new GaugeInfo
                {
                    Gauge = solidGauge3,
                    Label = "DIS",
                    GetValue = () => diskCounter.NextValue()
                },
                new GaugeInfo
                {
                    Gauge = solidGauge4,
                    Label = "NET",
                    GetValue = () =>
                    {
                        float net = netCounter != null ? netCounter.NextValue() / maxNetworkSpeedBps * 100 : 0f;
                        return Math.Min(net, 100f);
                    }
                }
            };
        }

        private void InitGaugeTheme()
        {
            foreach (var info in _gaugeInfos)
            {
                info.Gauge.From = 0;
                info.Gauge.To = 100;
                info.Gauge.Base.HighFontSize = 14;
                info.Gauge.FontFamily = new System.Windows.Media.FontFamily("Arial");
                info.Gauge.Base.GaugeBackground = System.Windows.Media.Brushes.LightBlue;
            }
        }
        private System.Windows.Media.Brush GetFixedRangeBrush(float value)
        {
            if (value <= 31)
                return System.Windows.Media.Brushes.LimeGreen;
            else if (value <= 51)
                return System.Windows.Media.Brushes.Green;
            else if (value <= 71)
                return System.Windows.Media.Brushes.Orange;
            else
                return System.Windows.Media.Brushes.Red;
        }


        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            foreach (var info in _gaugeInfos)
            {
                float value = info.GetValue();
                info.Gauge.Value = value;

                info.Gauge.Base.GaugeActiveFill = GetFixedRangeBrush(value);
                // 텍스트

                info.Gauge.Base.LabelFormatter = val => $"{val:F0}%\n{info.Label}";
            }
        }

        /*
        private void AppendLog(string message)
        {
            try
            {
                File.AppendAllText(_logDirectory, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // MessageBox.Show($"로그 저장 실패: {ex.Message}");
            }
        }

        private string _logDirectory;
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

            _logDirectory = Path.Combine(basePath, "LiveMemoryLogs");

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }
        */

        // 내부 전용 정보 클래스
        internal class GaugeInfo
        {
            public LiveCharts.WinForms.SolidGauge Gauge { get; set; }
            public string Label { get; set; }
            public Func<float> GetValue { get; set; }
            public bool IsLogged { get; set; } = false;
        }

    }
}
