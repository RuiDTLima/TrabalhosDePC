import java.util.concurrent.atomic.AtomicReference;

public class ConcurrentQueue<T> {
    private class Node<T> {
        public T value;
        public AtomicReference<Node<T>> next;

        public Node(T value) {
            this.value = value;
            next = new AtomicReference<>(null);
        }
    }

    private Node<T> dummy = new Node<>(null); // qual utilidade de dummy
    private AtomicReference<Node<T>> head = new AtomicReference<>(dummy);
    private AtomicReference<Node<T>> tail = new AtomicReference<>(dummy);

    /**
     * coloca no fim da fila o elemento passado como argumento
     * @param elem
     */
    public void put(T elem){
        Node<T> node = new Node<>(elem);

        while (true) {
            Node<T> observedNode = tail.get();
            Node<T> observedNodeNext = observedNode.next.get();

            if(observedNode == tail.get()) {
                if (observedNodeNext != null) {  // significa que nesta iteração do while o o valor apontado pelo tail já tem next
                    tail.compareAndSet(observedNode, observedNodeNext);
                } else {
                    if (observedNode.next.compareAndSet(null, node)) {
                        tail.compareAndSet(observedNode, node);
                        return;
                    }
                }
            }
        }
    }

    /**
     * retorna o elemento presente no início da fila ou null , no caso da fila estar vazia
     * @return
     */
    public T tryTake(){
        while (true) {
            Node<T> observedHead = head.get();
            Node<T> node = observedHead.next.get();
            if (node == null)
                return null;
            if (head.compareAndSet(observedHead, node)) {
                T value = node.value;
                node.value = null;
                return value;
            }
        }
    }

    /*public T tryTake(){
        while (true) {
            if (head.get().next.get() == null)
                return null;
            Node<T> observedHead = head.get();
            Node<T> node = observedHead.next.get();
            if (node != null) {
                if (head.compareAndSet(observedHead, node)) {
                    T value = node.value;
                    node.value = null;
                    return value;
                }
            }
        }
    }*/

    /**
     * indica se a fila está vazia
     * @return
     */
    public boolean isEmpty(){
        return head.get().next.get() == null;
    }

    public T dequeue() throws InterruptedException {
        T v;
        while ((v = tryTake()) == null) {
            Thread.sleep(0);
        }
        return v;
    }
}