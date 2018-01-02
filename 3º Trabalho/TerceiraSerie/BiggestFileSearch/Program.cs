using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BiggestFileSearch {
    class Program {
        private static object monitor = new object();

        static void Main(string[] args) {
            if(args.Length != 2) {
                Console.WriteLine("To call the application provide the directory path followed by the number of files to present");
                return;
            }

            string directoryPath = args[0];
            int numberOfFileToPresent = int.Parse(args[1]);

            CancellationTokenSource cancelationTokenSource = new CancellationTokenSource();
            Tuple<string[], long> result = GetBiggestFiles(directoryPath, numberOfFileToPresent, cancelationTokenSource.Token);

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

        private static Tuple<string[], long> GetBiggestFiles(string directoryPath, int numberOfFileToPresent, CancellationToken cancellationToken) {
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