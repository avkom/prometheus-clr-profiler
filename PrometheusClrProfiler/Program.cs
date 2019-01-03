using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Diagnostics.Runtime;

namespace PrometheusClrProfiler
{
	class Program
	{
		static void Main(string[] args)
		{
			Process[] processes = Process.GetProcessesByName("devenv");

			using (DataTarget dataTarget = DataTarget.AttachToProcess(processes[0].Id, 5000, AttachFlag.Passive))
			{
				ClrRuntime runtime = CreateClrRuntime(dataTarget);

				SampleCpu(runtime);
			}

			Console.ReadLine();
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

			// Next, let's try to make sure we have the right Dac to load.  Note we are doing this manually for
			// illustration.  Simply calling version.CreateRuntime with no arguments does the same steps.
			string dac = dataTarget.SymbolLocator.FindBinary(version.DacInfo);

			// Finally, check to see if the dac exists.  If not, throw an exception.
			if (dac == null || !File.Exists(dac))
			{
				throw new FileNotFoundException("Could not find the specified dac.", dac);
			}

			// Now that we have the DataTarget, the version of CLR, and the right dac, we create and return a
			// ClrRuntime instance.
			ClrRuntime runtime = version.CreateRuntime(dac);
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
			}
		}

		private static void ReportStackTrace(string stackTrace)
		{
			Console.WriteLine(stackTrace);
		}
	}
}
