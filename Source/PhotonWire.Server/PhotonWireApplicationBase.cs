using System;
using PhotonWire.Server.ServerToServer;
using Photon.SocketServer;
using System.Net;
using System.Reflection;
using PhotonWire.Server.Diagnostics;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace PhotonWire.Server
{
    /// <summary>
    /// Application Startup for PhotonWire.
    /// </summary>
    public abstract class PhotonWireApplicationBase : ApplicationBase
    {
        public static new PhotonWireApplicationBase Instance
        {
            get
            {
                return (PhotonWireApplicationBase)ApplicationBase.Instance;
            }
        }

        bool isStopRequested = false;
        Timer reconnectTimer = null;

        // Configuration

        protected virtual bool IsServerToServerPeer(InitRequest initRequest)
        {
            return false;
        }

        protected virtual Assembly[] HubTargetAssemlies
        {
            get
            {
                return AppDomain.CurrentDomain.GetAssemblies();
            }
        }

        protected virtual string[] HubTargetTags
        {
            get
            {
                return new string[0];
            }
        }

        /// <summary>
        /// If true,
        /// 1. return exception message return to client when operation failed.
        /// 2. broadcast JSON(dumpable) message when from PhotonWire.HubInvoker.
        /// 3. show Debug.Fail dialog if failed on initialization
        /// </summary>
        public virtual bool IsDebugMode
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// If use graceful-restart(EnableShadowCopy/EnableAutoRestart), AppDomain keep alived.
        /// This timestpan force disconnect for start to TearDown.
        /// </summary>
        public virtual TimeSpan? DisconnectAllPeersAfterOnRequestStopped
        {
            get
            {
                return null;
            }
        }

        public virtual IPhotonSerializer Serializer
        {
            get
            {
                return PhotonSerializers.MsgPack;
            }
        }

        public virtual IPhotonWireLogger Logger
        {
            get
            {
                return PhotonWireEventSource.Log;
            }
        }

        protected Task<PhotonWireOutboundS2SPeer> ConnectToOutboundServerAsync(IPEndPoint ipEndPoint, string applicationName, string groupName = null, object customInitObject = null, bool useMux = false, long? reconnectIntervalMs = 1000, Action<PhotonWireOutboundS2SPeer> onReconnected = null)
        {
            return PhotonWireApplicationBase.ConnectToOutboundServerAsync(this, ipEndPoint, applicationName, groupName, customInitObject, useMux, reconnectIntervalMs, onReconnected);
        }

        public static async Task<PhotonWireOutboundS2SPeer> ConnectToOutboundServerAsync(PhotonWireApplicationBase applicationBase, IPEndPoint ipEndPoint, string applicationName, string groupName = null, object customInitObject = null, bool useMux = false, long? reconnectIntervalMs = 1000, Action<PhotonWireOutboundS2SPeer> onReconnected = null)
        {
            var outboundPeer = new PhotonWireOutboundS2SPeer(applicationBase);

            if (reconnectIntervalMs != null)
            {
                applicationBase.reconnectTimer = new Timer(async _ =>
                {
                    if (applicationBase.isStopRequested)
                    {
                        // disable timer
                        applicationBase.reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    try
                    {
                        if (outboundPeer.ConnectionState == ConnectionState.Disconnected)
                        {
                            await ReconnectAsync(applicationBase, ipEndPoint, applicationName, groupName, onReconnected, outboundPeer, reconnectIntervalMs.Value, customInitObject, useMux).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        applicationBase.Logger.ConnectToOutboundReconnectTimerException(applicationBase.ApplicationName, ex.GetType().Name, ex.Message, ex.StackTrace);
                    }
                }, null, reconnectIntervalMs.Value, reconnectIntervalMs.Value);
            }

            var sw = Stopwatch.StartNew();
            if (await outboundPeer.ConnectTcpAsync(ipEndPoint, applicationName, customInitObject, useMux).ConfigureAwait(false))
            {
                sw.Stop();
                applicationBase.Logger.ConnectToOutboundServer(applicationBase.ApplicationName, ipEndPoint.ToString(), applicationName, sw.Elapsed.TotalMilliseconds);

                PeerManager.OutboundServerConnections.Add(outboundPeer);
                if (groupName != null)
                {
                    PeerManager.OutboundServerConnections.AddGroup(groupName, outboundPeer);
                }
            }
            else
            {
                sw.Stop();
                applicationBase.Logger.ConnectToOutboundServerFailed(applicationBase.ApplicationName, ipEndPoint.ToString(), applicationName, sw.Elapsed.TotalMilliseconds);
            }

            return outboundPeer;
        }

        private static async Task ReconnectAsync(PhotonWireApplicationBase applicationBase, IPEndPoint ipEndPoint, string applicationName, string groupName, Action<PhotonWireOutboundS2SPeer> onReconnected, PhotonWireOutboundS2SPeer outboundPeer, long reconnectInterval, object customInitObject, bool useMux)
        {
            var resw = Stopwatch.StartNew();
            if (await outboundPeer.ConnectTcpAsync(ipEndPoint, applicationName, customInitObject, useMux).ConfigureAwait(false))
            {
                resw.Stop();
                applicationBase.Logger.ReconnectToOutboundServer(applicationBase.ApplicationName, ipEndPoint.ToString(), applicationName, resw.Elapsed.TotalMilliseconds);

                PeerManager.OutboundServerConnections.Add(outboundPeer);
                if (groupName != null)
                {
                    PeerManager.OutboundServerConnections.AddGroup(groupName, outboundPeer);
                }

                if (onReconnected != null)
                {
                    onReconnected(outboundPeer);
                }
            }
            else
            {
                resw.Stop();
                applicationBase.Logger.ReconnectToOutboundServerFailed(applicationBase.ApplicationName, ipEndPoint.ToString(), applicationName, resw.Elapsed.TotalMilliseconds);
            }
        }

        protected sealed override PeerBase CreatePeer(InitRequest initRequest)
        {
            Logger.PeerReceived(ApplicationName, initRequest.ApplicationId, initRequest.ClientVersion?.ToString() ?? "", initRequest.ConnectionId, initRequest.RemoteIP, initRequest.RemotePort);

            if (IsServerToServerPeer(initRequest))
            {
                // Server-Server Connection
                var s2sPeer = new PhotonWireInboundS2SPeer(initRequest);
                PeerManager.InboundServerConnections.Add(s2sPeer);
                OnPeerCreated(s2sPeer, initRequest, true);
                return s2sPeer;
            }
            else
            {
                // Client-Server Connection
                var peer = new PhotonWireClientPeer(initRequest);

                // PhotonWire.HubInvoker use only Json, flag is embeded.
                if (initRequest.InitObject != null && initRequest.InitObject.ToString() == "UseJsonSerializer")
                {
                    peer.Items["PhotonWireApplicationBase.ModifySerializer"] = PhotonSerializers.Json;
                }

                PeerManager.ClientConnections.Add(peer);
                OnPeerCreated(peer, initRequest, false);
                return peer;
            }
        }

        protected sealed override void Setup()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                PhotonWireEngine.Initialize(HubTargetAssemlies, HubTargetTags, Serializer, IsDebugMode);
                sw.Stop();
                Logger.InitializeComplete(ApplicationName, sw.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                var aex = ex as AggregateException;
                if (aex != null) ex = aex.InnerException;

                Logger.SetupFailed(ApplicationName, ex.GetType().Name, ex.Message, ex.StackTrace);
                if (IsDebugMode)
                {
                    System.Diagnostics.Debug.Fail("PhotonWire Initialize Failed", ex.Message);
                }
                throw; // can't run.
            }
            PhotonWireEngine.Instance.RegisterApplication(this);
            SetupCore();
        }

        protected sealed override void TearDown()
        {
            var instance = PhotonWireEngine.Instance;
            if (instance != null)
            {
                PhotonWireEngine.Instance.UnregisterApplication(this);
            }

            Logger.TearDown(ApplicationName);
            TearDownCore();
        }

        Timer stopTimer = null;

        protected sealed override void OnStopRequested()
        {
            isStopRequested = true;
            Logger.OnStopRequested(ApplicationName);

            if (DisconnectAllPeersAfterOnRequestStopped != null)
            {
                var span = DisconnectAllPeersAfterOnRequestStopped.Value;
                stopTimer = new Timer(_ =>
                {
                    foreach (var item in PeerManager.ClientConnections.GetAll())
                    {
                        try
                        {
                            item.PeerBase.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.DisconnectPeerOnRequestStoppedException(ApplicationName, ex.GetType().Name, ex.Message, ex.StackTrace);
                        }
                    }
                    foreach (var item in PeerManager.InboundServerConnections.GetAll())
                    {
                        try
                        {
                            item.PeerBase.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.DisconnectPeerOnRequestStoppedException(ApplicationName, ex.GetType().Name, ex.Message, ex.StackTrace);
                        }
                    }
                    foreach (var item in PeerManager.OutboundServerConnections.GetAll())
                    {
                        try
                        {
                            item.PeerBase.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.DisconnectPeerOnRequestStoppedException(ApplicationName, ex.GetType().Name, ex.Message, ex.StackTrace);
                        }
                    }
                }, null, DisconnectAllPeersAfterOnRequestStopped.Value, TimeSpan.FromMinutes(1)); // After EveryMinutes.
            }

            OnStopRequestedCore();
        }

        protected virtual void SetupCore()
        {

        }

        protected virtual void TearDownCore()
        {

        }

        protected virtual void OnStopRequestedCore()
        {

        }

        protected virtual void OnPeerCreated(IPhotonWirePeer peer, InitRequest initRequest, bool isServerToServerPeer)
        {
        }
    }
}