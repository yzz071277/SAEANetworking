using System.Net.Sockets;
using System.Net;
using System;

namespace SAEANetworking
{

    /// <summary>
    /// 客户端对象类，用于作为客户端的创建和使用
    /// </summary>
    public class SAEAClient : SAEASocketBase
    {

        public SAEAClient() : base() { }

        public SAEAClient(string serverIP, int serverPort, int receiveBufferLength, int sendBufferLength) : base(serverIP, serverPort, receiveBufferLength, sendBufferLength) { }

        /// <summary>
        /// 本地客户端对象
        /// </summary>
        public SAEAClientData ClientData { get; private set; }

        public Socket serverAcceptSocket;



        /// <summary>
        /// 客户端连接服务器
        /// </summary>
        public void ClientConnect()
        {
            IPAddress ip = IPAddress.Parse(ServerIP);
            IPEndPoint point = new IPEndPoint(ip, ServerPort);

            SocketAsyncEventArgs connectAsyncEventArgs = SAEAPool.GetSocketAsyncEventArgs();
            connectAsyncEventArgs.Completed += OnAsyncConnectedCompleted;
            connectAsyncEventArgs.RemoteEndPoint = point;

            //创建本地客户端对象
            ClientData = new SAEAClientData(0, ClientType.LocalClient, new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), ReceiveBufferLength, SAEASocketManager.AsyncIO_Completed);

            ClientData.OnReceiveNetworkMessage += ParseMessageProtocol;

            ClientData.isConnected = true;
            SAEASocketManager.ClientAction.OnServerToClientDisconnected += SAEAClose;

            //连接服务器
            ClientData.clientSocket.ConnectAsync(connectAsyncEventArgs);
        }

        /// <summary>
        /// 异步完成连接请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAsyncConnectedCompleted(object sender, SocketAsyncEventArgs e)
        {


            if (SAEASocketManager.ClientAction.OnClientConnected != null)
            {
                SAEASocketManager.ClientAction.OnClientConnected();
            }
            SAEASocketManager.ReceiveNetMessage(ClientData);
        }

        /// <summary>
        /// 向服务器发送一条消息
        /// </summary>
        /// <param name="targetClient"></param>
        /// <param name="message"></param>
        public void SendMessageToServer(byte[] message)
        {
            byte[] messageTypeByte = SAEAMessageTools.SerializeShortToByte((short)SAEAMessageType.CustomMessage);
            message = SAEAMessageTools.ConcatByte(messageTypeByte, message);
            SAEASocketManager.SendNetMessage(ClientData, SendBufferLength, message);
        }

        /// <summary>
        /// 断开与服务器的连接
        /// </summary>
        public override void SAEAClose()
        {
            if (ClientData != null)
            {
                ClientData.isConnected = false;
                ClientData.DestoryThisClient();
            }

            SAEASocketManager.CloseSocket(ClientData.clientSocket);
            SAEASocketManager.CloseSocket(serverAcceptSocket);

            if (SAEASocketManager.ClientAction.OnClientClosed != null)
            {
                SAEASocketManager.ClientAction.OnClientClosed();
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
                if (SAEASocketManager.ClientAction.OnClientReceiveMessage != null)
                {
                    SAEASocketManager.ClientAction.OnClientReceiveMessage(clientData, message);
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
            Console.WriteLine("接收到服务器系统消息");
            SystemProtocol systemProtocol = (SystemProtocol)SAEAMessageTools.DeserializeByteToShort(message, 2);
            switch (systemProtocol)
            {
                case SystemProtocol.AllotClientID:
                    ClientData.clientID = SAEAMessageTools.DeserializeByteToUint(message, 2);
                    break;
                default:
                    throw new Exception("System protocol resolves exceptions");
            }
        }
    }
}


