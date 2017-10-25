using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadSave;
using System.Threading;

namespace Test1 {
    [TestClass]
    public class TestExpirableLazy {
        private const int NTHREADS = 5;

        [TestMethod]
        public void DifferentValues() {
            Thread[] threads = new Thread[NTHREADS];
            object[] privateValues = new object[NTHREADS];
            int providedValue = 0;
            ExpirableLazy<object> expLazy = new ExpirableLazy<object>(() => providedValue++, TimeSpan.FromMilliseconds(500));
            for (int i = 0; i < NTHREADS; i++)
            {
                int li = i;
                threads[i] = new Thread(() =>
                {
                    privateValues[li] = expLazy.Value;
                });
                Thread.Sleep(1000);
                threads[i].Start();
            }

            for (int i = 0; i < NTHREADS; i++)
            {
                threads[i].Join();
                Assert.AreEqual(i, privateValues[i]);
            }
        }

        [TestMethod]
        public void SameValues() {
            Thread[] threads = new Thread[NTHREADS];
            object[] privateValues = new object[NTHREADS];
            int providedValue = 0;
            ExpirableLazy<object> expLazy = new ExpirableLazy<object>(() => providedValue++, TimeSpan.FromMilliseconds(500));
            for (int i = 0; i < NTHREADS; i++) {
                int li = i;
                threads[li] = new Thread(() => {
                    privateValues[li] = expLazy.Value;
                });
                threads[i].Start();
            }

            for (int i = 0; i < NTHREADS; i++) {
                threads[i].Join();
                Assert.AreEqual(privateValues[i], 0);
            }
        }

        [TestMethod]
        public void SomeEqualValues() {
            Thread[] threads = new Thread[NTHREADS];
            object[] privateValues = new object[NTHREADS];
            int providedValue = 0;
            ExpirableLazy<object> expLazy = new ExpirableLazy<object>(() => providedValue++, TimeSpan.FromMilliseconds(500));

            for (int i = 0; i < NTHREADS; i++) {
                int li = i;
                threads[i] = new Thread(() => {
                    privateValues[li] = expLazy.Value;
                });
                threads[i].Start();
                if (i == 1 || i == 2)
                    Thread.Sleep(1000);
            }

            for (int i = 0; i < NTHREADS; i++) {
                threads[i].Join();
            }

            Assert.AreEqual(privateValues[0], 0);
            Assert.AreEqual(privateValues[1], 0);
            Assert.AreEqual(privateValues[2], 1);
            Assert.AreEqual(privateValues[3], 2);
            Assert.AreEqual(privateValues[4], 2);
        }

        [TestMethod]
        public void InvalidOperation() {
            Thread[] threads = new Thread[NTHREADS];
            object[] privateValues = new object[NTHREADS];
            int providedValue = 0;
            ExpirableLazy<object> expLazy = new ExpirableLazy<object>(
                () => {
                    if (providedValue++ == 2) {
                        throw new InvalidOperationException();
                    }
                    return providedValue;
                }, TimeSpan.FromMilliseconds(500)
            );

            for (int i = 0; i < NTHREADS; i++) {
                int li = i;
                threads[i] = new Thread(() => {
                    try {
                        privateValues[li] = expLazy.Value;
                    }
                    catch (InvalidOperationException) {
                        privateValues[li] = 0;
                    }
                });
                threads[i].Start();
                Thread.Sleep(1000);
            }

            for (int i = 0; i < NTHREADS; i++) {
                threads[i].Join();
            }

            Assert.AreEqual(privateValues[0], 1);
            Assert.AreEqual(privateValues[1], 2);
            Assert.AreEqual(privateValues[2], 0);
            Assert.AreEqual(privateValues[3], 4);
            Assert.AreEqual(privateValues[4], 5);
        }
    }
}
