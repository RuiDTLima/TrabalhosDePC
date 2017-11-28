using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadSave;
using System.Threading;

namespace TesteThreadSave {
    [TestClass]
    public class TesteUnsafeRefCountedHolder {
        private readonly UnsafeRefCountedHolder<String> holder = new UnsafeRefCountedHolder<string>("Hello World");

        private void CreateThread(Thread[] addThreads, Thread[] releaseThreads) {
            int count = 3000;

            for (int i = 0; i < addThreads.Length; i++) {
                addThreads[i] = new Thread(() => {
                    for (int j = 0; j < count; j++) {
                        holder.AddRef();
                    }
                });
            }

            for (int i = 0; i < releaseThreads.Length - 1; i++) {
                releaseThreads[i] = new Thread(() => {
                    for (int j = 0; j < count; j++) {
                        holder.ReleaseRef();
                    }
                });
            }

            releaseThreads[10] = new Thread(() => {
                holder.ReleaseRef();
            });
        }

        private void ExecuteAddAndDeletesReferences(Thread[] addThreads, Thread[] releaseThreads) {
            for (int i = 0; i < addThreads.Length; i++) {
                addThreads[i].Start();
            }

            for (int i = 0; i < addThreads.Length; i++) {
                addThreads[i].Join();
            }

            for (int i = 0; i < releaseThreads.Length; i++) {
                releaseThreads[i].Start();
            }

            for (int i = 0; i < releaseThreads.Length; i++) {
                releaseThreads[i].Join();
            }
        }

        /**
         *  Caso se queira testar o correcto funcionamento do teste relativamente ao código
         *  not Thread-save do enunciado, o mesmo deve ser descomentado do ficheiro do código.
         *  Atenção o teste pode nem sempre falhar. 
         */
        [TestMethod]
        public void MultiTimesMultiThreadAdds() {
            Thread[] addThreads = new Thread[10];
            Thread[] releaseThreads = new Thread[11];

            CreateThread(addThreads, releaseThreads);
            ExecuteAddAndDeletesReferences(addThreads, releaseThreads);
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
            Thread[] addThreads = new Thread[300];
            Thread[] releaseThreads = new Thread[301];

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
                //Thread.Sleep(100);
            }

            for (int i = 0; i < releaseThreads.Length; i++) {
                releaseThreads[i].Start();
            }

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