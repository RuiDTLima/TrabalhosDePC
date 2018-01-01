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

            Tuple<string[], long> result = GetBiggestFiles(directoryPath, numberOfFileToPresent);

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

        private static Tuple<string[], long> GetBiggestFiles(string directoryPath, int numberOfFileToPresent) {
            Tuple<string, long>[] biggestFiles = new Tuple<string, long>[numberOfFileToPresent];
            long filesEncountered = 0;
            List<FileInfo> files = new List<FileInfo>();

            Queue directories = new Queue();
            if (!Directory.Exists(directoryPath))
                return null;

            directories.Enqueue(directoryPath);

            while(directories.Count > 0) {
                string currentDirectory = directories.Dequeue().ToString();
                string[] subDirectories = { };

                try {
                    subDirectories = Directory.GetDirectories(currentDirectory);
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    continue;
                }

                foreach (string subDirectory in subDirectories)
                    directories.Enqueue(subDirectory);

                DirectoryInfo directoryInfo = new DirectoryInfo(currentDirectory);
                files.AddRange(directoryInfo.GetFiles());
            }

            try {
                foreach(string directory in directories) {
                    DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                    files.AddRange(directoryInfo.GetFiles());
                }

                Parallel.ForEach<FileInfo, FileInfo[]>(files,
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
                        } else if(partial[0].Length < file.Length) {
                            partial[0] = file;
                        }

                        for (int i = partial.Length - 1; i > 0; i--) {
                            if (partial[i - 1] == null)
                                break;
                            if(partial[i].Length < partial[i - 1].Length) {
                                var temp = partial[i];
                                partial[i] = partial[i - 1];
                                partial[i - 1] = temp;
                            }
                        }
                            
                        return partial;
                    }, (directoryFiles) => {
                        if (files == null)
                            return;
                        lock (monitor) {
                            int idx = directoryFiles.Length - 1;
                            for (int i = biggestFiles.Length - 1; i >= 0; i--) {
                                if (directoryFiles[idx] == null)
                                    break;
                                if(biggestFiles[i] == null || directoryFiles[idx].Length > biggestFiles[i].Item2) {
                                    biggestFiles[i] = new Tuple<string, long>(directoryFiles[idx].FullName, directoryFiles[idx].Length);
                                    idx--;
                                }
                            }
                        }
                });

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