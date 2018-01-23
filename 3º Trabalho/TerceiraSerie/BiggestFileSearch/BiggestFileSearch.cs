using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BiggestFileSearch {
    public class BiggestFilesSearch {
        private static object monitor = new object();   // Monitor sobre o qual são bloqueadas as execução concurrentes
        private static object reportMonitor = new object();
        private Tuple<string, long>[] biggestFiles;
        private long filesEncountered;

        public static void Main(string[] args) {
            if (args.Length != 2) {
                Console.WriteLine("To call the application provide the directory path followed by the number of files to present");
                return;
            }

            BiggestFilesSearch biggestFile = new BiggestFilesSearch();
            string directoryPath = args[0];
            int numberOfFileToPresent = int.Parse(args[1]);

            CancellationTokenSource cancelationTokenSource = new CancellationTokenSource();
            Progress<Tuple<Tuple<string, long>[], long>> progress = new Progress<Tuple<Tuple<string, long>[], long>>();
            Tuple<string[], long> result = biggestFile.GetBiggestFiles(directoryPath, numberOfFileToPresent, cancelationTokenSource.Token, progress);

            if (result == null) {
                Console.WriteLine("There was an error in find files");
                return;
            }
            string[] files = result.Item1;
            long numberOfFiles = result.Item2;

            Console.WriteLine("There was {0} that were found", numberOfFiles);
            foreach (string file in files) {
                Console.WriteLine(file);
            }
        }

        /**
         * Procura no directorio directoryPath e seus sub-directorios os maiores numberOfFileToPresent ficheiros, retornando um tuplo com os nomes dos ficheiros que satisfazem as condiçções e o número total 
         * de ficheiros encontrados. Esta operação é passivel de ser cancelada devendo para isso ser chamando o método cancel do CancellationToken passado em parametro, o parâmetro progress é usada na 
         * aplicação GUI para ir alterando os resultados apresentados (maiores numberOfFileToPresent ficheiros encontrados e o número total de ficheiros encontrados). É chamado o método auxiliar 
         * findBiggestFiles o qual procura os maiores numberOfFileToPresent ficheiros do directório e dos seus sub-diretório. Apesar de na primeira chamada ao método findBiggestFiles apenas ser passado um 
         * directório, que corresponde ao directório escolhido pelo utilizador, este directório é lhe passado num array para permitir usar o findBiggestFiles de forma recursiva. Depois de processados todos
         * os ficheiros de todos os directórios é preparado o tuplo para retornar, cotendo o número de ficheiros a apresentar e um array de nomes de ficheiros.
         */
        public Tuple<string[], long> GetBiggestFiles(string directoryPath, int numberOfFileToPresent, CancellationToken cancellationToken, IProgress<Tuple<Tuple<string, long>[], long>> progress) {
            biggestFiles = new Tuple<string, long>[numberOfFileToPresent];
            filesEncountered = 0;

            findBiggestFiles(new string[] { directoryPath }, numberOfFileToPresent, cancellationToken, progress);

            try {
                string[] filesReturned = new string[numberOfFileToPresent];

                for (int i = 0; i < numberOfFileToPresent; i++) {
                    filesReturned[i] = biggestFiles[i].Item1;
                }
                return new Tuple<string[], long>(filesReturned, filesEncountered);
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
            return null;
        }

        /**
         * Este método recebe um array de strings que correspondem ao path dos directorios sobre os quais será feita uma pesquisa pelos ficheiros que cumprem os requisitos em termos de tamanho. Sendo
         * que começa-se por fazer um ciclo paralelo para percorrer o array de directórios recebidos, sendo que para cada directório encontrado é chamado o método findBiggestFiles recursivamente para
         * pesquisar nos sub-directórios do actual, além disso é usado um ciclo paralelo para percorrer todos os ficheiros do directório actual na iteração. No fim de cada iteração do ciclo paralelo de
         * pesquisa do directório é feito um report de modo a na GUI poder ser apresentado o resultado parcial da pesquisa dos ficheiros, ou seja demonstra o efeito que cada iteração de pesquisa do 
         * directório teve no resultado final a apresentar. O primeiro ciclo paralelo tem dois parametros, primeiro o Iterable a percorrer, neste caso um string[], e o callback de acção, uma Action<T>,
         * a qual é executada uma vez para cada directório percorrida, sendo ai verificado se houve um pedido de cancelamento da pesquisa, se sim cancela o loop e é returnado. Caso contrário, é chamado 
         * o método actual de forma recursiva para realizar a mesma pesquisa sobre os subdirectórios do directório actual, depois é executado um outro ciclo paralelo mas desta vez para pesquisar todos 
         * os ficheiros do directório actual, quando isso acabar, ou seja antes de passar ao próximo elemento do Iterable, o próximo directório, é feito um report para manter actualizada a GUI sobre o 
         * efeito de cada iteração sobre o resultado final, este report é feito numa zona de exclusão mútua, para garantir que apenas é feito um report de cada vez. O segundo ciclo paralelo, de pesquisa
         * de ficheiros do directório actual, tem quatro parametros, o primeiro é o Iterable a percorrer, neste caso um FileInfo[], o segundo é um callback de iniciação, que é chamado apenas uma vez para
         * task a executar o ciclo, o terceiro paramêtro é o corpo da operação sendo este chamado em paralelo por cada task a executar o ciclo, aqui tem de se ter cuidado a trabalhar com váriaveis globais
         * pois este callback é executado em concorrência, nos parametros desse callback o partial é o array criado no callback de init, sendo nesse guardado os maiores ficheiros encontrados pela task em
         * questão. O último callback a executar é o callback final chamado uma vez por cada task a executar o ciclo, nesta como se trabalha com variáveis globais as suas operações são feitas numa zona 
         * de exclusão mútua, modificando o array de ficheiros retornado no fim.
         */
        private void findBiggestFiles(string[] directories, int numberOfFileToPresent, CancellationToken cancellationToken, IProgress<Tuple<Tuple<string, long>[], long>> progress) {
            List<FileInfo> files = new List<FileInfo>();

            Parallel.ForEach(directories, (directory, outLoopState) => {
                if (cancellationToken.IsCancellationRequested) {
                    outLoopState.Stop();
                    return;
                }

                findBiggestFiles(Directory.GetDirectories(directory), numberOfFileToPresent, cancellationToken, progress);

                Parallel.ForEach(new DirectoryInfo(directory).GetFiles(),
                    () => new FileInfo[numberOfFileToPresent],
                    (file, loopState, index, partial) => {
                        Interlocked.Increment(ref filesEncountered);
                        if (cancellationToken.IsCancellationRequested) {
                            loopState.Break();
                            return null;
                        }
                        if (partial[0] == null) {
                            partial[0] = file;
                            for (int i = 0; i < partial.Length - 1; i++) {
                                if (partial[i + 1] == null) {
                                    partial[i + 1] = partial[i];
                                    partial[i] = null;
                                }
                                else break;
                            }
                        }
                        else if (partial[0].Length < file.Length) {
                            partial[0] = file;
                        }

                        for (int i = 0; i < partial.Length - 1; i++) {
                            if (partial[i] == null)
                                continue;
                            if (partial[i].Length > partial[i + 1].Length) {
                                var temp = partial[i];
                                partial[i] = partial[i + 1];
                                partial[i + 1] = temp;
                            }
                        }

                        return partial;
                    }, (directoryFiles) => {
                        lock (monitor) {
                            if (cancellationToken.IsCancellationRequested) {
                                outLoopState.Stop();
                                return;
                            }
                            for (int i = directoryFiles.Length - 1; i >= 0 && directoryFiles[i] != null; i--) {
                                bool toInsert = false;
                                int index = 0;
                                for (int idx = 0; idx < biggestFiles.Length; idx++) {
                                    if (biggestFiles[idx] == null || biggestFiles[idx].Item2 < directoryFiles[i].Length) {
                                        index = idx;
                                        toInsert = true;
                                    }
                                    else break;
                                }
                                if (toInsert) {
                                    for (int j = 1; j <= index; j++) {
                                        biggestFiles[j - 1] = biggestFiles[j];
                                    }
                                    biggestFiles[index] = new Tuple<string, long>(directoryFiles[i].FullName, directoryFiles[i].Length);
                                }
                                else break; // o elemento não foi inserido, logo os seguintes tambem não serão uma vez que têm tamanho inferior
                            }
                        }
                    });
                lock (reportMonitor) {
                    progress.Report(new Tuple<Tuple<string, long>[], long>(biggestFiles, filesEncountered));
                }
            });
        }
    }
}