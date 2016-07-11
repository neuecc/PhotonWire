using UnityEngine;
using PhotonWire.Client;
using System.Collections;
using System.Threading;
using System;
using UniRx;

public class Main : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        UnityEngine.Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;

        var timer = new Timer(_ =>
        {
            UnityEngine.Debug.Log("go exception");
            throw new System.Exception("sinuyo");
        }, null, 5000, Timeout.Infinite);
    }

    private void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        UnityEngine.Debug.Log("Threaded message" + condition + stackTrace + type);
    }

    // Update is called once per frame
    void Update()
    {
        var peer = new ObservablePhotonPeer(ExitGames.Client.Photon.ConnectionProtocol.Tcp, "Test", 20);

        var proxy = peer.CreateTypedHub<SimpleHubProxy>();
            
        
        

        proxy.AttachInvokeFilter(x => new MogeMoge1(x));
        proxy.AttachInvokeFilter(x => new MogeMoge2(x));
        proxy.AttachReceiveFilter(x => new NugaNuga1(x));

        proxy.Publish.ToClient(100, 2000);

        proxy.Receive.ToClient().Subscribe(_ => { });




    }
}

public class NugaNuga1 : SimpleHubProxy.DelegatingSimpleHubClientReceiver
{
    public NugaNuga1(SimpleHubProxy.ISimpleHubClientReceiver parent) : base(parent)
    {
    }

    public override IObservable<SimpleHubProxy.SimpleHubClientToClientResponse> ToClient(bool observeOnMainThread)
    {
        return base.ToClient(observeOnMainThread);
    }
}

public class MogeMoge1 : SimpleHubProxy.DelegatingSimpleHubServerInvoker
{
    public MogeMoge1(SimpleHubProxy.ISimpleHubServerInvoker parent) : base(parent)
    {
    }

    public override IObservable<string> HogeAsync(int x, bool observeOnMainThread, bool encrypt)
    {
        return base.HogeAsync(x, observeOnMainThread, encrypt);
    }
}

public class MogeMoge2 : SimpleHubProxy.DelegatingSimpleHubServerInvoker
{
    public MogeMoge2(SimpleHubProxy.ISimpleHubServerInvoker parent) : base(parent)
    {
    }
    public override IObservable<string> HogeAsync(int x, bool observeOnMainThread, bool encrypt)
    {
        return base.HogeAsync(x, observeOnMainThread, encrypt);
    }
}