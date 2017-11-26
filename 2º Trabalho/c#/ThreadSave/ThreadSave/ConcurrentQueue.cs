using System;
using System.Threading;

namespace ThreadSave {
    public class ConcurrentQueue<T>  {
        /**
         * Classe de nós que farão a estrutura da lista
         */
        private class Node<T> {
            public T value;
            public Node<T> next;

            public Node(T value) {
                this.value = value;
                next = null;
            }
        }

        /**
         * Elemento inicial da lista para o qual a cabeça e a cauda da lista apontam no começo da vida da lista 
         */
        private Node<T> dummy = new Node<T>(default(T));
        private Node<T> head;   
        private Node<T> tail;   

        public ConcurrentQueue() {
            head = dummy;
            tail = dummy;
        }

        /**
         *  É criado inicialmente um novo elemento pronto a ser adicionado ao queue, e é guardado o estado 
         *  do queue no inicio de cada iteração do ciclo, é tentado depois adicionar o novo elemento ao
         *  queue sempre que o estado em que começou a inserção se mantenha o mesmo, para isso usa-se 
         *  instruções atómicas, neste caso o Interlocked.CompareExchange o qual coloca a cauda do 
         *  queue(tail) a apontar para o novo elemento caso o último elemento do queue no inicio da 
         *  iteração não esteja a apontar para outro elemento, o que significaria que estava a meio 
         *  uma alteração do queue. Durante a inserção de um novo elemento ao queue, a cabeça não é
         *  alterada, dado que as inserções são sempre feitas no fim da fila.
         */
        public void Put(T elem) {
            Node<T> node = new Node<T>(elem);

            while (true) {
                Node<T> observedNode = Interlocked.Exchange(ref tail, tail);
                Node<T> observedNodeNext = observedNode.next;

                if(observedNode == tail) {
                    if(observedNodeNext != null) {
                        Interlocked.CompareExchange(ref tail, observedNodeNext, observedNode);
                    } else {
                        if(Interlocked.CompareExchange(ref observedNode.next, node, null) == null) {
                            Interlocked.CompareExchange(ref tail, node, observedNode);
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
         */
        public T TryTake() {
            while (true) {
                if (head.next == null)
                    return default(T);
                Node<T> observedHead = Interlocked.Exchange(ref head, head);
                Node<T> node = observedHead.next;
                if(node != null) {
                    if(Interlocked.CompareExchange(ref head, node, observedHead) == observedHead) {
                        T value = node.value;
                        node.value = default(T);
                        return value;
                    }
                }
            }
        }

        /**
         *  Caso a cabeça da fila não aponte para nenhum elemento considera-se que a fila está vazia 
         */
        public bool IsEmpty() {
            return head.next == null;
        }

        /**
         *  Fica num ciclo infinito a tentar remover um elemento da fila 
         */
        public T Take() {
            T v;
            while((v = TryTake()) == null) {
                Thread.Sleep(0);
            }
            return v;
        }
    }
}
