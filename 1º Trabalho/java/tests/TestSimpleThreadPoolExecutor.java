import org.junit.Assert;
import org.junit.Test;
import java.util.concurrent.RejectedExecutionException;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;

public class TestSimpleThreadPoolExecutor {
    @Test
    /**
     * Testa se o threadPool executa como esperado quando é passado apenas um comando para executar
     */
    public void oneElementThreadPool() throws InterruptedException {
        SimpleThreadPoolExecutor threadPool = new SimpleThreadPoolExecutor(1, 500);
        try {
            Assert.assertTrue(threadPool.execute(() -> {
                System.out.println("Executou thread");
                Assert.assertTrue(true);
            }, 500));
        } catch (InterruptedException e) {
            Assert.assertTrue(false);
            System.out.println("Erro na execução");
        }
        Thread.sleep(10);

        threadPool.shutdown();

        try {
            Assert.assertTrue(threadPool.awaitTermination(500));
        } catch (InterruptedException e) {
            Assert.assertTrue(false);
            e.printStackTrace();
        }
    }

    @Test
    /**
     * Testa se o shutdown do threadPool executa tal como esperado. Quando se executa o shutdown, qualquer tentativa de
     * colocar um novo comando em execução irá lançar a exceção RejectedExecutionException
     */
    public void secondElementFailingThreadPool() throws InterruptedException {
        SimpleThreadPoolExecutor threadPool = new SimpleThreadPoolExecutor(1, 100);

        try {
            Assert.assertTrue(threadPool.execute(() -> {
                System.out.println("Executou thread");
                Assert.assertTrue(true);
            }, 500));
        } catch (InterruptedException e) {
            Assert.assertTrue(false);
            System.out.println("Erro na execução");
        }

        Thread.sleep(10);
        threadPool.shutdown();

        try {
            Assert.assertTrue(threadPool.execute(() -> Assert.assertTrue(false), 500));
        } catch (RejectedExecutionException e) {
            Assert.assertTrue(true);
            System.out.println("RejectedExecutionException Occurred");
        } catch (InterruptedException e){
            Assert.assertTrue(false);
        }

        Thread.sleep(10);
        try {
            Assert.assertTrue(threadPool.awaitTermination(100));
        } catch (InterruptedException e) {
            Assert.assertTrue(false);
            e.printStackTrace();
        }
    }

    @Test
    /**
     * Testa se o timeout de um comando funciona como esperado. Quando é pedido a execução de um novo comando, estando
     * já o threadPool cheio, o novo comando deve ficar à espera e se o fizer por mais de timeout deve ser returnado
     * falso
     */
    public void timeoutWaitingForThread() throws InterruptedException {
        int threadPoolSize = 5;
        AtomicBoolean termination = new AtomicBoolean(false);
        SimpleThreadPoolExecutor threadPool = new SimpleThreadPoolExecutor(threadPoolSize, 10);

        for (int i = 0; i < threadPoolSize; i++) {
            try {
                threadPool.execute(()->{
                    while(!termination.get());
                }, 500);
            } catch (InterruptedException e) {
                Assert.assertTrue(false);
                e.printStackTrace();
            }
        }

        try {
            Assert.assertFalse(threadPool.execute(() -> System.out.println("Execução não esperada"), 100));
        } catch (InterruptedException e) {
            Assert.assertTrue(false);
            e.printStackTrace();
        }

        termination.set(true);
        Thread.sleep(100);
        threadPool.shutdown();

        try {
            Assert.assertTrue(threadPool.awaitTermination(500));
        } catch (InterruptedException e) {
            Assert.assertTrue(false);
            e.printStackTrace();
        }
    }

    @Test
    /**
     * Verifica se o threadPool distribui o trabalho tal como esperado mesmo quando este é superior, neste caso o dobro,
     * ao número de threads na pool
     */
    public void multipleExecutesSuccess() throws InterruptedException {
        int executes = 10;
        Thread[] threads = new Thread[executes];
        SimpleThreadPoolExecutor threadPool = new SimpleThreadPoolExecutor(executes/2, 500);
        AtomicBoolean termination = new AtomicBoolean(false);
        AtomicInteger counter = new AtomicInteger(0);

        for (int i = 0; i < executes; i++) {
            threads[i] = new Thread(()->{
                try {
                    threadPool.execute(()->{
                        while (!termination.get());
                        counter.incrementAndGet();
                    },550);
                } catch (InterruptedException e) {
                    e.printStackTrace();
                }
            });
            threads[i].start();
            Thread.sleep(10);
        }

        termination.set(true);
        for (int i = 0; i < executes; i++) {
            threads[i].join();
        }
        Assert.assertEquals(executes, counter.get());
    }

    @Test
    /**
     * Verifica se a execução funciona como esperada no caso do lançamento de uma excepção, não sendo concluido o
     * execute.
     */
    public void interruptWaitingForWorkerWithoutValue_Test() throws InterruptedException {
        String text = "done", interrupted = "interrupted";
        SimpleThreadPoolExecutor threadPoolExecutor = new SimpleThreadPoolExecutor(1, 2000);
        String[] result = new String[2];
        Thread t1 = new Thread(() -> {
            try {
                threadPoolExecutor.execute(() -> {
                    try {
                        Thread.sleep(2000);
                    } catch (InterruptedException e) {
                        e.printStackTrace();
                    }
                    result[0] = text;
                }, 100);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });

        Thread t2 = new Thread(() -> {
            try {
                threadPoolExecutor.execute(() -> result[1] = text, 3000);
            } catch (InterruptedException e) {
                result[1] = interrupted;
            }
        });

        t1.start();
        Thread.sleep(50);
        t2.start();
        Thread.sleep(50);
        t2.interrupt();


        t1.join();
        t2.join();
        Thread.sleep(3000);

        Assert.assertEquals(text, result[0]);
        Assert.assertEquals(interrupted, result[1]);
    }
}