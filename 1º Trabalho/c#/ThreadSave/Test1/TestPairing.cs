using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadSave;
using System.Threading;

namespace Test1 {
    [TestClass]
    public class TestPairing {
        [TestMethod]
        public void TestWrite1TupleDifferentTypes() {
            Pairing<String, int> pairing = new Pairing<string, int>();
            Thread[] threads = new Thread[2];
            string sendMessage = "Hello World! it's ";
            int sendNumber = 2017;
            Tuple<string, int> resultString = null;    // irá receber o resultado da chamada ao provide que recebe uma String
            Tuple<string, int> resultInt = null;    // irá receber o resultado da chamada ao provide que recebe uma int
            threads[0] = new Thread(() => {
                resultString = pairing.Provide(sendMessage, 5000);
            });

            threads[1] = new Thread(() => {
                resultInt = pairing.Provide(sendNumber, 5000);
            });
            
            threads[0].Start();
            threads[1].Start();
            threads[0].Join();

            Assert.AreEqual(resultString.Item1, resultInt.Item1);
            Assert.AreEqual(resultString.Item2, resultInt.Item2);
            Assert.AreEqual(resultString.Item1, sendMessage);
            Assert.AreEqual(resultString.Item2, sendNumber);
        }

        /* [TestMethod]
         public void TestWrite1TupleSameTypes() {
             Pairing<string, string> pairing = new Pairing<string, string>();
             Thread[] threads = new Thread[2];
             string firstMessage = "Hello World!";
             string secondMessage = "It's 2017";
             Tuple<string, string> resultString = null;    // irá receber o resultado da chamada ao provide que recebe uma String
             Tuple<string, string> resultInt = null;    // irá receber o resultado da chamada ao provide que recebe uma int
             threads[0] = new Thread(() => {
                 resultString = pairing.Provide(firstMessage, 5000);
             });

             threads[1] = new Thread(() => {
                 resultInt = pairing.Provide(secondMessage, 5000);
             });

             threads[0].Start();
             threads[1].Start();
             threads[0].Join();

             Assert.AreEqual(resultString.Item1, resultInt.Item1);
             Assert.AreEqual(resultString.Item2, resultInt.Item2);
             Assert.AreEqual(resultString.Item1, sendMessage);
             Assert.AreEqual(resultString.Item2, sendNumber);
         }*/

        [TestMethod]
        public void TestTimeoutExceptionOnFirstThread() {
            Pairing<String, int> pairing = new Pairing<string, int>();
            Thread[] threads = new Thread[2];
            string sendMessage = "Hello World! it's ";
            int sendNumber = 2017;
            string exceptionMessage = null;
            Tuple<string, int> resultString = null;    // irá receber o resultado da chamada ao provide que recebe uma String
            Tuple<string, int> resultInt = null;    // irá receber o resultado da chamada ao provide que recebe uma int

            threads[0] = new Thread(() => {
                try {
                    resultString = pairing.Provide(sendMessage, 5000);
                }catch (TimeoutException) {
                    exceptionMessage = "Exception occured";
                }
            });

            threads[1] = new Thread(() => {
                resultInt = pairing.Provide(sendNumber, 5000);
            });

            threads[0].Start();
            Thread.Sleep(6000);
            threads[1].Start();
            threads[0].Join();
            Assert.AreEqual("Exception occured", exceptionMessage);
            Assert.AreEqual(resultString, null);
            Assert.AreEqual(resultInt, null);
        }

        [TestMethod]
        public void TestTimeoutExceptionOnSecondThread() {
            Pairing<String, int> pairing = new Pairing<string, int>();
            Thread[] threads = new Thread[2];
            string sendMessage = "Hello World! it's ";
            int sendNumber = 2017;
            string exceptionMessage = null;
            Tuple<string, int> resultString = null;    // irá receber o resultado da chamada ao provide que recebe uma String
            Tuple<string, int> resultInt = null;    // irá receber o resultado da chamada ao provide que recebe uma int

            threads[0] = new Thread(() => {
                resultString = pairing.Provide(sendMessage, 5000);
            });

            threads[1] = new Thread(() => {
                try {
                    resultInt = pairing.Provide(sendNumber, 5000);
                }catch (TimeoutException) {
                    exceptionMessage = "Exception occured";
                }
            });

            threads[1].Start();
            Thread.Sleep(6000);
            threads[0].Start();
            threads[1].Join();
            Assert.AreEqual("Exception occured", exceptionMessage);
            Assert.AreEqual(resultString, null);
            Assert.AreEqual(resultInt, null);
        }

        [TestMethod]
        public void TestMultipleTuples() {
            Pairing<String, int> pairing = new Pairing<string, int>();
            Thread[] threads = new Thread[10];
            string[] messages = { "Hello World!", "This is threads", "This is PC", "ISEL", "Windows" };
            int[] numbers = { 1, 2, 3, 4, 5 };
            Tuple<string, int>[] resultString = new Tuple<string, int>[5];    // irá receber o resultado da chamada ao provide que recebe uma String
            Tuple<string, int>[] resultInt = new Tuple<string, int>[5];    // irá receber o resultado da chamada ao provide que recebe uma int

            for (int i = 0; i < messages.Length; i++) {
                int li = i;
                threads[li] = new Thread(() => {
                    resultString[li] = pairing.Provide(messages[li], 50000);
                });
                threads[li].Start();
                Thread.Sleep(100);
            }

            for (int i = 0; i < numbers.Length; i++) {
                int li = i;
                threads[li + 5] = new Thread(() => {
                    resultInt[li] = pairing.Provide(numbers[li], 50000);
                });
                threads[li + 5].Start();
                Thread.Sleep(100);
                //threads[li + 5].Join();
            }

            for (int i = 0; i < resultString.Length; i++) {
                Assert.AreEqual(messages[i], resultString[i].Item1);
            }

            for (int i = 0; i < resultInt.Length; i++) {
                Assert.AreEqual(numbers[i], resultInt[i].Item2);
            }

            for (int i = 0; i < resultString.Length; i++) {
                Assert.AreEqual(resultInt[i].Item1, resultString[i].Item1);
                Assert.AreEqual(resultInt[i].Item2, resultString[i].Item2);
            }
        }
    }
}
