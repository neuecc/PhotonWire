#if UNITY_EDITOR

using System;
using System.Linq;
using System.Collections.Generic;
using PhotonWire.Client;
using UnityEditor;
using UnityEngine;
using UniRx;
using System.Text;

namespace PhotonWire.Editor
{
    public class PhotonWireWindow : EditorWindow
    {
        [MenuItem("Window/PhotonWire")]
        public static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<PhotonWireWindow>("PhotonWire");
            // window.autoRepaintOnSceneChange = true;
            window.Show();
        }

        static int lastWidth = 500;
        Vector2 scrollPosition = Vector2.zero;
        int width;
        int height;
        readonly GUIStyle sentStyle;
        readonly GUIStyle receivedStyle;
        readonly GUIStyle indentStyle;

        long updateCallCount = 0;

        public PhotonWireWindow()
        {
            var red = new GUIStyle();
            red.normal = new GUIStyleState { textColor = Color.red };
            this.sentStyle = red;

            var green = new GUIStyle();
            green.normal = new GUIStyleState { textColor = Color.green };
            this.receivedStyle = green;

            this.indentStyle = new GUIStyle();
            indentStyle.margin = new RectOffset((int)(2 * 15f), 0, 0, 0);
        }


        static List<ConnectionInfoViewModel> connectionInfos = new List<ConnectionInfoViewModel>();

        public static void AddConnection(ObservablePhotonPeer peer)
        {
            connectionInfos.Add(new ConnectionInfoViewModel(peer)
            {
                FoldOut = true
            });
        }

