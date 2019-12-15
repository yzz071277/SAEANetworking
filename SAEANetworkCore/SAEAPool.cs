using System.Collections.Generic;
using System.Net.Sockets;

namespace SAEANetworking
{

    /// <summary>
    /// SAEA对象池类
    /// </summary>
    public static class SAEAPool
    {
        /// <summary>
        /// 用于存放所有SocketAsyncEventArgs的对象池
        /// </summary>
        static private Queue<SocketAsyncEventArgs> socketAsyncEventArgsPool = new Queue<SocketAsyncEventArgs>();

        /// <summary>
        /// 从对象池中取出一个SocketAsyncEventArgs对象
        /// </summary>
        /// <returns></returns>
        static public SocketAsyncEventArgs GetSocketAsyncEventArgs()
        {
            SocketAsyncEventArgs getAsyncEventArgs;

            lock (socketAsyncEventArgsPool)
            {
                if (socketAsyncEventArgsPool.Count <= 0)
                {
                    getAsyncEventArgs = new SocketAsyncEventArgs();
                }
                else
                {
                    getAsyncEventArgs = socketAsyncEventArgsPool.Dequeue();
                }
            }
            return getAsyncEventArgs;

        }

        /// <summary>
        /// 回收SocketAsyncEventArgs对象
        /// </summary>
        /// <returns></returns>
        static public SocketAsyncEventArgs RecycleSocketAsyncEventArgs(SocketAsyncEventArgs e)
        {
            if (e == null) return null;
            e.RemoteEndPoint = null;
            socketAsyncEventArgsPool.Enqueue(e);
            return null;
        }
    }

}