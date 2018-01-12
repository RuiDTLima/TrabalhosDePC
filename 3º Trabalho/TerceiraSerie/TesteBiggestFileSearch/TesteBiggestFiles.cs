using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace TesteBiggestFileSearch {
    [TestClass]
    public class TesteBiggestFiles {
        private readonly string directoryPath = "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory";

        [TestMethod]
        /**
         *  Teste se o método GetBiggestFile encontra os 3 maiores ficheiros no directorio fornecido juntamente com o código
         */
        public void TesteGetTopThreeFiles() {
            int filesExpected = 17;
            string[] biggestFiles = { "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory\\SubDir1\\SubSub1\\carochinhaEight.txt", "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory\\SubDir1\\SubSub1\\SubSubSub1\\carochinhaNine.txt", "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory\\SubDir1\\SubSub2\\carochinhaTen.txt" };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Progress<Tuple<Tuple<string, long>[], long>> progress = new Progress<Tuple<Tuple<string, long>[], long>>();

            Tuple<string[], long> result = BiggestFileSearch.BiggestFileSearch.GetBiggestFiles(directoryPath, 3, cancellationTokenSource.Token, progress);

            Assert.AreEqual(filesExpected, result.Item2);

            int idx = 0;
            foreach(string file in result.Item1) {
                Assert.AreEqual(biggestFiles[idx++], file);
            }
        }

        [TestMethod]
        /**
         *  Teste se o método GetBiggestFile lida bem com o pedido de cancelamento da pesquisa, verificando para isso se o 
         *  resultado retornado não é um válido
         */
        public void TesteCancelationToken() {
            int filesExpected = 17;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Progress<Tuple<Tuple<string, long>[], long>> progress = new Progress<Tuple<Tuple<string, long>[], long>>();
            Tuple<string[], long> result = null;

            Thread tryFind = new Thread(() => {
                result = BiggestFileSearch.BiggestFileSearch.GetBiggestFiles(directoryPath, 3, cancellationTokenSource.Token, progress);
            });

            Thread cancel = new Thread(() => {
                cancellationTokenSource.Cancel();
            });

            tryFind.Start();
            cancel.Start();
            Thread.Sleep(10);
            Assert.IsTrue(cancellationTokenSource.IsCancellationRequested);
            if (result == null)
                Assert.IsTrue(true);
            else
                Assert.AreNotEqual(filesExpected, result.Item2);
        }

        [TestMethod]
        /**
         *  Teste se o método GetBiggestFile consegue encontrar os 3 maiores ficheiros do directorio fornecido juntamente com o código, 
         *  sendo esta chamada executada concurrentemente por várias threads
         */
        public void TesteMultipleGetTopThreeFiles() {
            int numberOfThreads = 5;
            int filesExpected = 17;
            string[] biggestFiles = { "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory\\SubDir1\\SubSub1\\carochinhaEight.txt", "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory\\SubDir1\\SubSub1\\SubSubSub1\\carochinhaNine.txt", "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory\\SubDir1\\SubSub2\\carochinhaTen.txt" };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Progress<Tuple<Tuple<string, long>[], long>> progress = new Progress<Tuple<Tuple<string, long>[], long>>();
            Thread[] threads = new Thread[numberOfThreads];
            Tuple<string[], long>[] results = new Tuple<string[], long>[numberOfThreads];

            for (int i = 0; i < threads.Length; i++) {
                int li = i;
                threads[i] = new Thread(() => {
                    results[li] = BiggestFileSearch.BiggestFileSearch.GetBiggestFiles(directoryPath, 3, cancellationTokenSource.Token, progress);
                });
            }

            foreach(Thread thread in threads) {
                thread.Start();
            }

            for (int i = 0; i < threads.Length; i++) {
                threads[i].Join();
                Assert.AreEqual(filesExpected, results[i].Item2);

                int idx = 0;
                foreach (string file in results[i].Item1) {
                    Assert.AreEqual(biggestFiles[idx++], file);
                }
            }
        }

        [TestMethod]
        /**
         *  Teste se o método GetBiggestFile lida bem com o cancelamento da pesquisa apenas para o cancellationTokenSource passado como parametro
         *  para isso faz-se duas chamadas concurrentes ao método GetBiggestFile sendo uma dessas chamadas canceladas depois de iniciada. Verifica-se
         *  se a operação não cancelada correu até ao fim com o resultado esperado e se a operação cancelada com vem com os valores correctos.
         */
        public void TesteMultipleThreadsOneCancel() {
            int filesExpected = 17;
            string[] biggestFiles = { "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory\\SubDir1\\SubSub1\\carochinhaEight.txt", "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory\\SubDir1\\SubSub1\\SubSubSub1\\carochinhaNine.txt", "D:\\rui_l\\Documentos\\Universidade\\3ºAno\\1º Semestre\\Programação Concorrente\\Trabalhos\\3º Trabalho\\testeDirectory\\SubDir1\\SubSub2\\carochinhaTen.txt" };

            CancellationTokenSource unusedcancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource usedcancellationTokenSource = new CancellationTokenSource();
            Progress<Tuple<Tuple<string, long>[], long>> progress = new Progress<Tuple<Tuple<string, long>[], long>>();
            Tuple<string[], long> notCancelResult = null;
            Tuple<string[], long> cancelResult = null;

            Thread threadNoCancel = new Thread(() => {
                notCancelResult = BiggestFileSearch.BiggestFileSearch.GetBiggestFiles(directoryPath, 3, unusedcancellationTokenSource.Token, progress);
            });

            Thread threadCancel = new Thread(() => {
                cancelResult = BiggestFileSearch.BiggestFileSearch.GetBiggestFiles(directoryPath, 3, usedcancellationTokenSource.Token, progress);
            });

            Thread cancel = new Thread(() => {
                usedcancellationTokenSource.Cancel();
            });

            threadNoCancel.Start();
            threadCancel.Start();
            cancel.Start();
            Thread.Sleep(10);

            Assert.IsTrue(usedcancellationTokenSource.IsCancellationRequested);
            Assert.IsFalse(unusedcancellationTokenSource.IsCancellationRequested);
            if (cancelResult == null)
                Assert.IsTrue(true);
            else
                Assert.AreNotEqual(filesExpected, cancelResult.Item2);

            Assert.AreEqual(filesExpected, notCancelResult.Item2);

            int idx = 0;
            foreach (string file in notCancelResult.Item1) {
                Assert.AreEqual(biggestFiles[idx++], file);
            }
        }
    }
}