        void Update()
        {
            if (!EditorApplication.isPlaying)
            {
                if (connectionInfos.Count == 0) return;
                foreach (var item in connectionInfos)
                {
                    item.Dispose();
                }
                connectionInfos.Clear();
                return;
            }

            if (updateCallCount++ % 10 == 0)
            {
                if (connectionInfos.Count == 0) return;

                foreach (var item in connectionInfos)
                {
                    DrawGraph(item, width, height);
                }
                Repaint();
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Activated Connections(" + connectionInfos.Count + ")", EditorStyles.boldLabel);

                foreach (var item in connectionInfos.ToArray())
                {
                    if (item.IsRemoved) connectionInfos.Remove(item);

                    if (item.FoldOut = EditorGUILayout.Foldout(item.FoldOut, item.ConnectionName))
                    {
                        EditorGUI.indentLevel += 1;
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.PrefixLabel("Address");
                                EditorGUILayout.LabelField(item.ServerAddress ?? "");
                            }
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.PrefixLabel("Application Name");
                                EditorGUILayout.LabelField(item.ApplicationName ?? "");
                            }
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.PrefixLabel("Connection State");
                                EditorGUILayout.LabelField(item.PeerState.ToString());
                            }

                            // Graph
                            if (EditorApplication.isPlaying)
                            {
                                EditorGUILayout.Space();

                                width = lastWidth = (int)(EditorGUIUtility.currentViewWidth - 25 - 30); // 25 = scroll size, 30 = indent size
                                height = 100;
                                EditorGUILayout.LabelField(item.MaxSize + "(Max Sent + Received in Graph)");
                                GUILayout.Box(item.GraphTexture, indentStyle, GUILayout.Width(width), GUILayout.Height(height));

                                EditorGUILayout.LabelField(string.Format("Total Sent:{0}, Max Sent in Graph:{1}", ToHumanReadableSize(item.TotalSent), item.MaxSent), sentStyle);
                                EditorGUILayout.LabelField(string.Format("Total Received:{0}, Max Received in Graph:{1}", ToHumanReadableSize(item.TotalReceived), item.MaxReceived), receivedStyle);
                            }
                        }
                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawGraph(ConnectionInfoViewModel info, int width, int height)
        {
            var buffer = info.GraphicBuffer;
            var bufferSize = width * height;
            if (buffer == null || buffer.Length != bufferSize)
            {
                // new buffer
                info.GraphTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
                info.GraphicBuffer = buffer = new Color32[width * height]; // x * y;
            }

            lock (info.GraphListLock)
            {
                var graphList = info.GraphList;
                if (graphList.Capacity < width)
                {
                    graphList.Capacity += width;
                }

                var bufferStart = (width < graphList.Count)
                    ? graphList.Count - width
                    : 0;

                var maxValue = 0;
                var maxReceived = 0;
                var maxSent = 0;
                for (int i = bufferStart; i < graphList.Count; i++)
                {
                    var tuple = graphList[i];
                    var max = tuple.Item1 + tuple.Item2;
                    if (maxValue < max) maxValue = max;
                    if (maxSent < tuple.Item1) maxSent = tuple.Item1;
                    if (maxReceived < tuple.Item2) maxReceived = tuple.Item2;
                }

                // side effect:)
                info.MaxSize = ToHumanReadableSize(maxValue);
                info.MaxReceived = ToHumanReadableSize(maxReceived);
                info.MaxSent = ToHumanReadableSize(maxSent);

                var basis = (maxValue != 0)
                    ? (double)height / (double)maxValue
                    : 0;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        var index = y * width + x;

                        var color = Color.clear;
                        if (x < graphList.Count)
                        {
                            var size = graphList[bufferStart + x];
                            var sent = ConvertToHeight(size.Item1, basis);
                            var received = ConvertToHeight(size.Item2, basis);
                            var max = sent + received;
                            if (y < max)
                            {
                                if (y < received)
                                {
                                    color = Color.green;
                                }
                                else
                                {
                                    color = Color.red;
                                }
                            }
                            else
                            {
                                color = Color.clear;
                            }
                        }

                        buffer[index] = color;
                    }
                }
            }

            if (buffer.Length != 0)
            {
                info.GraphTexture.SetPixels32(buffer);
                info.GraphTexture.Apply();
            }
        }

        int ConvertToHeight(int value, double basis)
        {
            return (int)(value * basis);
        }

        static string ToHumanReadableSize(double bytes)
        {
            if (bytes <= 1024) return bytes.ToString("f2") + " B";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " KB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " MB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " GB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " TB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " PB";

            bytes = bytes / 1024;
            if (bytes <= 1024) return bytes.ToString("f2") + " EB";

            bytes = bytes / 1024;
            return bytes + " ZB";
        }


        class ConnectionInfoViewModel : IDisposable
        {
            WeakReference Peer;
            IDisposable subscription;

            // does not create memory on every update.
            public Texture2D GraphTexture { get; set; }
            public Color32[] GraphicBuffer { get; set; }

            // send, received
            public CircularBuffer<UniRx.Tuple<int, int>> GraphList { get; set; }
            internal readonly object GraphListLock = new object(); // dangerous.

            public bool FoldOut { get; set; }
            public long TotalSent { get; set; }
            public long TotalReceived { get; set; }
            public string MaxSize { get; set; }
            public string MaxSent { get; set; }
            public string MaxReceived { get; set; }

            public ConnectionInfoViewModel(ObservablePhotonPeer peer)
            {
                this.MaxSize = "0B";
                this.MaxSent = "0B";
                this.MaxReceived = "0B";
                this.Peer = new WeakReference(peer);
                this.GraphList = new CircularBuffer<UniRx.Tuple<int, int>>(lastWidth);

                var interval = TimeSpan.FromMilliseconds(100);

                var send = peer.ObserveSendOpCustom().Select(x => GetSize(x)).Buffer(interval, Scheduler.ThreadPool);
                var sendReceive = peer.ObserveOperationResponse().Select(x => GetSize(x.OperationResponse.Parameters)).Buffer(interval, Scheduler.ThreadPool);
                var receive = peer.ObserveReceiveEventData().Select(x => GetSize(x.Parameters)).Buffer(interval, Scheduler.ThreadPool);

                subscription = Observable.Zip(send, sendReceive, receive, (x, y, z) => UniRx.Tuple.Create(x.Sum(), y.Sum() + z.Sum()))
                    .Subscribe(x =>
                    {
                        lock (GraphListLock)
                        {
                            TotalSent += x.Item1;
                            TotalReceived += x.Item2;
                            GraphList.Enqueue(x);
                        }
                    });
            }

            static int GetSize(Dictionary<byte, object> parameters)
            {
                var size = 0;
                foreach (var item in parameters.Values)
                {
                    if (item == null) continue;
                    var type = item.GetType();

                    if (type == typeof(int[]))
                    {
                        size += ((int[])item).Length * sizeof(int);
                        continue;
                    }
                    if (type == typeof(byte[]))
                    {
                        size = ((byte[])item).Length;
                        continue;
                    }

                    var code = Type.GetTypeCode(type);
                    switch (code)
                    {
                        case TypeCode.Byte:
                            size += sizeof(byte);
                            break;
                        case TypeCode.Boolean:
                            size += sizeof(bool);
                            break;
                        case TypeCode.Int16:
                            size += sizeof(short);
                            break;
                        case TypeCode.Int32:
                            size += sizeof(int);
                            break;
                        case TypeCode.Int64:
                            size += sizeof(long);
                            break;
                        case TypeCode.Single:
                            size += sizeof(float);
                            break;
                        case TypeCode.Double:
                            size += sizeof(double);
                            break;
                        case TypeCode.String:
                            size += Encoding.UTF8.GetByteCount((string)item);
                            break;
                        default:
                            break;
                    }
                }
                return size;
            }

            public void AddNetworkData(int sent, int received)
            {
                GraphList.Enqueue(UniRx.Tuple.Create(sent, received));
            }

            public void Dispose()
            {
                if (subscription != null)
                {
                    subscription.Dispose();
                }
            }

            public ObservablePhotonPeer ObservablePhotonPeer
            {
                get
                {
                    var peer = Peer;
                    if (peer.IsAlive)
                    {
                        var t = peer.Target;
                        if (t != null)
                        {
                            return (ObservablePhotonPeer)t;
                        }
                    }

                    return null;
                }
            }

            public bool IsRemoved
            {
                get
                {
                    var peer = ObservablePhotonPeer;
                    return (peer == null) || peer.IsDisposed;
                }
            }

            public string ServerAddress
            {
                get
                {
                    var peer = ObservablePhotonPeer;
                    if (peer != null)
                    {
                        return peer.LastConnectServerAddress;
                    }
                    return null;
                }
            }

            public string ApplicationName
            {
                get
                {
                    var peer = ObservablePhotonPeer;
                    if (peer != null)
                    {
                        return peer.LastConnectApplicationName;
                    }
                    return null;
                }
            }

            public string ConnectionName
            {
                get
                {
                    var peer = ObservablePhotonPeer;
                    if (peer != null)
                    {
                        return (peer.PeerName != null) ? peer.PeerName
                           : (peer.LastConnectServerAddress == null) ? "(Not connected yet)"
                           : peer.LastConnectServerAddress + " " + peer.LastConnectApplicationName;
                    }
                    return null;
                }
            }

            public ExitGames.Client.Photon.PeerStateValue PeerState
            {
                get
                {
                    var peer = ObservablePhotonPeer;
                    if (peer != null)
                    {
                        return peer.PeerState;
                    }

                    return ExitGames.Client.Photon.PeerStateValue.Disconnected;
                }
            }
        }
    }
}


#endif