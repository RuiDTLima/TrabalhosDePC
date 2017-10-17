public class TransferQueue<T> {
    private static final Object mon = new Object();

    public void Put(T msg){

    }

    public bool Transfer(T msg, int timeout) throws InterruptedException {

    }

    public bool Take(int timeout, out T rmsg) throws InterruptedException {

    }
}