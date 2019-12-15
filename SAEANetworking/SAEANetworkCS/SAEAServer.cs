using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;

namespace SAEANetworking
{
    /// <summary>
    /// 服务器对象类，用于作为服务器的创建和使用
    /// </summary>
    public class SAEAServer : SAEASocketBase
    {

        public SAEAServer() : base() { }

        public SAEAServer(string serverIP, int serverPort, int receiveBufferLength, int sendBufferLength, int maxClient) : base(serverIP, serverPort, receiveBufferLength, sendBufferLength)
        {
            SetMaxClients(maxClient);
        }

        object @object = new object();

        /// <summary>
        /// 用于接受客户端连接的Socket
        /// </summary>
        private Socket acceptSocket;

        /// <summary>
        /// 用于服务器端持续监听的SocketAsyncEventArgs对象
        /// </summary>
        private SocketAsyncEventArgs acceptAsyncEventArgs;

        /// <summary>
        /// 最大客户端连接数
        /// </summary>
        public int MaxClients { get; private set; }

        /// <summary>
        /// 已连接上的客户端池
        /// </summary>
        private Dictionary<Socket, SAEAClientData> clientPool = new Dictionary<Socket, SAEAClientData>();

        /// <summary>
        /// 当前已连接客户端数量
        /// </summary>
        public int ConnectedClientNum
        {
            get
            {
                if (clientPool == null) return 0;
                return clientPool.Count;
            }
        }

        /// <summary>
        /// 当前ID索引
        /// </summary>
        private uint curIdIndex = 1;

        /// <summary>
        /// 空闲ID缓存区
        /// </summary>
        private Queue<uint> leisureID = new Queue<uint>();

        /// <summary>
        /// 设置服务器端监听连接的最大数量
        /// </summary>
        /// <param name="num"></param>
        public void SetMaxClients(int num)
        {
            if (num < 0) num = 0;
            MaxClients = num;
        }

        /// <summary>
        /// 创建服务器
        /// </summary>
        public void CreateServer()
        {
            SAEASocketManager.ServerAction.OnClientToServerDisconnected += RemoveClientData;

            acceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Parse(ServerIP);
            IPEndPoint point = new IPEndPoint(ip, ServerPort);

            acceptSocket.Bind(point);
            acceptSocket.Listen(MaxClients);

            acceptAsyncEventArgs = SAEAPool.GetSocketAsyncEventArgs();
            acceptAsyncEventArgs.Completed += OnAsyncAcceptedCompleted;


            if (SAEASocketManager.ServerAction.OnServerStart != null)
            {
                SAEASocketManager.ServerAction.OnServerStart();
            }

            StartAccepted();
        }

        /// <summary>
        /// 开启接收客户端连接
        /// </summary>
        private void StartAccepted()
        {
            acceptAsyncEventArgs.AcceptSocket = null;
            bool isCompleted = acceptSocket.AcceptAsync(acceptAsyncEventArgs);
            if (!isCompleted)
            {
                OnAsyncAcceptedCompleted(this, acceptAsyncEventArgs);
            }
        }

        /// <summary>
        /// 异步检测到有客户端连接时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAsyncAcceptedCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;

            //创建服务器端客户端对象并分配数据
            SAEAClientData client = CreateConnectedClinet(e.AcceptSocket);
            client.isConnected = true;

            //发送客户端ID给新连接到的客户端
            SendClientID(client);

            //执行客户端连接回调
            if (SAEASocketManager.ServerAction.OnServerAccepted != null)
            {
                SAEASocketManager.ServerAction.OnServerAccepted(client);
            }

            //开始接收客户端数据
            SAEASocketManager.ReceiveNetMessage(client);
            //继续监听
            StartAccepted();
        }

        /// <summary>
        /// 向指定客户端发送一条消息
        /// </summary>
        /// <param name="targetClient"></param>
        /// <param name="message"></param>
        public void SendMessageToClient(SAEAClientData targetClient, byte[] message)
        {
            byte[] messageTypeByte = SAEAMessageTools.SerializeShortToByte((short)SAEAMessageType.CustomMessage);
            message = SAEAMessageTools.ConcatByte(messageTypeByte, message);
            SAEASocketManager.SendNetMessage(targetClient, SendBufferLength, message);
        }

        /// <summary>
        /// 向所有已连接客户端发送一条消息
        /// </summary>
        /// <param name="message"></param>
        public void SendMessageToAllClient(byte[] message)
        {
            byte[] messageTypeByte = SAEAMessageTools.SerializeShortToByte((short)SAEAMessageType.CustomMessage);
            message = SAEAMessageTools.ConcatByte(messageTypeByte, message);
            foreach (var item in clientPool.Keys)
            {
                SAEASocketManager.SendNetMessage(clientPool[item], SendBufferLength, message);
            }
        }

