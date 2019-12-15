using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;

namespace SAEANetworking
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public enum SAEAMessageType
    {
        SystemMessage,  //系统内部消息
        CustomMessage,  //自定义消息
    }

    /// <summary>
    /// 系统内部协议
    /// </summary>
    public enum SystemProtocol
    {
        AllotClientID,  //分配客户端ID
    }

    /// <summary>
    /// 处理Socket信息的管理类
    /// </summary>
    public static class SAEASocketManager
    {

        /// <summary>
        /// 消息类型起始位置标记
        /// </summary>
        public const int messageTypeStartMarker = 0;

        /// <summary>
        /// 协议类型起始位置标记
        /// </summary>
        public const int protocolStartMarker = 2;

        /// <summary>
        /// 消息内容起始位置标记
        /// </summary>
        public const int messageStartMarker = 4;



        /// <summary>
        /// 异步IO操作完成回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void AsyncIO_Completed(object sender, SocketAsyncEventArgs e)
        {
            //根据刚刚完成的异步操作确定调用哪个回调函数
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    OnReceiveCompleted(e);
                    break;
                case SocketAsyncOperation.Send:
                    OnSendCompleted(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        /// <summary>
        /// 指定的客户端开启接收服务端消息
        /// </summary>
        /// <param name="clientData"></param>
        public static void ReceiveNetMessage(SAEAClientData clientData)
        {
            clientData.isAsyncReceiveCompleted = clientData.clientSocket.ReceiveAsync(clientData.ReceiveSocketAsyncEventArgs);
            if (!clientData.isAsyncReceiveCompleted)
            {
                OnReceiveCompleted(clientData.ReceiveSocketAsyncEventArgs);
            }
        }

        /// <summary>
        /// 完成接收数据
        /// </summary>
        /// <param name="e"></param>
        public static void OnReceiveCompleted(SocketAsyncEventArgs e)
        {
            try
            {
                SAEAClientData clientData = (SAEAClientData)e.UserToken;
                if (e.SocketError == SocketError.OperationAborted) return;
                //如果接收来的数据大于0
                if (e.BytesTransferred > 0)
                {

                    byte[] receiveBuffer = new byte[e.BytesTransferred];
                    System.Buffer.BlockCopy(e.Buffer, 0, receiveBuffer, 0, e.BytesTransferred);

                    //如果上一条有未能解析的数据，则和当前数据合并
                    if (clientData.residueByte.Length > 0)
                    {
                        receiveBuffer = SAEAMessageTools.ConcatByte(clientData.residueByte, receiveBuffer);
                        clientData.residueByte = new byte[0];
                    }

                    //拆分处理数据
                    while (clientData.isConnected)
                    {
                        //如果当前数据长度小于4，表示连获取本条消息内容的长度基本操作都无法完成
                        //就将当前数据继续放到存储区中，等待后续消息发来进行合并再进行此步骤验证
                        if (receiveBuffer.Length < 4)
                        {
                            clientData.residueByte = receiveBuffer;
                            break;
                        }

                        clientData.residueCount = SAEAMessageTools.DeserializeByteToInt(receiveBuffer, 0);
                        //如果当前收到的数组长度等于本条消息的长度
                        //表示此条消息正常接收到，直接处理并跳出循环
                        if (clientData.residueCount == receiveBuffer.Length - 4)
                        {
                            if (clientData.OnReceiveNetworkMessage != null)
                            {
                                clientData.OnReceiveNetworkMessage(clientData, SAEAMessageTools.CutByte(receiveBuffer, 4, clientData.residueCount));
                            }
                            break;
                        }

                        //如果解析出来一条消息的内容长度小于本条消息数组的长度
                        //则表示出现了粘包，进行循环拆分并处理
                        else if (clientData.residueCount < receiveBuffer.Length - 4)
                        {
                            if (clientData.OnReceiveNetworkMessage != null)
                            {
                                clientData.OnReceiveNetworkMessage(clientData, SAEAMessageTools.CutByte(receiveBuffer, 4, clientData.residueCount));
                                receiveBuffer = SAEAMessageTools.CutByte(receiveBuffer, clientData.residueCount + 4, receiveBuffer.Length - (clientData.residueCount + 4));
                                continue;
                            }
                            break;
                        }

                        //如果当前收到的消息小于本条消息内容的长度
                        //表示出现半包或消息是分包发送的，储存并等待后续合并
                        else if (clientData.residueCount > receiveBuffer.Length - 4)
                        {
                            clientData.residueByte = receiveBuffer;
                            break;
                        }
                    }
                    clientData.clientSocket.ReceiveAsync(e);
                }
                //如果收到了关闭连接的消息，根据当前接收到的客户端所属执行断开操作
                else
                {
                    switch (clientData.ClientType)
                    {
                        case ClientType.LocalClient:
                            if (ClientAction.OnServerToClientDisconnected != null)
                            {
                                ClientAction.OnServerToClientDisconnected();
                            }
                            break;
                        case ClientType.ServerClient:
                            if (ServerAction.OnClientToServerDisconnected != null)
                            {
                                ServerAction.OnClientToServerDisconnected(clientData);
                            }
                            break;
                    }
                }
            }
            catch (Exception)
            {
                //C_DebugManager.Instance.PrintDebugInfo(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// 向指定客户端发送一条消息
        /// </summary>
        /// <param name="targetClient"></param>
        /// <param name="message"></param>
        public static void SendNetMessage(SAEAClientData clientData, int buffer, byte[] message)
        {
            SAEAMessageTools.SplitByteToContainer(SAEAMessageTools.AddLengthMarkerForMassage(message), buffer, clientData.MessageQueue);
            if (clientData.isQueueSendCompleted)
            {
                SendNetMessageForQueue(clientData);
            }
        }

        /// <summary>
        /// 从客户端对象的消息队列中获取一条消息并发送
        /// </summary>
        /// <param name="clientData"></param>
        public static void SendNetMessageForQueue(SAEAClientData clientData)
        {
            clientData.isQueueSendCompleted = false;
            ////获取但不移除队列中第一条消息
            byte[] curMessage = clientData.GetMessage(true);

            if (curMessage == null) return;

            //如果当前起始发送位置为0则表示之前此条消息完整发送,设置发送长度为当前完整长度
            if (clientData.startIndex == 0)
            {
                clientData.residueLength = curMessage.Length;
            }

            clientData.SendSocketAsyncEventArgs.SetBuffer(curMessage, clientData.startIndex, clientData.residueLength);

            clientData.isAsyncSendCompleted = clientData.clientSocket.SendAsync(clientData.SendSocketAsyncEventArgs);
            if (!clientData.isAsyncSendCompleted)
            {
                OnSendCompleted(clientData.SendSocketAsyncEventArgs);
            }
        }

        /// <summary>
        /// 完成发送数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnSendCompleted(SocketAsyncEventArgs e)
        {
            SAEAClientData clientData = (SAEAClientData)e.UserToken;
            if (e.SocketError != SocketError.Success) return;
            e.SetBuffer(null, 0, 0);

            //如果已发送长度等于剩余长度则表示此条消息已经完整发送
            if (e.BytesTransferred == clientData.residueLength)
            {
                clientData.GetMessage();
                clientData.startIndex = 0;
            }
            else
            {
                clientData.startIndex = e.BytesTransferred;
                clientData.residueLength = clientData.residueLength - e.BytesTransferred;
                if (clientData.residueLength < 0)
                {
                    Console.WriteLine("发送长度异常");
                    clientData.residueLength = 0;
                }
            }

            if (clientData.MessageCount == 0)
            {
                clientData.isQueueSendCompleted = true;
                return;
            }

            SendNetMessageForQueue(clientData);
        }

        /// <summary>
        /// 关闭Socket连接
        /// </summary>
        /// <param name="socket"></param>
        public static void CloseSocket(Socket socket)
        {
            if (socket == null) return;
            if (!socket.Connected) return;

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                socket.Close();
            }

        }

        /// <summary>
        /// 获取本机HostName
        /// </summary>
        /// <returns></returns>
        public static string GetHostName()
        {
            string hostname = Dns.GetHostName();
            return hostname;
        }

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        /// <returns></returns>
        public static string GetAddress()
        {
            string Address;
            IPHostEntry ip = Dns.GetHostEntry(GetHostName());
            for (int i = 0; i < ip.AddressList.Length; i++)
            {
                if (ip.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    Address = ip.AddressList[i].ToString();
                    return Address;
                }
            }
            return null;
        }



        /// <summary>
        /// 服务器网络运行相关回调类
        /// </summary>
        public static class ServerAction
        {
            /// <summary>
            /// 当服务器启动时的回调
            /// </summary>
            public static Action OnServerStart;

            /// <summary>
            /// 当服务端检测到有客户端断开时的事件
            /// </summary>
            public static Action<SAEAClientData> OnClientToServerDisconnected;

            /// <summary>
            /// 当服务端检测到有客户端连接时的事件
            /// </summary>
            public static Action<SAEAClientData> OnServerAccepted;

            /// <summary>
            /// 当服务端接收到消息
            /// </summary>
            public static Action<SAEAClientData, byte[]> OnServerReceiveMessage;

            /// <summary>
            /// 当服务器关闭时的回调
            /// </summary>
            public static Action OnServerClosed;
        }

        /// <summary>
        /// 客户端网络运行相关回调类
        /// </summary>
        public static class ClientAction
        {
            /// <summary>
            /// 当检测到与服务器断开连接时的回调
            /// </summary>
            public static Action OnServerToClientDisconnected;

            /// <summary>
            /// 当连接到服务器时的回调
            /// </summary>
            public static Action OnClientConnected;

            /// <summary>
            /// 当客户端被关闭时的回调
            /// </summary>
            public static Action OnClientClosed;

            /// <summary>
            /// 当客户端接收到消息
            /// </summary>
            public static Action<SAEAClientData, byte[]> OnClientReceiveMessage;
        }

    }

}