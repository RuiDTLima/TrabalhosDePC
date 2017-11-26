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
     *  É criado inicialmente um novo elemento pronto a ser adicionado ao queue, e é guardado o estado
     *  do queue no inicio de cada iteração do ciclo, tenta-se depois adicionar o novo elemento ao
     *  queue sempre que o estado em que começou a inserção se mantenha o mesmo, para isso usa-se
     *  instruções atómicas, neste caso o compareAndSet o qual coloca a cauda do
     *  queue(tail) a apontar para o novo elemento caso o último elemento do queue no inicio da
     *  iteração não esteja a apontar para outro elemento, o que significaria que estava a meio
     *  uma alteração do queue. Durante a inserção de um novo elemento ao queue, a cabeça não é
     *  alterada, dado que as inserções são sempre feitas no fim da fila.
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
     *  É retirado o primeiro elemento da fila. Caso a fila esteja vazia é retornado null. Num ciclo
     *  tenta-se remover o elemento da fila, para isso guarda-se o estado inicial da fila, e sempre
     *  que se consiga colocar a cabeça da fila a apontar para o elemento que ocupava a segunda
     *  posição da fila, o primeiro elemento da fila é removido e é retornado o seu valor, colocado o
     *  valor do nó com o valor default de T, para o Garbage Colector poder "limpar" o elemento.
     * @return
     */
    public T tryTake(){
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
    }

    /**
     * indica se a fila está vazia. O que acontece quando a cabeça da fila não aponte para nenhum elemento
     * @return
     */
    public boolean isEmpty(){
        return head.get().next.get() == null;
    }

    /**
     * Fica num ciclo infinito a tentar remover um elemento da fila
     * @return
     * @throws InterruptedException
     */
    public T dequeue() throws InterruptedException {
        T v;
        while ((v = tryTake()) == null) {
            Thread.sleep(0);
        }
        return v;
    }
}