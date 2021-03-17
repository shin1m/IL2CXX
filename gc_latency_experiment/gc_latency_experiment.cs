using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using HdrHistogram;

namespace gc_latency_experiment
{
    // See https://github.com/spion/hashtable-latencies
    // and https://gitlab.com/frje/gc-latency-experiment/blob/master/Main.cs
    // and http://prl.ccs.neu.edu/blog/2016/05/24/measuring-gc-latencies-in-haskell-ocaml-racket/
    // and https://blog.pusher.com/latency-working-set-ghc-gc-pick-two/
    // and https://blog.pusher.com/golangs-real-time-gc-in-theory-and-practice/
    // and https://mattwarren.org/2017/01/13/Analysing-Pause-times-in-the-.NET-GC/
    class Main_
    {
        //private const int windowSize = 200000; // 200,000
        private static int windowSize = 200000; // 200,000
        private const int msgCount = 10000000; // 10,000,000 
        private const int msgSize = 1024;      // 1,024

        private static void pushMessage(Dictionary<int, byte[]> map, int id)
        {
            var lowId = id - windowSize;
            map.Add(id, createMessage(id));
            if (lowId >= 0)
                map.Remove(lowId);
        }

        private static unsafe void pushMessage(byte[][] array, int id, bool offHeap)
        {
            if (offHeap)
            {
                //System.Diagnostics.Debugger.Launch();
                var dest = array[id % windowSize];
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(dest.Length);
                byte* bytePtr = (byte *) unmanagedPointer;
                for (int i = 0; i < dest.Length; ++i)
                {
                    *(bytePtr + i) = (byte)id;
                }

                // Copy the unmanaged byte array (byte*) into the managed one (byte[])
                Marshal.Copy(unmanagedPointer, dest, 0, dest.Length);

                Marshal.FreeHGlobal(unmanagedPointer);
            }
            else
            {
                array[id % windowSize] = createMessage(id);               
            }
        }

        private static byte[] createMessage(int n)
        {
            var data = new byte[msgSize];
            for (int i = 0; i < data.Length; ++i)
                data[i] = (byte)n;
            return data;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting GC Latency Experiment - {0} - {1} - {2}",
                Environment.Is64BitProcess ? "64-bit" : "32-bit", 
                GCSettings.IsServerGC ? "SERVER" : "WORKSTATION",
                GCSettings.LatencyMode);

            //A single tick represents one hundred nanoseconds or one ten-millionth of a second. 
            //There are 10,000 ticks in a millisecond, or 10 million ticks in a second.
            // https://msdn.microsoft.com/en-us/library/system.datetime.ticks(v=vs.110).aspx

            // A Histogram covering the range from 100 nano-seconds to 1 hour (3,600,000,000,000 ns) with 3 decimal point resolution:
            //var histogram = new LongHistogram(TimeSpan.TicksPerHour, 3);
            // A Histogram covering the range from 100 nano-seconds to 1 second with 3 decimal point resolution:
            //var histogram = new LongHistogram(TimeSpan.TicksPerSecond, 3);
            var histogram = new LongHistogram(Stopwatch.Frequency, 3);
            
            var modeName = "NoGCRegion".ToLower();
            var noGcRegionMode = args.Any(a => a.ToLower().Contains(modeName));
            if (noGcRegionMode)
            {
                //var totalAllocs = 23500L * 1024 * 1024; // As per PerfView 23,500 MB is more than we allocate
                //var totalAllocs = 16000L * 1024 * 1024; // Max we can get (should be able to get)
                var totalAllocs = 10000L * 1024 * 1024;
                var startNoGcRegionTimer = Stopwatch.StartNew();
                var modeEntered = GC.TryStartNoGCRegion(totalAllocs);
                startNoGcRegionTimer.Stop();
                
                if (modeEntered == false)
                {
                    Console.WriteLine($"Unable to Start No GC Region mode - requested size = {totalAllocs:N0} bytes ({totalAllocs / 1024.0 / 1024.0:N2} MB)");
                    return;
                }
                else
                {
                    Console.WriteLine($"SUCCESSFULLY started GC Region mode, {totalAllocs:N0} bytes ({totalAllocs / 1024.0 / 1024.0:N2} MB), took {startNoGcRegionTimer.ElapsedMilliseconds:N0} ms");                
                } 
            }
            
            if (noGcRegionMode)
                Console.WriteLine($"\nBefore - GC Mode: {GCSettings.LatencyMode}");

            Console.WriteLine("\nBefore");
            PrintProcessMemoryInfo();

