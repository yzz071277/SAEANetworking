using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SAEANetworking
{

    /// <summary>
    /// 客户端数据类
    /// </summary>
    public class SAEAClientData
    {
        /// <summary>
        /// 当前连接的客户端ID
        /// </summary>
        public uint clientID;

        /// <summary>
        /// 客户端是否连接
        /// </summary>
        public bool isConnected;

        /// <summary>
        /// 客户端对象是本地还是在服务器上的
        /// </summary>
        public ClientType ClientType { get; private set; }

        /// <summary>
        /// 当前连接客户端的套接字
        /// </summary>
        public Socket clientSocket;

        /// <summary>
        /// 当前连接客户端的接收buffer
        /// </summary>   
        public byte[] receiveBuffer;

        /// <summary>
        /// 当前连接客户端的待发送消息队列
        /// </summary>
        public Queue<byte[]> MessageQueue { get; private set; }

        /// <summary>
        /// 同于发送消息的SocketAsyncEventArgs对象
        /// </summary>
        public SocketAsyncEventArgs SendSocketAsyncEventArgs;

        /// <summary>
        /// 用于接收消息的SocketAsyncEventArgs对象
        /// </summary>
        public SocketAsyncEventArgs ReceiveSocketAsyncEventArgs;

        /// <summary>
        /// 消息是否异步发送成功
        /// </summary>
        public bool isAsyncSendCompleted;

        /// <summary>
        /// 消息是否异步接收成功
        /// </summary>
        public bool isAsyncReceiveCompleted;

        /// <summary>
        /// 当前队列中的消息是否已经全部发送完成
        /// </summary>
        public bool isQueueSendCompleted;

        /// <summary>
        /// 用于处理收发消息的回调函数
        /// </summary>
        private EventHandler<SocketAsyncEventArgs> eventHandler;

        /// <summary>
        /// 上一条消息剩余数据
        /// </summary>
        public int residueCount = 0;

        /// <summary>
        /// 接收不完整的数据
        /// </summary>
        public byte[] unfinishedByte = new byte[0];

        /// <summary>
        /// 上次接收剩余的数据
        /// </summary>
        public byte[] residueByte = new byte[0];

        /// <summary>
        /// 开始发送的起始位置
        /// </summary>
        public int startIndex;

        /// <summary>
        /// 剩余待发送长度
        /// </summary>
        public int residueLength;

        /// <summary>
        /// ClientData类的构造函数,用于初始化一个新的客户端对象
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="clientSocket"></param>
        /// <param name="bufferLength"></param>
        /// <param name="eventHandler"></param>
        public SAEAClientData(uint clientID, ClientType clientType, Socket clientSocket, int bufferLength, EventHandler<SocketAsyncEventArgs> eventHandler)
        {
            this.clientID = clientID;
            ClientType = clientType;
            this.clientSocket = clientSocket;

            startIndex = 0;

            receiveBuffer = new byte[bufferLength];
            MessageQueue = new Queue<byte[]>();

            SendSocketAsyncEventArgs = SAEAPool.GetSocketAsyncEventArgs();
            ReceiveSocketAsyncEventArgs = SAEAPool.GetSocketAsyncEventArgs();

            this.eventHandler = eventHandler;

            SendSocketAsyncEventArgs.Completed += this.eventHandler;
            ReceiveSocketAsyncEventArgs.Completed += this.eventHandler;
            ReceiveSocketAsyncEventArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            SendSocketAsyncEventArgs.UserToken = this;
            ReceiveSocketAsyncEventArgs.UserToken = this;

            isQueueSendCompleted = true;

        }

        /// <summary>
        /// 当接受到一条完整网络消息时的回调
        /// </summary>
        public Action<SAEAClientData, byte[]> OnReceiveNetworkMessage;

        /// <summary>
        /// 当前待发送消息队列剩余消息数量
        /// </summary>
        public int MessageCount
        {
            get
            {
                if (MessageQueue == null) return 0;
                return MessageQueue.Count;
            }
        }

        /// <summary>
        /// 向待发送消息队列中增加一条消息
        /// </summary>
        /// <param name="message"></param>
        public void AddMessage(byte[] message)
        {
            if (MessageQueue == null) return;
            MessageQueue.Enqueue(message);
        }

        /// <summary>
        /// 从待发送消息队列中获取一条消息
        /// </summary>
        /// <returns></returns>
        public byte[] GetMessage(bool isPeek = false)
        {
            if (MessageCount == 0) return null;
            if (!isPeek)
            {
                return MessageQueue.Dequeue();
            }
            else
            {
                return MessageQueue.Peek();
            }
        }

        /// <summary>
        /// 销毁这个客户端数据对象
        /// </summary>
        public virtual void DestoryThisClient()
        {
            isConnected = false;
            MessageQueue.Clear();
            isQueueSendCompleted = true;
            SendSocketAsyncEventArgs.Completed -= eventHandler;
            ReceiveSocketAsyncEventArgs.Completed -= eventHandler;


            SAEAPool.RecycleSocketAsyncEventArgs(SendSocketAsyncEventArgs);
            SAEAPool.RecycleSocketAsyncEventArgs(ReceiveSocketAsyncEventArgs);
        }
    }

    /// <summary>
    /// 客户端对象所属类型
    /// </summary>
    public enum ClientType
    {
        LocalClient,  //本地客户端创建
        ServerClient,  //服务端创建
    }


}