using System;
using System.Threading;

namespace ThreadSave {
    public class ConcurrentQueue<T>  {
        private class Node<T> {
            public T value;
            public Node<T> next;

            public Node(T value) {
                this.value = value;
                next = null;
            }
        }

        private Node<T> dummy = new Node<T>(default(T));
        private Node<T> head;
        private Node<T> tail;

        public ConcurrentQueue() {
            head = dummy;
            tail = dummy;
        }

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

        public bool IsEmpty() {
            return head.next == null;
        }

        public T Take()
        {
            T v;
            while((v = TryTake()) == null)
            {
                Thread.Sleep(0);
            }
            return v;
        }
    }
}
