using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;


/// <summary>
/// 基于Unity的网络发现类
/// </summary>
public class SocketNetworkDiscovery : MonoBehaviour
{
    private static SocketNetworkDiscovery Instance { get; set; }
    private SocketNetworkDiscovery() { }
    void Awake()
    {
        Instance = this;
    }


    public int netDisPort = 10010;
    public float broadcastInterval = 1f;
    public float listenTimeout = 4f;
    public bool isDiscovery = true;
    private bool isListen;

    Thread thread;


    SAEANetworkDiscovery networkDiscovery = new SAEANetworkDiscovery();


    private void Start()
    {
        if (isDiscovery)
        {
            networkDiscovery.OnReceiveBroadcast += OnReceiveBroadcast;
            networkDiscovery.OnTimeOut += OnTimeOut;
            networkDiscovery.CreateBroadcastReceivingEnd(netDisPort);
            networkDiscovery.StartReceiveBroadcast(listenTimeout);
            isListen = true;
        }
    }


    /// <summary>
    /// 当收听到广播时
    /// </summary>
    /// <param name="bytes"></param>
    private void OnReceiveBroadcast(byte[] bytes)
    {
        isListen = false;
        networkDiscovery.StopReceiveBroadcast();
        CreateClient(SAEAMessageTools.DeserializeByteToString(bytes, 0).Split('|')[1]);
    }

    /// <summary>
    /// 当超时时执行的事件
    /// </summary>
    private void OnTimeOut() {
        if (isListen)
        {
            CreateServer(SAEASocketManager.GetAddress());
            networkDiscovery.CreateBroadcastSendingEnd(netDisPort);
            networkDiscovery.StartBroadcast(broadcastInterval);
            isListen = false;
        }
    }

    /// <summary>
    /// 创建服务器方法
    /// </summary>
    private void CreateServer(string serverIP) {
        SocketNetworkManager.Instance.serverIP = serverIP;
        thread = new Thread(SocketNetworkManager.Instance.CreateServer);
        thread.Start();
    }

    /// <summary>
    /// 创建客户端方法
    /// </summary>
    private void CreateClient(string serverIP) {
        SocketNetworkManager.Instance.serverIP = serverIP;
        SocketNetworkManager.Instance.CreateClient();
    }



    private void OnDestroy()
    {
        networkDiscovery.CloseBroadcastReceivingEnd();
        networkDiscovery.CloseBroadcastSendingEnd();
        if (thread != null && thread.IsAlive)
        {
            thread.Abort();
        }
    }

    /// <summary>
    /// 停止发送广播
    /// </summary>
    public void StopSendBroadcast()
    {
        networkDiscovery.StopBroadcast();
    }

    /// <summary>
    /// 停止监听广播
    /// </summary>
    public void StopListenBroadcast()
    {
        networkDiscovery.StopReceiveBroadcast();
    }


}
