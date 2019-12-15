using System.Net.Sockets;
using System.Net;
using System;

namespace SAEANetworking
{

    /// <summary>
    /// SAEASocket对象基类，提供了创建和设置SAEASocket对象的基本方法
    /// </summary>
    public abstract class SAEASocketBase
    {
        public SAEASocketBase() { }

        public SAEASocketBase(string serverIP, int serverPort, int receiveBufferLength, int sendBufferLength)
        {
            ServerIP = serverIP;
            ServerPort = serverPort;
            ReceiveBufferLength = receiveBufferLength;
            SendBufferLength = sendBufferLength;
        }

        /// <summary>
        /// 服务器IP地址
        /// </summary>
        public string ServerIP { get; protected set; }

        /// <summary>
        /// 服务器端口号
        /// </summary>
        public int ServerPort { get; protected set; }

        /// <summary>
        /// 接收消息缓冲区长度
        /// </summary>
        public int ReceiveBufferLength { get; protected set; }

        /// <summary>
        /// 发送缓冲区长度
        /// </summary>
        public int SendBufferLength { get; protected set; }



        /// <summary>
        /// 设置接收缓冲区
        /// </summary>
        /// <param name="receiveBuffer"></param>
        public void SetReceiveBuffer(int receiveBuffer)
        {
            ReceiveBufferLength = receiveBuffer;
        }

        /// <summary>
        /// 设置发送缓冲区
        /// </summary>
        /// <param name="sendBuffer"></param>
        public void SetSendBuffer(int sendBuffer)
        {
            SendBufferLength = sendBuffer;
        }

        /// <summary>
        /// 设置服务器IP地址
        /// </summary>
        /// <param name="serverIP"></param>
        public virtual void SetServerIP(string serverIP)
        {
            ServerIP = serverIP;
        }

        /// <summary>
        /// 设置服务器端口号
        /// </summary>
        /// <param name="serverPort"></param>
        public virtual void SetServerPort(int serverPort)
        {
            ServerPort = serverPort;
        }

        /// <summary>
        /// 关闭SAEA对象
        /// </summary>
        public abstract void SAEAClose();

        /// <summary>
        /// 系统内部命令处理
        /// </summary>
        protected abstract void SystemMessageDispose(SAEAClientData clientData, byte[] message);

        /// <summary>
        /// 解析消息类
        /// </summary>
        /// <param name="clientData"></param>
        /// <param name="message"></param>
        public abstract void ParseMessageProtocol(SAEAClientData clientData, byte[] message);

    }

}