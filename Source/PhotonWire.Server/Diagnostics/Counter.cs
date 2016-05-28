using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Photon.SocketServer;

namespace PhotonWire.Server.Diagnostics
{
    public class Counter
    {
        public PhotonWireStats PhotonWireStats { get; }
        public ApplicationBaseStats[] ApplicationBaseStats { get; }
        public ThreadPoolStats ThreadPoolStats { get; }

        private Counter()
        {
            this.PhotonWireStats = new PhotonWireStats(PhotonWireEngine.Instance);
            this.ApplicationBaseStats = PhotonWireEngine.Instance.GetApplicationBaseStats();
            this.ThreadPoolStats = ThreadPoolStats.Capture();
        }

        public static Counter GetCounter()
        {
            return new Counter();
        }

        public static KeyValuePair<string, string[]>[] GetRegisteredHubInfo()
        {
            return PhotonWireEngine.Instance.GetRegisteredHubInfo().ToArray();
        }
    }

    public class PhotonWireStats
    {
        public int ClientConnectionCount { get; }
        public int InboundServerConnectionCount { get; }
        public int OutboundServerConnectionCount { get; }

        public int CurrentRequestCounnt { get; }
        public int TotalRequestCount { get; }

        internal PhotonWireStats(PhotonWireEngine engine)
        {
            ClientConnectionCount = PeerManager.ClientConnections.GetAll().Count;
            InboundServerConnectionCount = PeerManager.InboundServerConnections.GetAll().Count;
            OutboundServerConnectionCount = PeerManager.OutboundServerConnections.GetAll().Count;

            // Engine Stats
            CurrentRequestCounnt = engine.RunningCount;
            TotalRequestCount = engine.TotalCompleteCount;
        }
    }

    public class ApplicationBaseStats
    {
        public string ApplicationName { get; }
        public int PeerCount { get; }
        public string PhotonInstanceName { get; }

        internal ApplicationBaseStats(ApplicationBase app)
        {
            this.ApplicationName = app.ApplicationName;
            this.PeerCount = app.PeerCount;
            this.PhotonInstanceName = app.PhotonInstanceName;
        }
    }

    public class ThreadPoolStats
    {
        public int MaxIoThreads { get; }
        public int MinIoThreads { get; }
        public int FreeIoThreads { get; }
        public int BusyIoThreads { get; }
        public int MaxWorkerThreads { get; }
        public int MinWorkerThreads { get; }
        public int FreeWorkerThreads { get; }
        public int BusyWorkerThreads { get; }

        ThreadPoolStats()
        {
            int maxIoThreads, maxWorkerThreads;
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxIoThreads);

            int freeIoThreads, freeWorkerThreads;
            ThreadPool.GetAvailableThreads(out freeWorkerThreads, out freeIoThreads);

            int minIoThreads, minWorkerThreads;
            ThreadPool.GetMinThreads(out minWorkerThreads, out minIoThreads);

            int busyIoThreads = maxIoThreads - freeIoThreads;
            int busyWorkerThreads = maxWorkerThreads - freeWorkerThreads;

            this.MaxIoThreads = maxIoThreads;
            this.MinIoThreads = minIoThreads;
            this.FreeIoThreads = freeIoThreads;
            this.BusyIoThreads = busyIoThreads;
            this.MaxWorkerThreads = maxWorkerThreads;
            this.MinWorkerThreads = minWorkerThreads;
            this.FreeWorkerThreads = freeWorkerThreads;
            this.BusyWorkerThreads = busyWorkerThreads;
        }

        public static ThreadPoolStats Capture()
        {
            return new ThreadPoolStats();
        }
    }
}
