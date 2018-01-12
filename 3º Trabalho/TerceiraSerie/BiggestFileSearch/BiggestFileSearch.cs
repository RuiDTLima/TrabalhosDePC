using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BiggestFileSearch {
    public class BiggestFileSearch {
        private static object monitor = new object();   // Monitor sobre o qual são bloqueadas as execução concurrentes

        public static void Main(string[] args) {
            if(args.Length != 2) {
                Console.WriteLine("To call the application provide the directory path followed by the number of files to present");
                return;
            }

            string directoryPath = args[0];
            int numberOfFileToPresent = int.Parse(args[1]);

            CancellationTokenSource cancelationTokenSource = new CancellationTokenSource();
            Progress<Tuple<Tuple<string, long>[], long>> progress = new Progress<Tuple<Tuple<string, long>[], long>>();
            Tuple<string[], long> result = GetBiggestFiles(directoryPath, numberOfFileToPresent, cancelationTokenSource.Token, progress);

            if (result == null) {
                Console.WriteLine("There was an error in find files");
                return;
            }
            string[] files = result.Item1;
            long numberOfFiles = result.Item2;

            Console.WriteLine("There was {0} that were found", numberOfFiles);
            foreach(string file in files) {
                Console.WriteLine(file);
            }
        }

        /**
         * Procura no directorio directoryPath e seus sub-directorios o maiores numberOfFileToPresent ficheiros, retornando um tuplo com os nomes dos ficheiros que satisfazem as condiçções e o número total 
         * de ficheiros encontrados. Esta operação é passivel de ser cancelada devendo para isso ser chamando o método cancel do CancellationToken passado em parametro, o parâmetro progress é usada na 
         * aplicação GUI para ir alterando os resultados apresentados (maiores numberOfFileToPresent ficheiros encontrados e o número total de ficheiros encontrados).
         * O método fica num ciclo enquando houver sub-directórios para pesquisar e enquanto não tiver sido pedido o cancelamento da operação. Insere-se num contentor todos os directórios encontrados, sendo
         * removidos desse contentar o directorio cujos ficheiros estão a ser processados. Depois faz-se um ciclo em paralelo sobre os ficheiros que estão nesse directório, este ciclo é em paralelo, porque a 
         * funcionalidade pode ser paralelizada, pois pretende-se encontrar os maiores ficheiros do directório.
         * O ciclo paralelo, tem quatro parametros, o primeiro é o Iterable a percorrer, neste caso um FileInfo[], o segundo é um callback de iniciação, que é chamado apenas uma vez para task a executar 
         * o ciclo, o terceiro paramêtro é o corpo da operação sendo este chamado em paralelo por cada task a executar o ciclo, aqui tem de se ter cuidado a trabalhar com váriaveis globais pois este
         * callback é executado em concorrência, nos parametros desse callback o partial é o array criado no callback de init, sendo nesse guardado os maiores ficheiros encontrados pela task em questão
         * O último callback a executar é o callback final chamado uma vez por cada task a executar o ciclo, nesta como se trabalha com variáveis globais as suas operações são feitas numa zona de 
         * exclusão mútua, modificando o array de ficheiros retornado no fim. Ao terminar a chamada ao ciclo em paralelo, ou seja depois de avaliados todos os ficheiros do directório
         * actual é feito um Report do progress para poder ser alterar a vista como os resultados parciais. Depois de processados todos os ficheiros de todos os directórios é preparado o tuplo de retorna 
         * para retornar.
         */
        public static Tuple<string[], long> GetBiggestFiles(string directoryPath, int numberOfFileToPresent, CancellationToken cancellationToken, IProgress<Tuple<Tuple<string, long>[], long>> progress) {
            Tuple<string, long>[] biggestFiles = new Tuple<string, long>[numberOfFileToPresent];
            long filesEncountered = 0;

            List<FileInfo> files = new List<FileInfo>();

            Queue directories = new Queue();
            if (!Directory.Exists(directoryPath))
                return null;

            directories.Enqueue(directoryPath);
            
            while(directories.Count > 0 && !cancellationToken.IsCancellationRequested) {
                string currentDirectory = directories.Dequeue().ToString();
                string[] subDirectories = { };

                DirectoryInfo directoryInfo = new DirectoryInfo(currentDirectory);

                Parallel.ForEach(directoryInfo.GetFiles(),
                    () => new FileInfo[numberOfFileToPresent],
                    (file, loopState, index, partial) => {
                        Interlocked.Increment(ref filesEncountered);
                        if (cancellationToken.IsCancellationRequested) {
                            loopState.Stop();
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
                            bool toInsert = false;
                            for (int i = directoryFiles.Length - 1; i >= 0 && directoryFiles[i] != null; i--) {
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

                progress.Report(new Tuple<Tuple<string, long>[], long>(biggestFiles, filesEncountered));

                try {
                    subDirectories = Directory.GetDirectories(currentDirectory);
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    continue;
                }

                foreach (string subDirectory in subDirectories) {
                    directories.Enqueue(subDirectory);
                }
            }

            try {
                string[] filesReturned = new string[numberOfFileToPresent];

                for (int i = 0; i < numberOfFileToPresent; i++) {
                    filesReturned[i] = biggestFiles[i].Item1;
                }
                return new Tuple<string[], long>(filesReturned, filesEncountered);

            } catch(Exception e) {
                Console.WriteLine(e.Message);
            }
            return null;
        }
    }
}