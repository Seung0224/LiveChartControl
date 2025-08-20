# LiveCharts WinForms Controls

Windows 환경에서 **LiveCharts**(WinForms/WPF/Geared)를 활용해 다음 4가지 시각화 컨트롤을 제공하는 **WinForms 사용자 컨트롤 모음**입니다.

* **LiveSolidControl** — CPU / 메모리 / 디스크 / 네트워크 사용률을 실시간 게이지로 표시
* **LivePieChartControl** — OK/NG 등 카테고리 비율 파이차트 + 중앙 Total 라벨
* **LiveCartesianChartMontlyControl** — 일자(yyyyMMdd) 기준 누적 카운트 막대차트 + 30일 로그 입출력
* **LiveCartesianChartAlignLogControl** — X/Y/T 3축 정렬 데이터 시계열 + MAX/MIN/AVG/STDEV 보조선

모든 컨트롤은 **컨텍스트 메뉴**(Zoom Reset, Save as CSV, Save as Image, Reset All Values)를 지원합니다.

---

## 📦 프로젝트 개요

* **플랫폼:** Windows 10/11, .NET Framework/WinForms
* **라이브러리:** LiveCharts, LiveCharts.WinForms, LiveCharts.Wpf, LiveCharts.Geared (선택)
* **목적:** 생산/테스트 현장의 실시간 지표와 집계 로그를 가볍게 시각화

---

## ✅ 구성 컨트롤 & 기능

### 1) LiveSolidControl — 시스템 리소스 게이지

* 4개의 **SolidGauge**로 **CPU / MEM / DISK / NET** 표시
* Windows **PerformanceCounter** 기반 실시간 갱신, 네트워크 인터페이스 자동 선택(Ethernet/Wi‑Fi 등)
* 값 구간에 따른 **고정 색상 브러시**(LimeGreen → Green → Orange → Red)
* 글꼴/배경 등 기본 테마 프리셋 포함

> 네임스페이스: `LiveSolidGaugeControl`

**빠른 사용 예**

```csharp
var gauge = new LiveSolidGaugeControl.LiveSolidControl();
gauge.Dock = DockStyle.Fill;
this.Controls.Add(gauge);
```

---

### 2) LivePieChartControl — 카테고리 파이차트

* 기본 시리즈: **OK (LimeGreen), NG (Red)** / 필요 시 **동적 시리즈 추가**
* **중앙 Total 라벨** 자동 표시
* **값 설정/증가 API**: `SetValue(label, value)`, `IncrementValue(label, amount)`
* 자정 롤오버 시 **자동 리셋**(옵션 타이머)
* 컨텍스트 메뉴: **CSV/이미지 저장, 값 리셋**

> 네임스페이스: `PieChartControl`

**빠른 사용 예**

```csharp
var pie = new PieChartControl.LivePieChartControl();
pie.SetSeries(
    Tuple.Create("OK", Color.LimeGreen),
    Tuple.Create("NG", Color.Red),
    Tuple.Create("RETRY", Color.Orange)
);
pie.IncrementValue("OK");
this.Controls.Add(pie);
```

---

### 3) LiveCartesianChartMontlyControl — 월간 집계 막대차트

* 날짜 라벨(`yyyyMMdd`) 기반 **일별 누적 카운트** 표시(OK/NG 등 다중 시리즈)
* `InitializeLogPath()` 호출 시 **로그 폴더 생성** 및 과거 로그 로드
* 런타임 누적 시 자동으로 **파일 저장**(`SaveLogsToFile`) 및 **폴더 로드**(`LoadLogsFromFolder`)
* 컨텍스트 메뉴: **Zoom Reset, CSV/이미지 저장, 전체 값 초기화**
* **줌/패닝**(X/Y) 지원, 툴팁 공유 모드

> 네임스페이스: `LiveCartesianChartControl`

**빠른 사용 예**

```csharp
var monthly = new LiveCartesianChartControl.LiveCartesianChartMontlyControl();
monthly.InitializeLogPath(); // 기본: 바탕화면/LiveChartsLogs(30Days)
monthly.IncrementValue("OK", 1);
this.Controls.Add(monthly);
```

---

### 4) LiveCartesianChartAlignLogControl — 정렬 로그(X/Y/T) 라인차트

* 3개의 CartesianChart로 **X / Y / T** 시계열 데이터 동시 표시
* **데이터 최대 개수** 지정(`SetDataMaxCount`) 및 초과 시 **선두 절삭(rolling)**
* **평균/최대/최소/표준편차**를 Y축 **AxisSection**으로 오버레이 표시
* CSV 저장 시 **Summary(AVG/MAX/MIN/STDEV)** 자동 기록, 이미지 저장 지원
* 컨텍스트 메뉴: Zoom Reset, CSV/이미지 저장, Reset

> 네임스페이스: `LiveCartesianChartAlignLogControl`

**빠른 사용 예**

```csharp
var align = new LiveCartesianChartAlignLogControl.LiveCartesianChartAlignLogControl();
align.SetDataMaxCount(20);
align.SetValue(xList, yList, tList); // 각 List<double>
this.Controls.Add(align);
```

---

## 🧩 공통 유틸리티

* **ChartContextMenuHelper**: 차트 컨트롤에 표준 컨텍스트 메뉴(Zoom Reset / Save as CSV / Save as Image / Reset All Values)를 부착
* **IContextMenuSupport**: 각 컨트롤이 자체 컨텍스트 메뉴 초기화를 노출하도록 하는 인터페이스

---

## 🔧 설치

NuGet에서 LiveCharts 패키지를 설치한 뒤, 솔루션에 본 컨트롤 프로젝트를 참조(Add Reference)하세요.

```powershell
Install-Package LiveCharts
Install-Package LiveCharts.WinForms
Install-Package LiveCharts.Wpf
Install-Package LiveCharts.Geared   # 필요한 경우
```

---

## 🧰 개발/실행 환경

* **언어/런타임:** C#, .NET Framework/WinForms
* **IDE:** Visual Studio 2019+
* **OS:** Windows 10/11 (x64)

---

## ❓ FAQ

* **파이차트 값이 누적 갱신되나요?** → `IncrementValue(label)` 사용 시 현재 날짜에 대해 누적됩니다(월간 차트는 날짜 라벨 기준 자동 확장).
* **로그 저장/로드 경로는?** → `InitializeLogPath()`로 지정(미지정 시 바탕화면 기준 하위 폴더 생성).
* **데이터가 많아지면?** → AlignLog 컨트롤은 `SetDataMaxCount()`로 표시 최대 수를 제한합니다.

---

## 🗓️ 변경 이력

* v0.1.0: 초기 공개 — 4종 컨트롤/컨텍스트 메뉴/CSV&이미지 저장
