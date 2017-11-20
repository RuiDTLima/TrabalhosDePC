import org.junit.Assert;
import org.junit.Test;

public class TestConcurrentQueue {
    @Test
    public void testOneElementPutAndTryTake(){
        ConcurrentQueue<String> queue = new ConcurrentQueue<>();
        String messageSend = "Hello World";

        Assert.assertTrue(queue.isEmpty());
        queue.put(messageSend);

        Assert.assertFalse(queue.isEmpty());
        String result = queue.tryTake();
        Assert.assertTrue(queue.isEmpty());

        Assert.assertEquals(messageSend, result);
    }

    @Test
    public void testTwoElementPutAndTryTake(){
        ConcurrentQueue<String> queue = new ConcurrentQueue<>();
        String firstMessageSend = "Hello World";
        String secondMessageSend = "ISEL";

        Assert.assertTrue(queue.isEmpty());
        queue.put(firstMessageSend);

        Assert.assertFalse(queue.isEmpty());
        queue.put(secondMessageSend);

        String firstResult = queue.tryTake();
        Assert.assertFalse(queue.isEmpty());

        String secondResult = queue.tryTake();

        Assert.assertTrue(queue.isEmpty());

        Assert.assertEquals(firstMessageSend, firstResult);
        Assert.assertEquals(secondMessageSend, secondResult);
    }

    @Test
    public void testMultiplePutAndTryTake(){
        ConcurrentQueue<String> queue = new ConcurrentQueue<>();
        String[] messages = {"Hello World", "Hello ISEL", "This is Threads", "This is PC", "Windows"};
        String[] results = new String[messages.length];
        Assert.assertTrue(queue.isEmpty());

        for (int i = 0; i < messages.length; i++) {
            queue.put(messages[i]);
        }

        Assert.assertFalse(queue.isEmpty());

        for (int i = 0; i < messages.length; i++) {
            results[i] = queue.tryTake();
        }

        Assert.assertTrue(queue.isEmpty());

        for (int i = 0; i < messages.length; i++) {
            Assert.assertEquals(messages[i], results[i]);
        }
    }

    @Test
    public void testTryTakeNullReturn(){
        ConcurrentQueue<String> queue = new ConcurrentQueue<>();

        Assert.assertNull(queue.tryTake());
    }

    @Test
    public void testMultipleThreadsPutAndTryTake() throws InterruptedException {
        ConcurrentQueue<String> queue = new ConcurrentQueue<>();

        Assert.assertTrue(queue.isEmpty());

        String[] messages = {"Hello World", "Hello ISEL", "This is Threads", "This is PC", "Windows"};
        String[] results = new String[messages.length];
        Thread[] threads = new Thread[messages.length];

        for (int i = 0; i < threads.length; i++) {
            int j = i;
            threads[j] = new Thread(()-> queue.put(messages[j]));
            threads[i].start();
            Thread.sleep(10);
        }

        for (int i = 0; i < threads.length; i++) {
            threads[i].join();
        }

        Assert.assertFalse(queue.isEmpty());

        for (int i = 0; i < threads.length; i++) {
            int j = i;
            threads[j] = new Thread(()-> results[j] = queue.tryTake());
            threads[i].start();
            Thread.sleep(10);
        }

        for (int i = 0; i < threads.length; i++) {
            threads[i].join();
            Assert.assertEquals(messages[i], results[i]);
        }

        Assert.assertTrue(queue.isEmpty());
    }
}