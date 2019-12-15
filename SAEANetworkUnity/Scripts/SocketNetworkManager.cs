using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

/// <summary>
/// 网络消息协议
/// </summary>
public enum NetworkMessageProtocol {
    AllotClientID, //分配客户端ID
    SyncWorldAnchor, //同步空间锚
}


public class SocketNetworkManager : MonoBehaviour
{
    public static SocketNetworkManager Instance { get; private set; }

    void Awake()
    {
        Instance = this; 
        DontDestroyOnLoad(gameObject);
    }

    public string serverIP = "127.0.0.1";
    public int serverPort = 10086;

    public int receiveBuffer = 1024;
    public int sendBuffer = 1024;

    public int maxClient = 4;

    public bool IsHost { get; private set; }
    public bool IsServer { get; private set; }
    public bool IsClient { get; private set; }

    public SAEAServer SocketServer { get; private set;}
    public SAEAClient SocketClient { get; private set; }

    /// <summary>
    /// 创建主机(Server + Client)
    /// </summary>
    /// <param name="serverIP"></param>
    /// <param name="ServerPort"></param>
    public void CreateHost() {
        CreateServer();
        CreateClient();
        IsServer = false;
        IsClient = false;
        IsHost = true;
    }

    /// <summary>
    /// 作为服务器创建
    /// </summary>
    /// <param name="serverIP"></param>
    /// <param name="serverPort"></param>
    /// <param name="acceptCallback"></param>
    public void CreateServer() {
        SocketServer = new SAEAServer(serverIP, serverPort, receiveBuffer, sendBuffer, maxClient);
        SocketServer.CreateServer();
        IsServer = true;
    }

    /// <summary>
    /// 作为客户端创建
    /// </summary>
    /// <param name="serverIP"></param>
    /// <param name="serverPort"></param>
    /// <param name="connectCallback"></param>
    public void CreateClient() {
        SocketClient = new SAEAClient(serverIP, serverPort, receiveBuffer, sendBuffer);
        SocketClient.ClientConnect(); 
        IsClient = true;
    }


    private void OnDestroy()
    {
        if (SocketClient != null)
        {
            SocketClient.SAEAClose();
        }
        if (SocketServer != null)
        {
            SocketServer.SAEAClose();
        }
    }



}