            var best = new TimeSpan(hours: 0, minutes: 0, seconds: 60);
            var worst = new TimeSpan();

            var windowSizeRaw = (args.FirstOrDefault(a => a.ToLower().Contains("windowsize")) ?? "")
                                    .Trim('-').Replace("=", "")
                                    .ToLower().Replace("windowsize", "");
            //Console.WriteLine($"\nWindow Size Raw = \'{windowSizeRaw}\'");
            int altWindowSize;
            if (string.IsNullOrWhiteSpace(windowSizeRaw) == false && int.TryParse(windowSizeRaw, out altWindowSize))
            {                
                windowSize = altWindowSize;
                Console.WriteLine($"\nUsing alternative windowSize = {windowSize:N0}");
            }

            var presize = args.Any(a => a.ToLower().Contains("presize"));
            var map = presize ? new Dictionary<int, byte[]>(windowSize) : new Dictionary<int, byte[]>();
            if (presize)
                Console.WriteLine($"\nPre-sizing the Dictionary to contain {windowSize:N0} items");

            var offHeap = args.Any(a => a.ToLower().Contains("offheap"));
            if (offHeap)
                Console.WriteLine($"\nGoing off heap for the byte arrays (also implies 'useArray')");

            var useArray = args.Any(a => a.ToLower().Contains("usearray"));
            var array = (useArray || offHeap) ? new byte[windowSize][] : null;
            if (useArray)
                Console.WriteLine($"\nUsing an Array instead of a Dictionary to store the messages/items");

            if (offHeap)
            {
                for (int i = 0; i < array.Length; i++)
                    array[i] = new byte[msgSize];
            }

            var totalTime = Stopwatch.StartNew();
            for (var i = 0; i < msgCount; i++)
            {
                var sw = Stopwatch.StartNew();
                if (useArray || offHeap)
                    pushMessage(array, i, offHeap);
                else
                    pushMessage(map, i);
                sw.Stop();

                if (sw.Elapsed > worst)
                    worst = sw.Elapsed;                
                else if (sw.Elapsed < best)
                    best = sw.Elapsed;

                histogram.RecordValue(sw.ElapsedTicks);
            }
            totalTime.Stop();

            if (noGcRegionMode)
                Console.WriteLine($"\nAfter  - GC Mode: {GCSettings.LatencyMode}");

            Console.WriteLine("\nAfter");
            PrintProcessMemoryInfo();

            Console.WriteLine($"\nTotal Time : {totalTime.Elapsed} ({totalTime.Elapsed.TotalMilliseconds:N4} ms)\n");
            Console.WriteLine($"Best push time  : {best.TotalMilliseconds,9:N4} ms");
            Console.WriteLine($"Worst push time : {worst.TotalMilliseconds,9:N4} ms");
            //Console.ReadLine();

            ProcessHistogrmaResults(histogram);
        }

        private static void ProcessHistogrmaResults(LongHistogram histogram)
        {
            Console.WriteLine();
            var percentiles = new[] {50.0, 90.0, 95.0, 99.9, 99.99, 99.999, 99.9999, 99.99999, 99.999999, 100.0};
            foreach (var percentile in percentiles)
            {
                var value = histogram.GetValueAtPercentile(percentile)/OutputScalingFactor.TimeStampToMilliseconds;
                Console.WriteLine($"{percentile,10:##.######}th Percentile : {value,9:N4} ms");
            }
            Console.WriteLine(
                $"                    Max : {histogram.GetMaxValue()/OutputScalingFactor.TimeStampToMilliseconds,9:N4} ms");

            var rootFolder = ".";
            var fileName = Path.Combine(rootFolder, "HistogramResults.hgrm");
            if (File.Exists(fileName))
                File.Delete(fileName);
            using (var writer = new StreamWriter(fileName))
            {
                histogram.OutputPercentileDistribution(writer, outputValueUnitScalingRatio: OutputScalingFactor.TimeStampToMilliseconds);
            }
        }

        private static void PrintProcessMemoryInfo()
        {
            var currentProcess = Process.GetCurrentProcess();
            Console.WriteLine($"Process - PrivateMemory:      {currentProcess.PrivateMemorySize64,15:N0} bytes");
            Console.WriteLine($"Process - Peak WorkingSet:    {currentProcess.PeakWorkingSet64,15:N0} bytes");
            Console.WriteLine($"Process - Peak PagedMemory:   {currentProcess.PeakPagedMemorySize64,15:N0} bytes");
            Console.WriteLine($"Process - Peak VirtualMemory: {currentProcess.PeakVirtualMemorySize64,15:N0} bytes");
        }
    }    
}
