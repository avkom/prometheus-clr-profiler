# [WIP] Real-time CLR profiler to build flame graphs in Grafana and store historical data in Prometheus

## Getting Started

1. Setup prometheus-clr-profiler.
    * `git clone https://github.com/avkom/prometheus-clr-profiler.git`
    * Build solution in Visual Studio (tested with VS 2017).
    * Run `PrometheusClrProfiler.exe` with elevated privileges. 
      * You can start Visual Studio with elevated privileges, so if you run `PrometheusClrProfiler.exe` from Visual Studio, it will be run with elevated privileges too.
    * Open http://localhost:9999/metrics in browser. You will see a page that contains metrics in Prometheus format like the following:
```
# HELP cpu_sample CPU Sample
# TYPE cpu_sample counter
cpu_sample{stack="System.Threading.ThreadHelper.ThreadStart;System.Threading.ExecutionContext.Run;System.Threading.ExecutionContext.Run;System.Threading.ExecutionContext.RunInternal;System.Threading.ThreadHelper.ThreadStart_Context;System.Threading.Tasks.ThreadPoolTaskScheduler.LongRunningThreadWork;System.Threading.Tasks.Task.ExecuteEntry;System.Threading.Tasks.Task.ExecuteWithThreadLocal;System.Threading.ExecutionContext.Run;System.Threading.ExecutionContext.RunInternal;System.Threading.Tasks.Task.ExecutionContextCallback;System.Threading.Tasks.Task.Execute;System.Threading.Tasks.Task.InnerInvoke;Roslyn.Utilities.TaskFactoryExtensions+<>c__DisplayClass1_0.<SafeStartNew>g__wrapped|0;Microsoft.CodeAnalysis.Diagnostics.DiagnosticEventTaskScheduler.Start;System.Collections.Concurrent.BlockingCollection<System.__Canon>.Take;System.Collections.Concurrent.BlockingCollection<System.__Canon>.TryTake;System.Collections.Concurrent.BlockingCollection<System.__Canon>.TryTakeWithNoTimeValidation;System.Threading.SemaphoreSlim.Wait;System.Threading.SemaphoreSlim.WaitUntilCountOrTimeout;System.Threading.Monitor.Wait;System.Threading.Monitor.ObjWait"} 31
```
2. Setup Prometheus.
    * Download Prometheus from https://prometheus.io/download/
      * Tested with `prometheus-2.6.0.windows-amd64.tar.gz`
    * Unpack the archive.
    * Add the following settings to `scrape_configs` section to `prometheus.yml` file:
```
  - job_name: 'clr-profiler'
    static_configs:
    - targets: ['localhost:9999']
```
    * Run `prometheus --config.file=prometheus.yml`. Allow access if Windows dialog appears.
    * You can verify that Prometheus is serving metrics about itself by navigating to its own metrics endpoint: http://localhost:9090/metrics
    * More details on configuring Prometheus: https://prometheus.io/docs/introduction/first_steps/
    * Verify Prometeus pulls metrics from `prometheus-clr-profiler` on a page `http://localhost:9090/graph`: enter metric name `cpu_sample` and press Enter. You must see a list of cpu_sample metrics with different labels.
3. Setup Grafana.
    * Download Grafana from https://grafana.com/grafana/download
      * The plugin works only with https://grafana.com/grafana/download/4.4.3?platform=windows
    * See details about installing Grafana on Windows: http://docs.grafana.org/installation/windows/
4. Setup grafana-flamegraph-panel Grafana plugin.
    * Copy `dist` folder of https://github.com/samber/grafana-flamegraph-panel to grafana's `data\plugins\` folder.
    * Set Metrics -> A: `cpu_sample`
    * Set Options -> Signature column name: `stack`
    * Set Options -> Separator character: `;`
