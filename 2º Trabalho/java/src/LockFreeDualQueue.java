import java.util.concurrent.atomic.AtomicReference;

public class LockFreeDualQueue<T> {
    // types of queue nodes
    private enum NodeType { DATUM, REQUEST }

    // the queue node
    private static class QNode<T> {
        NodeType type;
        final T data;
        final AtomicReference<QNode<T>> request;
        final AtomicReference<QNode<T>> next;

        //  build a datum or request node
        QNode(T d, NodeType t) {
            type = t;
            data = d;
            request = new AtomicReference<>(null);
            next = new AtomicReference<>(null);
        }
    }

    // the head and tail references
    private final AtomicReference<QNode<T>> head;
    private final AtomicReference<QNode<T>> tail;

    public LockFreeDualQueue() {
        QNode<T> sentinel = new QNode<T>(null, NodeType.DATUM);
        head = new AtomicReference<>(sentinel);
        tail = new AtomicReference<>(sentinel);
    }

    /**
     * Operação enqueue coloca no fim da fila o elemento v, sem o uso de zona exclusiva, mas de forma a garantir que não
     * existe perda de actualizações. Por isso a tentativa de inserir o elemento v na fila é feita dentro de um ciclo
     * até a operação ser bem sucedida. Isso acontece quando desde o inicio da tentativa de inserir o elemento na fila
     * até o inserir é garantido que a fila não sofreu alterações, verificando se o valor da tail da fila, o último
     * elemento não sofreu alterações. Quando a cabeça da lista observada no inicio do ciclo for igual à cauda da lista
     * observada no inicio do ciclo ou quando a cauda observada não for do tipo Request significa que a lista está vazia
     * ou a cauda não está devidamente actualizada ou ainda se a lista contiver elementos não devidamente ligados, então
     * são todas as definidas acções para tentar remediar esta situação, nomeadamente actualizar a cauda da lista para o
     * seu devido valor que tanto pode ser o nó correspondente ao elemento a inserir na lista, como pode ser o valor
     * seguinte observado relativamente à cauda da lista observada. Já caso nenhuma destas situações se verifique então
     * é tentado inserir o nó na cabeça da lista. O ciclo termina sempre que se consiga inserir o elemento na lista
     * @param v
     */
    public void enqueue(T v) {
        QNode<T> node = new QNode<>(v, NodeType.DATUM);
        while(true){
            QNode<T> observedTail = tail.get();
            QNode<T> observedHead = head.get();
            if (observedTail == observedHead || !observedTail.type.equals(NodeType.REQUEST)){
                // queue empty, tail falling behind, or queue contains data
                // (queue could also contain exactly one outstanding request with
                // tail pointer as yet unswung)
                QNode<T> observedNext = observedTail.next.get();
                if (observedTail == tail.get()){
                    if (observedNext != null){
                        tail.compareAndSet(observedTail, observedNext);
                    } else{ // tenta adicionar o novo elemento à lista
                        if (observedTail.next.compareAndSet(null, node)){
                            tail.compareAndSet(observedTail, node);
                            return;
                        }
                    }
                }
            } else{ // a lista consiste em elementos do tipo Request
                QNode<T> observedNext = observedHead.next.get();
                if (observedTail == tail.get()){
                    QNode<T> observedRequest = observedHead.request.get();
                    if (observedHead == head.get()){
                        boolean success = (observedRequest == null && observedHead.request.compareAndSet(null, node));
                        head.compareAndSet(observedHead, observedNext);
                        if (success)
                            return;
                    }
                }
            }
        }
    }

    // dequeue a datum - spinning if necessary
    public T dequeue() throws InterruptedException {
        QNode<T> h, hnext, t, tnext, n = null;
        do {
            h = head.get();
            t = tail.get();

            if (t == h || t.type == NodeType.REQUEST) {
                // queue empty, tail falling behind, or queue contains data (queue could also
                // contain exactly one outstanding request with tail pointer as yet unswung)
                tnext = t.next.get();

                if (t == tail.get()) {		// tail and next are consistent
                    if (tnext != null) {	// tail falling behind
                        tail.compareAndSet(t, tnext);
                    } else {	// try to link in a request for data
                        if (n == null) {
                            n = new QNode<T>(null, NodeType.REQUEST);
                        }
                        if (t.next.compareAndSet(null, n)) {
                            // linked in request; now try to swing tail pointer
                            tail.compareAndSet(t, n);

                            // help someone else if I need to
                            if (h == head.get() && h.request.get() != null) {
                                head.compareAndSet(h, h.next.get());
                            }

                            // busy waiting for a data done.
                            // we use sleep instead of yield in order to accept interrupts
                            while (t.request.get() == null) {
                                Thread.sleep(0);  // spin accepting interrupts!!!
                            }

                            // help snip my node
                            h = head.get();
                            if (h == t) {
                                head.compareAndSet(h, n);
                            }

                            // data is now available; read it out and go home
                            return t.request.get().data;
                        }
                    }
                }
            } else {    // queue consists of real data
                hnext = h.next.get();
                if (t == tail.get()) {
                    // head and next are consistent; read result *before* swinging head
                    T result = hnext.data;
                    if (head.compareAndSet(h, hnext)) {
                        return result;
                    }
                }
            }
        } while (true);
    }

    /**
     * Verifica se a lista não contém elemento, usando para isso a mesma condição que o método dequeue
     * @return
     */
    public boolean isEmpty() {
        return tail.get().request.get() == null;
    }
}