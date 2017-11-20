import java.util.LinkedList;
import java.util.concurrent.RejectedExecutionException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.ReentrantLock;

public class SimpleThreadPoolExecutor {
    private final ReentrantLock lock;
    private final LinkedList<WorkItem> work = new LinkedList<>();
    private final LinkedList<WorkerThread> threads = new LinkedList<>();
    private final Condition waitTermination;    // Condição para bloquear as threads na espera da terminação do ThreadPool ou quando uma thread está à espera de trabalho
    private final int maxPoolSize, keepAliveTime;
    private int workingThreads,waitingTerminationThreads;
    private boolean isShuttingDown;

    public SimpleThreadPoolExecutor(int maxPoolSize, int keepAliveTime){
        this.maxPoolSize = maxPoolSize;
        this.keepAliveTime = keepAliveTime;
        lock = new ReentrantLock();
        waitTermination = lock.newCondition();
        waitingTerminationThreads = 0;
    }

    /**
     * Caso o ThreadPool esteja em shutdown lança a excepção RejectedExecutionException, uma vez que o pedido de
     * realização de trabalho não será satisfeito. Depois verifica se existe alguma thread à espera de trabalho, em caso
     * afirmativo é lhe dado o trabalho acabado de receber e é acordada de forma a executar o trabalho. Por outro lado
     * caso o número de threads a trabalhar ou à procura de trabalho seja inferior ao tamanho máximo do threadPool é
     * criada uma nova thread que é colocada a executar o trabalho recebido como parâmetro. Caso contrário é criado um
     * novo elemento trabalho que é colocado na lista de trabalho e é colocado em espera até alguma thread estar
     * disponivel para o executar ou passar o timeout
     * @param command // o comando a ser executado
     * @param timeout // o tempo máximo que o trabalho pode estar bloqueado
     * @return
     * @throws InterruptedException
     */
    public boolean execute(Runnable command, int timeout) throws InterruptedException{
        lock.lock();
        try {
            if (isShuttingDown)
                throw new RejectedExecutionException();

            if(!threads.isEmpty()){
                WorkerThread worker = threads.removeLast();
                worker.setCommand(command);
                worker.ready = true;
                worker.waitThread.signal();
                return true;
            }

            if (workingThreads < maxPoolSize){
                WorkerThread worker = new WorkerThread(command);
                worker.start();
                workingThreads++;
                return true;
            }

            WorkItem workItem = new WorkItem(command, lock.newCondition());
            work.add(workItem);

            long t = Timeouts.start(timeout);
            long remaining = Timeouts.remaining(t);
            while (true){
                try {
                    workItem.condition.await(remaining, TimeUnit.MILLISECONDS);
                }catch (InterruptedException e){
                    if (workItem.isExecuting) {
                        Thread.currentThread().interrupt();
                        return true;
                    }
                    work.remove(workItem);
                    throw e;
                }

                if (workItem.isExecuting)
                    return true;

                remaining = Timeouts.remaining(t);
                if (Timeouts.isTimeout(remaining)) {
                    work.remove(workItem);
                    return false;
                }
            }
        }finally {
            lock.unlock();
        }
    }

    /**
     * O ThreadPool é colocado em modo shutdown e caso já exista algumas threads bloqueada à espera que o pool termine
     * estas são acordadas, uma vez que houve uma alteração do estado do pool
     */
    public void shutdown(){
        lock.lock();
        try {
            isShuttingDown = true;
            if (waitingTerminationThreads > 0) {
                waitingTerminationThreads = 0;
                waitTermination.signalAll();
            }
        }finally {
            lock.unlock();
        }
    }

    /**
     * Fica à espera da terminação do pool antes que o timeout seja ultrapassado. Caso não existam threads a trabalhar e
     * o pool esteja a ser encerrado, retorna true porque o pool foi encerrado com sucesso. Caso contrário verifica se
     * Fica bloqueado à espera que todas as threads actualmente em trabalho terminem a sua execução, caso isso aconteça
     * dentro do timeout retorna true, caso contrário retorna false
     * @param timeout
     * @return
     * @throws InterruptedException
     */
    public boolean awaitTermination(int timeout) throws InterruptedException{
        lock.lock();
        try {
            if (workingThreads == 0 && isShuttingDown)
                return true;

            if (Timeouts.noWait(timeout))
                return false;

            long t = Timeouts.start(timeout);
            long remaining = Timeouts.remaining(t);
            waitingTerminationThreads++;
            while(true){
                try {
                    waitTermination.await(remaining, TimeUnit.MILLISECONDS);
                }catch (InterruptedException e){
                    waitingTerminationThreads--;
                    if (workingThreads == 0 && isShuttingDown)
                        return true;
                    throw e;
                }
                if (workingThreads == 0 && isShuttingDown)
                    return true;
                remaining = Timeouts.remaining(t);
                if (Timeouts.isTimeout(remaining)){
                    waitingTerminationThreads--;
                    return false;
                }
            }
        }finally {
            lock.unlock();
        }
    }

    /**
     * Classe para reprensentar o trabalho a ser realizado
     */
    private class WorkItem{
        public final Condition condition;
        private final Runnable work;
        private boolean isExecuting;

        private WorkItem(Runnable work, Condition condition) {
            this.work = work;
            this.condition = condition;
        }

        public Runnable getWork(){
            return work;
        }
    }

    /**
     * Classe para representar as Threads usadas pelo ThreadPool para realizar trabalho
     */
    private class WorkerThread extends Thread{
        private Runnable command;
        private Condition waitThread;
        public boolean ready;
        private long timeLiving = TimeUnit.MILLISECONDS.toNanos(keepAliveTime);

        public void setCommand(Runnable command){
            this.command = command;
        }

        private WorkerThread(Runnable command){
            this.command = command;
            waitThread = lock.newCondition();
            ready = true;
        }

        @Override
        public void run() {
            do{
                command.run();
            }while(findWork());
        }

        /**
         * Procura trabalho durante o tempo em que pode estar viva. Caso exista trabalho disponivel a thread actual
         * passa a executá-lo e sinaliza a condição do trabalho para ele sair da espera. Caso contrário e caso o
         * ThreadPool esteja em shutdown é returnado false de modo a parar a execução da thread. Se nenhuma dessas
         * situações se verificar a thread é colocada em espera durante o tempo que cada thread pode estar sem trabalho
         * @return true caso encontre trabalho para executar, false caso seja para terminar a execução da thread
         */
        private boolean findWork() {
            lock.lock();
            try {
                if (!work.isEmpty()){
                    WorkItem current = work.removeFirst();
                    command = current.getWork();
                    current.isExecuting = true;
                    current.condition.signal();
                    return true;
                }

                if (isShuttingDown) {
                    workingThreads--;
                    if (workingThreads == 0 && isShuttingDown) {
                        waitingTerminationThreads = 0;
                        waitTermination.signalAll();
                    }
                    return false;
                }

                ready = false;
                threads.add(this);
                while (true){
                    try {
                        timeLiving = waitThread.awaitNanos(timeLiving);
                    } catch (InterruptedException e) {
                        ;//ignored
                    }
                    if (ready) {
                        return true;
                    }
                    if (Timeouts.isTimeout(timeLiving)){
                        workingThreads--;
                        threads.remove(this);
                        if (workingThreads == 0 && isShuttingDown) {
                            waitingTerminationThreads = 0;
                            waitTermination.signalAll();
                        }
                        return false;
                    }
                }
            }finally {
                lock.unlock();
            }
        }
    }
}