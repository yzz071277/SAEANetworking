using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;

namespace SAEANetworking
{
    namespace SAEADiscovery {

        /// <summary>
        /// 将本机IP和进行广播或接收广播的网络发现类
        /// </summary>
        public class SAEANetworkDiscovery
        {

            /// <summary>
            /// 广播间隔时间
            /// </summary>
            public float SendBroadcastInterval { get; private set; }


            /// <summary>
            /// 当开始发送广播时的事件
            /// </summary>
            public Action OnStartSendBroadcast;

            /// <summary>
            /// 当完成发送一条广播时的事件
            /// </summary>
            public Action OnSendBroadcastCompleted;

            /// <summary>
            /// 当停止发送广播时的事件
            /// </summary>
            public Action OnStopSendBroadcast;


            /// <summary>
            /// 当开始接收广播时的事件
            /// </summary>
            public Action OnStartReceiveBroadcast;

            /// <summary>
            /// 当接收到广播时执行的事件
            /// </summary>
            public Action<byte[]> OnReceiveBroadcast;

            /// <summary>
            /// 当超时时执行的事件
            /// </summary>
            public Action OnTimeOut;

            /// <summary>
            /// 当停止接收广播时的事件
            /// </summary>
            public Action OnStopReceiveBroadcast;


            private Socket sendingEndSocket;
            private Socket receivingEndSocket;

            private IPEndPoint sendingEndPoint;
            private IPEndPoint receivingEndPoint;

            private SocketAsyncEventArgs sendingSAEA;
            private SocketAsyncEventArgs receivingSAEA;

            private string messageStr;
            private byte[] messageByte;

            private bool isStartSend = false;
            private bool isStartReceive = false;

            private byte[] buffer = new byte[1024];

            Timer timeOutTimer;

            bool isSendingEnd;
            bool isReceivingEnd;



            /// <summary>
            /// 创建广播发送端
            /// </summary>
            public void CreateBroadcastSendingEnd(int broadcastPort)
            {
                if (isStartSend) return;
                if (isSendingEnd) return;
                sendingEndSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sendingEndPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
                sendingEndSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                messageStr = SAEASocketManager.GetHostName() + "|" + SAEASocketManager.GetAddress();
                messageByte = SAEAMessageTools.AddLengthMarkerForMassage(SAEAMessageTools.serializeStringToByte(messageStr));
                sendingSAEA = SAEAPool.GetSocketAsyncEventArgs();
                sendingSAEA.RemoteEndPoint = sendingEndPoint;
                sendingSAEA.Completed += IO_Completed;
                isSendingEnd = true;
            }

            /// <summary>
            /// 开始广播
            /// </summary>
            public void StartBroadcast(float broadcastInterval = 1f)
            {
                if (!isStartSend && sendingSAEA != null && sendingEndSocket != null)
                {
                    OnStartSendBroadcast?.Invoke();
                    isStartSend = true;
                    SendBroadcastInterval = broadcastInterval;
                    SendBroadcast(sendingSAEA);
                }
            }

            /// <summary>
            /// 发送广播数据
            /// </summary>
            private void SendBroadcast(SocketAsyncEventArgs e)
            {
                sendingSAEA.SetBuffer(messageByte, 0, messageStr.Length);
                bool isCompleted = sendingEndSocket.SendToAsync(e);
                if (!isCompleted)
                {
                    OnSentBroadcast(e);
                }
            }

            /// <summary>
            /// 当异步发送完成一条消息时执行的方法
            /// </summary>
            /// <param name="e"></param>
            private void OnSentBroadcast(SocketAsyncEventArgs e)
            {
                e.SetBuffer(null, 0, 0);
                OnSendBroadcastCompleted?.Invoke();
                if (SendBroadcastInterval > 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(SendBroadcastInterval));
                }

                if (isStartSend)
                {
                    SendBroadcast(e);
                }
            }

            /// <summary>
            /// 停止广播
            /// </summary>
            public void StopBroadcast()
            {
                if (isStartSend)
                {
                    isStartSend = false;
                    SAEASocketManager.CloseSocket(sendingEndSocket);
                    if (OnStopSendBroadcast != null)
                    {
                        OnStopSendBroadcast();
                    }
                }
            }

            /// <summary>
            /// 更改广播发送间隔时间
            /// </summary>
            /// <param name="value"></param>
            public void ChangeBroadcastInterval(float value)
            {
                if (value < 0) return;
                SendBroadcastInterval = value;
            }

