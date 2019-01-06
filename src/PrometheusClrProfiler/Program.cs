using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Runtime;
using Prometheus;

namespace PrometheusClrProfiler
{
	class Program
	{
		private static CancellationTokenSource _cancellationTokenSource;
		private static Counter _cpuSampleCounter;

		static void Main()
		{
			var metricServer = new MetricServer(9999);
			metricServer.Start();

			_cpuSampleCounter = Metrics.CreateCounter("cpu_sample", "CPU Sample", "stack");

			_cancellationTokenSource = new CancellationTokenSource();
			Console.CancelKeyPress += (sender, e) => _cancellationTokenSource.Cancel();

			Process[] processes = Process.GetProcessesByName("devenv");

			using (DataTarget dataTarget = DataTarget.AttachToProcess(processes[0].Id, 5000, AttachFlag.Passive))
			{
				ClrRuntime runtime = CreateClrRuntime(dataTarget);

				RunPeriodic(runtime, TimeSpan.FromSeconds(1), _cancellationTokenSource.Token);
			}
		}

		private static void RunPeriodic(ClrRuntime runtime, TimeSpan interval, CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				SampleCpu(runtime);
				Thread.Sleep(interval);
			}
		}

		private static ClrRuntime CreateClrRuntime(DataTarget dataTarget)
		{
			// Now check bitness of our program/target:
			bool isTarget64Bit = dataTarget.PointerSize == 8;
			if (Environment.Is64BitProcess != isTarget64Bit)
			{
				throw new Exception(
					$"Architecture mismatch:  Process is {(Environment.Is64BitProcess ? "64 bit" : "32 bit")} but target is {(isTarget64Bit ? "64 bit" : "32 bit")}");
			}

			// Note I just take the first version of CLR in the process.  You can loop over every loaded
			// CLR to handle the SxS case where both v2 and v4 are loaded in the process.
			ClrInfo version = dataTarget.ClrVersions[0];
			ClrRuntime runtime = version.CreateRuntime();
			return runtime;
		}

		private static void SampleCpu(ClrRuntime runtime)
		{
			foreach (ClrThread thread in runtime.Threads)
			{
				StringBuilder sb = new StringBuilder();

				for (int i = thread.StackTrace.Count - 1; i >= 0; i--)
				{
					ClrStackFrame stackFrame = thread.StackTrace[i];
					if (!string.IsNullOrWhiteSpace(stackFrame.Method?.Type?.Name) ||
					    !string.IsNullOrWhiteSpace(stackFrame.Method?.Name))
					{
						sb.Append($"{stackFrame.Method?.Type?.Name}.{stackFrame.Method?.Name};");
					}
				}

				// Trim the last ';'
				if (sb.Length > 0 && sb[sb.Length - 1] == ';')
				{
					sb.Length = sb.Length - 1;
				}

				string stackString = sb.ToString();
				if (!string.IsNullOrWhiteSpace(stackString))
				{
					ReportStackTrace(stackString);
				}

				runtime.Flush();
			}
		}

		private static void ReportStackTrace(string stackTrace)
		{
			Console.WriteLine(stackTrace);
			_cpuSampleCounter.WithLabels(stackTrace).Inc();
		}
	}
}
