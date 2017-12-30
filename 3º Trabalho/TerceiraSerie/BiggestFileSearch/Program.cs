using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BiggestFileSearch {
    class Program {
        private static int smallestSizeIndex = 0;
        private static long smallestSize = 0;
        private static object monitor = new object();

        static void Main(string[] args) {
            if(args.Length != 2) {
                Console.WriteLine("To call the application provide the directory path followed by the number of files to present");
                return;
            }

            string directoryPath = args[0];
            int numberOfFileToPresent = int.Parse(args[1]);

            Tuple<string[], long> result = GetBiggestFiles(directoryPath, numberOfFileToPresent);

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
            int processoresCount = System.Environment.ProcessorCount;
            //List<FileInfo> fileInfo = new List<FileInfo>();

            Queue directories = new Queue();
            if (!Directory.Exists(directoryPath))
                return null;

            directories.Enqueue(directoryPath);

            while(directories.Count > 0) {
                string currentDirectory = directories.Dequeue().ToString();
                string[] subDirectories = { };

                try {
                    subDirectories = Directory.GetDirectories(currentDirectory);
                } catch(Exception e) {
                    Console.WriteLine(e.Message);
                    continue;
                }

                foreach (string subDirectory in subDirectories)
                    directories.Enqueue(subDirectory);

                try {
                    DirectoryInfo directoryInfo = new DirectoryInfo(currentDirectory);
                    FileInfo[] fileInfo = directoryInfo.GetFiles();

                    /*if (fileInfo.Length < processoresCount) {
                        foreach (FileInfo file in fileInfo) {
                            filesEncountered++;
                            if (file.Length > smallestSize) {
                                biggestFiles[smallestSizeIndex] = new Tuple<string, long>(file.FullName, file.Length);
                                if (filesEncountered >= biggestFiles.Length) {

                                    while(Interlocked.CompareExchange(ref smallestSize, file.Length, smallestSize) != smallestSize);

                                    //smallestSize = file.Length;
                                    long iterationSmallestSize = smallestSize;
                                    for (int i = 0; i < biggestFiles.Length; i++) {
                                        if(biggestFiles[i].Item2 < iterationSmallestSize) {
                                            iterationSmallestSize = biggestFiles[i].Item2;
                                            smallestSizeIndex = i;
                                        }
                                    }

                                } else {
                                    smallestSizeIndex = (smallestSizeIndex + 1) % numberOfFileToPresent;
                                }
                            }
                        }
                    } else {*/
                    Parallel.ForEach<FileInfo, FileInfo>(fileInfo, () => {
                        return null;
                    }, (file, loopState, index, partial) => {
                        filesEncountered++;
                        return (file.Length > smallestSize) ? file : null;
                    }, (file) => {
                        if (file == null)
                            return;
                        lock (monitor) {
                            Console.WriteLine(file.FullName);
                            if (file.Length > smallestSize) {
                                biggestFiles[smallestSizeIndex] = new Tuple<string, long>(file.FullName, file.Length);
                                if (filesEncountered >= biggestFiles.Length) {
                                    for (int i = 0; i < biggestFiles.Length; i++) {
                                        if (biggestFiles[i] == null) {
                                            smallestSizeIndex = i;
                                            break;
                                        }
                                        if (biggestFiles[i].Item2 < smallestSize) {
                                            smallestSize = biggestFiles[i].Item2;
                                            smallestSizeIndex = i;
                                        }
                                    }

                                }
                                else {
                                    smallestSizeIndex = (smallestSizeIndex + 1) % numberOfFileToPresent;
                                }
                            }
                        }
                    }); 
                        /*Parallel.ForEach(fileInfo, (file, loopState) => {
                            if (loopState.IsStopped)
                                return;

                            if (file.Length > smallestSize) {
                                lock (monitor) {
                                    Interlocked.Increment(ref filesEncountered);
                                    biggestFiles[smallestSizeIndex] = new Tuple<string, long>(file.FullName, file.Length);
                                    smallestSizeIndex = (smallestSizeIndex + 1) % numberOfFileToPresent;
                                    if (filesEncountered >= biggestFiles.Length) {
                                        long iterationSmallestSize = file.Length;
                                        int iterationSmallestSizeIndex = 0;
                                        for (int i = 0; i < biggestFiles.Length; i++) {
                                            if (biggestFiles[i].Item2 < smallestSize) {
                                                iterationSmallestSize = biggestFiles[i].Item2;
                                                iterationSmallestSizeIndex = i;
                                            }
                                        }
                                        smallestSize = iterationSmallestSize;
                                        smallestSizeIndex = iterationSmallestSizeIndex;
                                    }
                                }
                                
                            }
                        });*/
                    //}
                } catch(Exception e) {
                    Console.WriteLine(e.Message);
                    continue;
                }
            }
            string[] filesReturned = new string[numberOfFileToPresent];

            for (int i = 0; i < numberOfFileToPresent; i++) {
                filesReturned[i] = biggestFiles[i].Item1;
            }
            return new Tuple<string[], long>(filesReturned, filesEncountered);
        }
    }
}