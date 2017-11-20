using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadSave;
using System.Threading;

namespace TesteThreadSave {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestOneElementPutAndTryTake() {
            ConcurrentQueue<String> queue = new ConcurrentQueue<string>();
            String messageSend = "Hello World";

            Assert.IsTrue(queue.IsEmpty());
            queue.Put(messageSend);

            Assert.IsFalse(queue.IsEmpty());
            String result = queue.TryTake();
            Assert.IsTrue(queue.IsEmpty());

            Assert.AreEqual(messageSend, result);
        }

        [TestMethod]
        public void TestTwoElementPutAndTryTake() {
            ConcurrentQueue<String> queue = new ConcurrentQueue<string>();
            String firstMessageSend = "Hello World";
            String secondMessageSend = "ISEL";

            Assert.IsTrue(queue.IsEmpty());
            queue.Put(firstMessageSend);

            Assert.IsFalse(queue.IsEmpty());
            queue.Put(secondMessageSend);

            String firstResult = queue.TryTake();
            Assert.IsFalse(queue.IsEmpty());

            String secondResult = queue.TryTake();
            Assert.IsTrue(queue.IsEmpty());

            Assert.AreEqual(firstMessageSend, firstResult);
            Assert.AreEqual(secondMessageSend, secondResult);
        }

        [TestMethod]
        public void TestMultiplePutAndTryTake() {
            ConcurrentQueue<String> queue = new ConcurrentQueue<string>();
            String[] messages = { "Hello World", "Hello ISEL", "This is Threads", "This is PC", "Windows" };
            String[] results = new String[messages.Length];
            Assert.IsTrue(queue.IsEmpty());

            for (int i = 0; i < messages.Length; i++) {
                queue.Put(messages[i]);
            }

            Assert.IsFalse(queue.IsEmpty());

            for (int i = 0; i < messages.Length; i++) {
                results[i] = queue.TryTake();
            }

            Assert.IsTrue(queue.IsEmpty());

            for (int i = 0; i < messages.Length; i++) {
                Assert.AreEqual(messages[i], results[i]);
            }
        }

        [TestMethod]
        public void TestTryTakeNullReturn() {
            ConcurrentQueue<String> queue = new ConcurrentQueue<string>();

            Assert.IsNull(queue.TryTake());
        }

        [TestMethod]
        public void TestMultipleThreadsPutAndTryTake() {
            ConcurrentQueue<String> queue = new ConcurrentQueue<string>();
            
            Assert.IsTrue(queue.IsEmpty());

            String[] messages = { "Hello World", "Hello ISEL", "This is Threads", "This is PC", "Windows" };
            String[] results = new String[messages.Length];
            Thread[] threads = new Thread[messages.Length];

            for (int i = 0; i < threads.Length; i++) {
                int j = i;
                threads[j] = new Thread(() => queue.Put(messages[j]));
                threads[j].Start();
                Thread.Sleep(10);
            }

            for (int i = 0; i < threads.Length; i++) {
                threads[i].Join();
            }

            Assert.IsFalse(queue.IsEmpty());

            for (int i = 0; i < threads.Length; i++) {
                int j = i;
                threads[j] = new Thread(() => results[j] = queue.TryTake());
                threads[j].Start();
                Thread.Sleep(10);
            }

            for (int i = 0; i < threads.Length; i++) {
                threads[i].Join();
                Assert.AreEqual(messages[i], results[i]);
            }

            Assert.IsTrue(queue.IsEmpty());
        }
    }
}
