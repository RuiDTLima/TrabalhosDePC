using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadSave;
using System.Threading;

namespace TesteThreadSave {
    [TestClass]
    public class TesteUnsafeRefCountedHolder {
        private readonly UnsafeRefCountedHolder<String> holder = new UnsafeRefCountedHolder<string>("Hello World");

        private void CreateThread(Thread[] addThreads, Thread[] releaseThreads) {
            for (int i = 0; i < addThreads.Length; i++) {
                addThreads[i] = new Thread(() => {
                    holder.AddRef();
                });
            }

            for (int i = 0; i < releaseThreads.Length; i++) {
                releaseThreads[i] = new Thread(() => {
                    holder.ReleaseRef();
                });
            }
        }

        private void ExecuteAddAndDeletesReferences(Thread[] addThreads, Thread[] releaseThreads) {
            for (int i = 0; i < addThreads.Length; i++) {
                addThreads[i].Start();
                //Thread.Sleep(100);
                releaseThreads[i].Start();
            }

            releaseThreads[10].Start();

            for (int i = 0; i < addThreads.Length; i++) {
                addThreads[i].Join();
                releaseThreads[i].Join();
            }

            releaseThreads[10].Join();
        }

        [TestMethod]
        public void MultiTimesMultiThreadAdds() {
            Thread[] addThreads = new Thread[10];
            Thread[] releaseThreads = new Thread[11];
            int count = 30;

            for (int i = 0; i < count; i++) {
                CreateThread(addThreads, releaseThreads);
                ExecuteAddAndDeletesReferences(addThreads, releaseThreads);
            }

            String result = null;
            try {
                result = holder.Value;
            }
            catch (InvalidOperationException) {
                Assert.IsTrue(true);
            }

            Assert.AreNotEqual("Hello World", result);
        }

        [TestMethod]
        public void MultiThreadAdds() {
            Thread[] addThreads = new Thread[10];
            Thread[] releaseThreads = new Thread[11];

            for (int i = 0; i < addThreads.Length; i++) {
                addThreads[i] = new Thread(() => {
                    holder.AddRef();
                });
            }

            for (int i = 0; i < releaseThreads.Length; i++) {
                releaseThreads[i] = new Thread(() => {
                    holder.ReleaseRef();
                });
            }

            for (int i = 0; i < addThreads.Length; i++) {
                addThreads[i].Start();
                Thread.Sleep(100);
                releaseThreads[i].Start();
            }

            releaseThreads[10].Start();

            for (int i = 0; i < addThreads.Length; i++) {
                addThreads[i].Join();
                releaseThreads[i].Join();
            }

            releaseThreads[10].Join();

            String result = null;
            try {
                result = holder.Value;
            }
            catch (InvalidOperationException) {
                Assert.IsTrue(true);
            }

            Assert.AreNotEqual("Hello World", result);
        }
    }
}
