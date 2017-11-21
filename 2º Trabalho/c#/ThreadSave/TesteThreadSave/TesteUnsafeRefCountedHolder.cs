using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadSave;
using System.Threading;

namespace TesteThreadSave
{
    [TestClass]
    public class TesteUnsafeRefCountedHolder
    {
        [TestMethod]
        public void MultiThreadAdds() {
            UnsafeRefCountedHolder<String> holder = new UnsafeRefCountedHolder<string>("Hello World");
            Thread[] threads = new Thread[10];

            for (int i = 0; i < threads.Length; i++) {
                threads[i] = new Thread(() => {
                    holder.AddRef();
                });
                threads[i].Start();
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            for (int i = 0; i < threads.Length; i++)
            {
                holder.ReleaseRef();
            }

            //holder.ReleaseRef();
            String result;
            try
            {
                result = holder.Value;
            }
            catch (InvalidOperationException)
            {
                Assert.IsTrue(true);
                return;
            }

            Assert.AreEqual("Hello World", result);
        }
    }
}
