using System;
using System.Linq;
using System.Threading;
using ThreadSave;

namespace Teste {
    class Program {
        public static bool TestMichaelScottQueue() {

            int CONSUMER_THREADS = 2;
            int PRODUCER_THREADS = 1;
            int MAX_PRODUCE_INTERVAL = 100;
            int MAX_CONSUME_TIME = 25;
            int FAILURE_PERCENT = 5;
            int JOIN_TIMEOUT = 100;
            int RUN_TIME = 5 * 1000;
            int POLL_INTERVAL = 20;


            Thread[] consumers = new Thread[CONSUMER_THREADS];
            Thread[] producers = new Thread[PRODUCER_THREADS];
            ConcurrentQueue<String> msqueue = new ConcurrentQueue<String>();
            int[] productions = new int[PRODUCER_THREADS];
            int[] consumptions = new int[CONSUMER_THREADS];
            int[] failuresInjected = new int[PRODUCER_THREADS];
            int[] failuresDetected = new int[CONSUMER_THREADS];

            Console.Write("\n\n--> Start test of Michael-Scott queue in producer/consumer context...\n\n");
		
		    // create and start the consumer threads.		
		    for (int i = 0; i<CONSUMER_THREADS; i++) {
			    int tid = i;
                consumers[i] = new Thread(() => {
                Random rnd = new Random(Thread.CurrentThread.ManagedThreadId);
                int count = 0;

                Console.Write("-->c#{0} starts...\n", tid);
                do
                {
                    try
                    {
                        String data = msqueue.Take();
                        if (!data.Equals("hello"))
                        {
                            failuresDetected[tid]++;
                                Console.Write("[f#{0}]", tid);
                        }

                        if (++count % 10 == 0)
                                Console.Write("[c#{0}]", tid);

                        // Simulate the time needed to process the data.

                        if (MAX_CONSUME_TIME > 0)
                            Thread.Sleep(rnd.Next(MAX_CONSUME_TIME));

                    }
                    catch (ThreadInterruptedException ie)
                    {
                        //do {} while (tid == 0);
                        break;
                    }
                } while (true);

                    // display the consumer thread's results.				
                    Console.Write("\n<--c#{0} exits, consumed: {1}, failures: {2}",
                                  tid, count, failuresDetected[tid]);
                consumptions[tid] = count;
                });
			    //consumers[i].d (true);
                consumers[i].Start();
            }

		// create and start the producer threads.		
		for (int i = 0; i<PRODUCER_THREADS; i++) {
			int tid = i;
            producers[i] = new Thread(() => {
            Random rnd = new Random(Thread.CurrentThread.ManagedThreadId);
        int count = 0;

                Console.Write("-->p#{0} starts...\n", tid);
        do
        {
            String data;

            if (rnd.Next(100) >= FAILURE_PERCENT)
            {
                data = "hello";
            }
            else
            {
                data = "HELLO";
                failuresInjected[tid]++;
            }

            // enqueue a data item
            msqueue.Put(data);

            // increment request count and periodically display the "alive" menssage.
            if (++count % 10 == 0)
                        Console.Write("[p#{0}]", tid);

            // production interval.

            try
            {
                Thread.Sleep(rnd.Next(MAX_PRODUCE_INTERVAL));
            }
            catch (ThreadInterruptedException ie)
            {
                //do {} while (tid == 0);
                break;
            }
        } while (true);

                // display the producer thread's results
                Console.Write("\n<--p#{0} exits, produced: {1}, failures: {2}",
                          tid, count, failuresInjected[tid]);
        productions[tid] = count;
    });
			//producers[i].setDaemon(true);
    producers[i].Start();
}

// run the test RUN_TIME milliseconds.

Thread.Sleep(RUN_TIME);

		// interrupt all producer threads and wait for for until each one finished. 
		int stillRunning = 0;
		for (int i = 0; i<PRODUCER_THREADS; i++) {
			producers[i].Interrupt();
producers[i].Join(JOIN_TIMEOUT);
			if (producers[i].IsAlive)
				stillRunning++;
			
		}
		
		// wait until the queue is empty 
		while (!msqueue.IsEmpty())
			Thread.Sleep(POLL_INTERVAL);
		
		// interrupt each consumer thread and wait for a while until each one finished.
		for (int i = 0; i<CONSUMER_THREADS; i++) {
			consumers[i].Interrupt();
consumers[i].Join(JOIN_TIMEOUT);
			if (consumers[i].IsAlive)
				stillRunning++;
		}
		
		// if any thread failed to fisnish, something is wrong.
		if (stillRunning > 0) {
                Console.Write("\n*** failure: {0} thread(s) did answer to interrupt\n", stillRunning);
			return false;
		}
				
		// compute and display the results.
		
		long sumProductions = 0, sumFailuresInjected = 0;
		for (int i = 0; i<PRODUCER_THREADS; i++) {
			sumProductions += productions[i];
			sumFailuresInjected += failuresInjected[i];
		}
		long sumConsumptions = 0, sumFailuresDetected = 0;
		for (int i = 0; i<CONSUMER_THREADS; i++) {
			sumConsumptions += consumptions[i];
			sumFailuresDetected += failuresDetected[i];
		}
            Console.Write("\n\n<-- successful: {0}/{1}, failed: {2}/{3}\n",
                          sumProductions, sumConsumptions, sumFailuresInjected, sumFailuresDetected);
						   
		return sumProductions == sumConsumptions && sumFailuresInjected == sumFailuresDetected;
	}
	
	    public static void Main(String[] args)
        {
            Console.Write("\n--> Test Michael-Scott concurrent queue: {0}\n",
				 		    (TestMichaelScottQueue() ? "passed" : "failed"));
        }
    }
}
