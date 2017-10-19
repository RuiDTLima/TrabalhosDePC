using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using ThreadSave;

namespace Test1 {
    [TestClass]
    public class TestTransferQueue {
        [TestMethod]
        /**
         * Testa se a ligação put->take funciona como esperado. Se o take recebe o valor introduzido pelo put.
         */
        public void TestPutTake() {
            Thread[] threads = new Thread[2];

            TransferQueue<String> transQueue = new TransferQueue<string>();
            String receivedMessage = "";
            String sendMessage = "Hello World of Threads";
            bool sucess = false;
            threads[0] = new Thread(()=> {
                sucess = transQueue.Take(500, out receivedMessage);
            });

            threads[1] = new Thread(() => {
                transQueue.Put(sendMessage);
            });

            threads[0].Start();
            threads[1].Start();
            threads[0].Join();
            Assert.IsTrue(sucess);
            Assert.AreEqual(sendMessage, receivedMessage);
        }

        [TestMethod]
        /**
         * Testa se a ligação transfer->take funciona como esperado. Se o take recebe o valor introduzido pelo transfer.
         */
        public void TestTransferTake() {
            Thread[] threads = new Thread[2];

            TransferQueue<String> transQueue = new TransferQueue<string>();
            String receivedMessage = "";
            String sendMessage = "Hello World of Threads";
            bool sucess = false;
            threads[0] = new Thread(() => {
                sucess = transQueue.Take(500, out receivedMessage);
            });

            threads[1] = new Thread(() => {
                transQueue.Transfer(sendMessage, 500);
            });

            threads[0].Start();
            threads[1].Start();
            threads[0].Join();
            Assert.IsTrue(sucess);
            Assert.AreEqual(sendMessage, receivedMessage);
        }

        [TestMethod]
        /**
         * Testa se o método Transfer espera realmente o timeout passado que a mensagem seja lida.
         */
        public void TestUnsuccessfullTransferTake() {
            Thread[] threads = new Thread[2];

            TransferQueue<String> transQueue = new TransferQueue<string>();
            String receivedMessage = "";
            String sendMessage = "Hello World of Threads";
            bool sucess = false;
            threads[0] = new Thread(() => {
                sucess = transQueue.Take(1000, out receivedMessage);
            });

            threads[1] = new Thread(() => {
                transQueue.Transfer(sendMessage, 500);
            });

            threads[1].Start();
            Thread.Sleep(500);
            threads[0].Start();
            threads[0].Join();
            Assert.IsFalse(sucess);
            Assert.AreNotEqual(sendMessage, receivedMessage);
        }

        [TestMethod]
        /**
         * Testa se o método Take espera realmente o timeout passado que uma mensagem seja escrita.
         */
        public void TestUnsuccessfullPutTake() {
            Thread[] threads = new Thread[2];

            TransferQueue<String> transQueue = new TransferQueue<string>();
            String receivedMessage = "";
            String sendMessage = "Hello World of Threads";
            bool sucess = false;
            threads[0] = new Thread(() => {
                sucess = transQueue.Take(1000, out receivedMessage);
            });

            threads[1] = new Thread(() => {
                transQueue.Put(sendMessage);
            });

            threads[0].Start();
            Thread.Sleep(1000);
            threads[1].Start();
            threads[0].Join();
            Assert.IsFalse(sucess);
            Assert.AreNotEqual(sendMessage, receivedMessage);
        }

        [TestMethod]
        /**
         * Testa se o existe um correcto funcionameto do método Take no caso de ThreadInterruptedException
         */
        public void TestThreadInterruptedExceptionInTake() {
            Thread[] threads = new Thread[2];

            TransferQueue<String> transQueue = new TransferQueue<string>();
            String receivedMessage = "";
            String sendMessage = "Hello World of Threads";
            String exceptionMessage = "Exception Occured";
            bool sucess = true;
            threads[0] = new Thread(() => {
                try {
                    sucess = transQueue.Take(1000, out receivedMessage);
                }
                catch (ThreadInterruptedException) {
                    receivedMessage = exceptionMessage;
                }
            });

            threads[1] = new Thread(() => {
                transQueue.Put(sendMessage);
            });

            threads[0].Start();
            Thread.Sleep(500);
            threads[1].Start();
            threads[0].Interrupt();
            threads[0].Join();
            Assert.AreEqual(exceptionMessage, receivedMessage);
        }

        [TestMethod]
        /**
         * Testa se o existe um correcto funcionameto do método Take no caso de ThreadInterruptedException
         */
        public void TestThreadInterruptedExceptionInTransfer() {
            Thread[] threads = new Thread[2];

            TransferQueue<String> transQueue = new TransferQueue<string>();
            String receivedMessage = "";
            String sendMessage = "Hello World of Threads";
            String exceptionMessage = "Exception Occured";
            bool sucess = false;
            threads[0] = new Thread(() => {
                try {
                    sucess = transQueue.Take(1000, out receivedMessage);
                }
                catch (ThreadInterruptedException) {
                    Assert.IsTrue(true);
                }
            });

            threads[1] = new Thread(() => {
                try {
                    transQueue.Transfer(sendMessage, 500);
                }
                catch (ThreadInterruptedException) {
                    receivedMessage = exceptionMessage;
                }
            });

            threads[1].Start();
            threads[0].Start();
            threads[1].Interrupt();
            threads[1].Join();
            Assert.AreEqual(exceptionMessage, receivedMessage);
        }
    }
}
