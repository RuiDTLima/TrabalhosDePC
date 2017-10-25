import java.util.LinkedList;
import java.util.concurrent.RejectedExecutionException;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;

public class SimpleThreadPoolExecutor {
    private final Object mon = new Object();
    private final LinkedList<Runnable> work = new LinkedList<>();
    private final int maxPoolSize;
    private final int keepAliveTime;
    private AtomicInteger activeThreads = new AtomicInteger(0);
    private AtomicBoolean shuttingDown = new AtomicBoolean(false);

    public int getActiveThreads() {
        return activeThreads.get();
    }

    public SimpleThreadPoolExecutor(int maxPoolSize, int keepAliveTime){
        this.maxPoolSize = maxPoolSize;
        this.keepAliveTime = keepAliveTime;
    }

    /**
     * Inicialmente é verificado se o pool não está a ser encerrado para poder prosseguir com a sua execução normal.
     * Caso o número total de threads actualmente em trabalho ou à procura de trabalho seja menor que o tamanho máximo
     * de threads que o pool pode ter, é criada uma nova thread e esta é colocada à procura de trabalho através do
     * método runWork, o trabalho fica depois à espera de ser executado, antes de que o timeout termine, ou que a espera
     * seja interrumpida
     * @param command o método a ser executado pela thread, o trabalho
     * @param timeout o tempo máximo que o trabalho fica à espera que alguma thread o "atenda"
     * @return
     * @throws InterruptedException
     * @throws RejectedExecutionException
     */
    public boolean execute(Runnable command, int timeout) throws InterruptedException, RejectedExecutionException{
        synchronized (mon){
            if (shuttingDown.get())
                throw new RejectedExecutionException();
            if (activeThreads.get() < maxPoolSize){
                Thread thread = new Thread(this::runWork);
                thread.start();
                activeThreads.incrementAndGet();
            }

            work.add(command);
            long t = Timeouts.start(timeout);
            long remaining = Timeouts.remaining(t);
            while(true){
                try {
                    mon.wait(remaining);
                }catch (InterruptedException e){
                    work.remove(command);
                    throw e;
                }

                if (!work.contains(command))
                    return true;

                remaining = Timeouts.remaining(t);
                if (Timeouts.isTimeout(remaining)) {
                    work.remove(command);
                    return false;
                }
            }
        }
    }

    /**
     * Fica num
     */
    private void runWork() {
        long t = Timeouts.start(keepAliveTime);
        long remaining = Timeouts.remaining(t);
        while (true){
            if (Timeouts.isTimeout(remaining)){
                activeThreads.decrementAndGet();
                return;
            }
            Runnable runnable = getThreadWork();
            if (runnable != null){
                runnable.run();
                t = Timeouts.start(keepAliveTime);
            }
            remaining = Timeouts.remaining(t);
        }
    }

    private Runnable getThreadWork() {
        synchronized (mon) {
            if (work.size() != 0) {
                return work.removeFirst();
            }
            return null;
        }
    }

    public void shutdown(){
        shuttingDown.set(true);
    }

    public boolean awaitTermination(int timeout) throws InterruptedException {
        synchronized (mon){
            if (activeThreads.get() == 0)
                return true;

            if (Timeouts.noWait(timeout))
                return false;

            long t = Timeouts.start(timeout);
            long remaining = Timeouts.remaining(t);
            while(true){
                mon.wait(remaining);

                if (activeThreads.get() == 0)
                    return true;

                remaining = Timeouts.remaining(t);
                if (Timeouts.isTimeout(remaining))
                    return false;
            }
        }
    }
}