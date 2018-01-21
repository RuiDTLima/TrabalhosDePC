import org.junit.Assert;
import org.junit.Test;

public class TestLockFreeDualQueue {
    @Test
    public void TestOneEnqueueDequeue(){
        LockFreeDualQueue<String> queue = new LockFreeDualQueue<>();
        String enqueued = "Hello", obtainedResponse = "";
        queue.enqueue(enqueued);

        try {
            obtainedResponse = queue.dequeue();
        } catch (InterruptedException e) {
            Assert.assertTrue(false);
        }

        Assert.assertEquals(enqueued, obtainedResponse);
    }

    @Test
    public void TestIsEmpty(){
        LockFreeDualQueue<String> queue = new LockFreeDualQueue<>();
        String enqueued = "Hello", obtainedResponse = "";
        Assert.assertTrue(queue.isEmpty());

        queue.enqueue(enqueued);

        Assert.assertFalse(queue.isEmpty());

        try {
            obtainedResponse = queue.dequeue();
        } catch (InterruptedException e) {
            Assert.assertTrue(false);
        }

        Assert.assertEquals(enqueued, obtainedResponse);

        Assert.assertTrue(queue.isEmpty());
    }

    @Test
    public void TestMultipleEnqueueDequeue(){
        LockFreeDualQueue<String> queue = new LockFreeDualQueue<>();
        String[] enqueueds = {"Hello", "WORLD", "Isel", "Lisboa", "Portugal"};
        String[] responses = new String[enqueueds.length];

        for (String enqueue : enqueueds) {
            queue.enqueue(enqueue);
        }

        for (int i = 0; i < responses.length; i++) {
            try {
                responses[i] = queue.dequeue();
            } catch (InterruptedException e) {
                Assert.assertTrue(false);
            }
        }

        for (int i = 0; i < responses.length; i++) {
            Assert.assertEquals(enqueueds[i], responses[i]);
        }
    }

    @Test
    public void TestMultiThreadEnqueue(){
        LockFreeDualQueue<String> queue = new LockFreeDualQueue<>();
        int numberOfMessages = 10;

        Assert.assertTrue(queue.isEmpty());

        String messages = "Hello World";
        String[] results = new String[numberOfMessages];
        Thread[] enqueueThreads = new Thread[numberOfMessages];
        Thread[] dequeueThreads = new Thread[numberOfMessages];

        for (int i = 0; i < dequeueThreads.length; i++) {
            int li = i;
            dequeueThreads[i] = new Thread(() -> {
                try {
                    results[li] = queue.dequeue();
                } catch (InterruptedException e) {
                    Assert.assertTrue(false);
                }
            });
            dequeueThreads[i].start();
        }

        for (int i = 0; i < enqueueThreads.length; i++) {
            int li = i;
            enqueueThreads[i] = new Thread(() -> queue.enqueue(messages));
            enqueueThreads[i].start();
        }

        for (Thread enqueueThread : enqueueThreads) {
            try {
                enqueueThread.join();
            } catch (InterruptedException e) {
                Assert.assertTrue(false);
            }
        }

        for (Thread dequeueThread : dequeueThreads){
            try {
                dequeueThread.join();
            } catch (InterruptedException e) {
                Assert.assertTrue(false);
            }
        }

        for (int i = 0; i < results.length; i++) {
            Assert.assertEquals(messages, results[i]);
        }
    }
}
