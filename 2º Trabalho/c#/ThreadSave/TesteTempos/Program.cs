using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ThreadSave;

namespace TesteTempos {
    class Program {
        static void Main(string[] args) {
            int iteration = 25000000, warmUp = 250000000;
            int THREADS = Environment.ProcessorCount;
            object ignored;
            Thread[] threads = new Thread[THREADS];
            ManualResetEventSlim start = new ManualResetEventSlim(false);
            Stopwatch sw = Stopwatch.StartNew();

            Console.WriteLine("--warm up start");
            for (int i = 0; i < warmUp; ++i)
                Interlocked.MemoryBarrier();

            Console.WriteLine("--start measure for ExpirableLazy<T> optimized version");
            ExpirableLazy<object> expLazy3 = new ExpirableLazy<object>(() => "opt_value", TimeSpan.FromMilliseconds(300000));
            ignored = expLazy3.Value;
            ThreadStart threadBodyEx3 = () => {
                start.Wait();
                for (int i = 0; i < iteration; ++i)
                    ignored = expLazy3.Value;
            };

            for (int i = 0; i < THREADS; i++) {
                threads[i] = new Thread(threadBodyEx3);
                threads[i].Start();
            }
            sw.Restart();
            start.Set();
            for (int i = 0; i < THREADS; i++)
                threads[i].Join();
            sw.Stop();
            start.Reset();
            long elapsed3 = sw.ElapsedMilliseconds;
            Console.WriteLine("Ex3: elapsed: {0} ms counted, cost per loop: {1} ns",
                              elapsed3, (elapsed3 * 1000000L) / (iteration * THREADS));

            Console.WriteLine("--start measure for ExpirableLazy<T> normal version");
            ExpirableLazy1<object> expLazy1 = new ExpirableLazy1<object>(() => "normal_value", TimeSpan.FromMilliseconds(300000));
            ignored = expLazy1.Value;
            ThreadStart threadBodyEx1 = () => {
                for (int i = 0; i < iteration; ++i)
                    ignored = expLazy1.Value;
            };

            for (int i = 0; i < THREADS; i++) {
                threads[i] = new Thread(threadBodyEx1);
                threads[i].Start();
            }
            sw.Restart();
            start.Reset();
            for (int i = 0; i < THREADS; i++)
                threads[i].Join();
            sw.Stop();
            long elapsed1 = sw.ElapsedMilliseconds;
            Console.WriteLine("Ex1: elapsed: {0}, cost per loop: {1} ms", elapsed1, (elapsed1 * 1000000) / (iteration * THREADS));
        }
    }
}