            /// <summary>
            /// 关闭广播发送端
            /// </summary>
            public void CloseBroadcastSendingEnd()
            {
                StopBroadcast();
                sendingEndSocket = null;
                sendingEndPoint = null;
                if (sendingSAEA != null)
                {
                    sendingSAEA.Completed -= IO_Completed;
                    SAEAPool.RecycleSocketAsyncEventArgs(sendingSAEA);
                    sendingSAEA = null;
                }

                isSendingEnd = false;
            }






            /// <summary>
            /// 创建广播接收端
            /// </summary>
            public void CreateBroadcastReceivingEnd(int broadcastPort)
            {
                if (isStartReceive) return;
                if (isReceivingEnd) return;

                receivingEndSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                receivingEndPoint = new IPEndPoint(IPAddress.Any, broadcastPort);
                receivingEndSocket.Bind(receivingEndPoint);

                receivingSAEA = SAEAPool.GetSocketAsyncEventArgs();
                receivingSAEA.RemoteEndPoint = receivingEndPoint;
                receivingSAEA.Completed += IO_Completed;

                isReceivingEnd = true;
            }

            /// <summary>
            /// 开始接收广播
            /// </summary>
            public void StartReceiveBroadcast(float timeOut = 0)
            {
                if (!isStartReceive && receivingEndSocket != null && receivingSAEA != null)
                {
                    OnStartReceiveBroadcast?.Invoke();
                    isStartReceive = true;
                    ReceiveBroadcast(receivingSAEA);
                    //开启超时判断计时器
                    if (timeOut > 0)
                    {
                        timeOutTimer = new Timer(new TimerCallback(TimeOutCallback), null, TimeSpan.FromSeconds(timeOut), TimeSpan.FromSeconds(0));
                    }
                }
            }

            /// <summary>
            /// 接收广播
            /// </summary>
            /// <param name="e"></param>
            private void ReceiveBroadcast(SocketAsyncEventArgs e)
            {
                bool isReceive = receivingEndSocket.ReceiveFromAsync(e);
                if (!isReceive)
                {
                    OnReceivedBroadcast(e);
                }
            }

            /// <summary>
            /// 当接收到广播的回调
            /// </summary>
            /// <param name="e"></param>
            private void OnReceivedBroadcast(SocketAsyncEventArgs e)
            {
                if (e.SocketError != SocketError.Success)
                {
                    StopReceiveBroadcast();
                }
                if (e.BytesTransferred > 4)
                {
                    if (SAEAMessageTools.DeserializeByteToInt(e.Buffer, 0) == e.Buffer.Length - 4)
                    {
                        OnReceiveBroadcast?.Invoke(e.Buffer);
                    }
                }
                if (isStartReceive)
                {
                    ReceiveBroadcast(e);
                }
            }

            /// <summary>
            /// 停止接收广播
            /// </summary>
            public void StopReceiveBroadcast()
            {
                if (!isStartReceive) return;
                if (timeOutTimer != null) timeOutTimer.Dispose();
                SAEASocketManager.CloseSocket(receivingEndSocket);
                OnStopReceiveBroadcast?.Invoke();
                isStartReceive = false;
            }

            /// <summary>
            /// 当超时时执行的操作
            /// </summary>
            /// <param name="state"></param>
            private void TimeOutCallback(object state)
            {
                StopReceiveBroadcast();
                OnTimeOut?.Invoke();
            }

            /// <summary>
            /// 关闭广播接收端
            /// </summary>
            public void CloseBroadcastReceivingEnd()
            {
                StopReceiveBroadcast();
                receivingEndSocket = null;
                receivingEndPoint = null;
                if (receivingSAEA != null)
                {
                    receivingSAEA.Completed -= IO_Completed;
                    SAEAPool.RecycleSocketAsyncEventArgs(receivingSAEA);
                    receivingSAEA = null;
                }

                isReceivingEnd = false;

            }



            private void IO_Completed(object sender, SocketAsyncEventArgs e)
            {
                //根据刚刚完成的异步操作确定调用哪个回调函数
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.SendTo:
                        OnSentBroadcast(e);
                        break;
                    case SocketAsyncOperation.ReceiveFrom:
                        OnReceivedBroadcast(e);
                        break;
                    default:
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }

        }
    }
}