        /// <summary>
        /// 创建一个已连接客户端对象，分配参数并返回此对象
        /// </summary>
        private SAEAClientData CreateConnectedClinet(Socket clientSocket)
        {
            if (clientSocket == null) return null;
            SAEAClientData curConnectedClinet = new SAEAClientData(AllocationClientID(), ClientType.ServerClient, clientSocket, ReceiveBufferLength, SAEASocketManager.AsyncIO_Completed);

            curConnectedClinet.OnReceiveNetworkMessage += ParseMessageProtocol;

            clientPool.Add(clientSocket, curConnectedClinet);
            return curConnectedClinet;
        }

        /// <summary>
        /// 销毁并从客户端池中移除此客户端
        /// </summary>
        /// <param name="clientSocket"></param>
        public void RemoveClientData(SAEAClientData clientData)
        {
            if (clientData == null || clientData.clientSocket == null) return;
            if (!clientPool.ContainsKey(clientData.clientSocket)) return;
            clientData.isConnected = false;

            SAEASocketManager.CloseSocket(clientData.clientSocket);

            clientPool.Remove(clientData.clientSocket);
            leisureID.Enqueue(clientData.clientID);
            clientData.DestoryThisClient();
        }

        /// <summary>
        /// 为已连接客户端分配ID
        /// </summary>
        /// <returns></returns>
        public uint AllocationClientID()
        {
            lock (@object)
            {
                uint clientID;
                if (leisureID.Count <= 0)
                {
                    clientID = curIdIndex;
                    curIdIndex++;
                }
                else
                {
                    clientID = leisureID.Dequeue();
                }
                return clientID;
            }
        }

        /// <summary>
        /// 关闭服务器
        /// </summary>
        public override void SAEAClose()
        {
            List<Socket> tempCloseList = new List<Socket>();

            foreach (var item in clientPool.Keys)
            {
                clientPool[item].isConnected = false;
                tempCloseList.Add(clientPool[item].clientSocket);
            }

            for (int i = 0; i < tempCloseList.Count; i++)
            {
                SAEASocketManager.CloseSocket(tempCloseList[i]);
            }

            if (acceptSocket != null && acceptSocket.Connected)
            {
                acceptSocket.Shutdown(SocketShutdown.Both);
            }

            //执行服务器关闭的委托事件
            if (SAEASocketManager.ServerAction.OnServerClosed != null)
            {
                SAEASocketManager.ServerAction.OnServerClosed();
            }



        }

        /// <summary>
        /// 解析接收到的消息
        /// </summary>
        /// <param name="clientData"></param>
        /// <param name="message"></param>
        public override void ParseMessageProtocol(SAEAClientData clientData, byte[] message)
        {
            SAEAMessageType messageType = (SAEAMessageType)SAEAMessageTools.DeserializeByteToShort(message, 0);
            if (messageType == SAEAMessageType.SystemMessage)
            {
                SystemMessageDispose(clientData, message);
            }
            else
            {
                if (SAEASocketManager.ServerAction.OnServerReceiveMessage != null)
                {
                    SAEASocketManager.ServerAction.OnServerReceiveMessage(clientData, message);
                }
            }
        }

        /// <summary>
        /// 解析系统内部协议
        /// </summary>
        /// <param name="clientData"></param>
        /// <param name="message"></param>
        protected override void SystemMessageDispose(SAEAClientData clientData, byte[] message)
        {
            SystemProtocol systemProtocol = (SystemProtocol)SAEAMessageTools.DeserializeByteToShort(message, 2);
            switch (systemProtocol)
            {
                case SystemProtocol.AllotClientID:
                    //C_DebugManager.Instance.PrintDebugInfo("服务端: AllotClientID");
                    break;
                default:
                    throw new Exception("System protocol resolves exceptions");
            }
        }


        /// <summary>
        /// 发送客户端ID给当前连接的客户端
        /// </summary>
        /// <param name="ClientID"></param>
        private void SendClientID(SAEAClientData clientDate)
        {
            byte[] messageTypeByte = SAEAMessageTools.SerializeShortToByte((short)SAEAMessageType.SystemMessage);
            byte[] protocolType = SAEAMessageTools.SerializeShortToByte((short)SystemProtocol.AllotClientID);
            byte[] clientID = SAEAMessageTools.SerializeUintToByte(clientDate.clientID);
            byte[] message = SAEAMessageTools.ConcatByte(messageTypeByte, protocolType, clientID);
            SAEASocketManager.SendNetMessage(clientDate, SendBufferLength, message);
        }







    }


}

