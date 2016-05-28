using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using ExitGames.Client.Photon;
using Newtonsoft.Json;
using PhotonWire.Client;
using Reactive.Bindings;

namespace PhotonWire.HubInvoker
{
    public class MainWindowViewModel : IDisposable
    {
        readonly string directoryBase = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        readonly ReactiveProperty<ObservablePhotonPeer> peer;
        readonly System.Reactive.Disposables.SerialDisposable listenerSubscription = new System.Reactive.Disposables.SerialDisposable();
        IReadOnlyDictionary<short, HubInfo> hubInfoLookup = new Dictionary<short, HubInfo>();

        // Props

        public ReactiveProperty<string> ProcessPath { get; } = new ReactiveProperty<string>("PhotonSocketServer.exe");
        public ReactiveProperty<string> ProcessArgument { get; } = new ReactiveProperty<string>("");
        public ReactiveProperty<string> WorkingDir { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> AppName { get; } = new ReactiveProperty<string>("");
        public ReactiveProperty<string> Address { get; } = new ReactiveProperty<string>("127.0.0.1:4530");
        public ReactiveProperty<string> DllPath { get; } = new ReactiveProperty<string>("");
        public ReactiveProperty<string> Log { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<int> HubInfoListSelectedIndex { get; } = new ReactiveProperty<int>();
        public ReactiveProperty<int> UseConnectionTypeSelectedIndex { get; } = new ReactiveProperty<int>(1); // default = TCP
        public ReactiveProperty<string> CurrentConnectionStatus { get; } = new ReactiveProperty<string>();

        public string VersionInfo { get; }

        // Collection

        public ReactiveCollection<HubInfo> HubInfoList { get; } = new ReactiveCollection<HubInfo>();
        public ReactiveCollection<OperationItemViewModel> OperationInfoList { get; } = new ReactiveCollection<OperationItemViewModel>();
        public string[] UseConnectionType { get; } = Enum.GetNames(typeof(ConnectionProtocol));
        public Tuple<string, ReactiveCommand>[] Configrations { get; private set; }

        // Commands

        public ReactiveCommand KillPhotonProcess { get; }
        public ReactiveCommand StartPhotonProcess { get; }
        public ReactiveCommand ReloadDll { get; }
        public ReactiveCommand Connect { get; }
        public ReactiveCommand Disconnect { get; }
        public ReactiveCommand LogClear { get; }

        public MainWindowViewModel()
        {
            // Load
            LoadDefaultConfiguration();

            var useConnectionType = UseConnectionTypeSelectedIndex
                .Select(x => (ConnectionProtocol)Enum.Parse(typeof(ConnectionProtocol), UseConnectionType[x]))
                .ToReactiveProperty();

            // Setup peer
            peer = new ReactiveProperty<ObservablePhotonPeer>(new UseJsonObservablePhotonPeer(useConnectionType.Value));
            peer.Select(x => x.ObserveStatusChanged())
                .Switch()
                .Subscribe(x =>
                {
                    if (x == StatusCode.Connect)
                    {
                        CurrentConnectionStatus.Value = "Connecting : " + Address.Value + " " + AppName.Value;
                    }
                    else
                    {
                        CurrentConnectionStatus.Value = x.ToString();
                    }
                    Log.WriteLine(CurrentConnectionStatus.Value);
                });

            // Setup Properties

            HubInfoListSelectedIndex.Subscribe(x =>
            {
                foreach (var item in OperationInfoList)
                {
                    item.Dispose();
                }
                OperationInfoList.Clear();
                if (x == -1) return;
                if (HubInfoList.Count - 1 < x) return;

                var hub = HubInfoList[x];
                foreach (var item in hub.Operations)
                {
                    OperationInfoList.Add(new OperationItemViewModel(peer, Log, item));
                }
            });

            // Setup Commands

            var photonProcessExists = Observable.Interval(TimeSpan.FromSeconds(1)).Select(x => Process.GetProcessesByName("PhotonSocketserver").Any());
            KillPhotonProcess = photonProcessExists.ToReactiveCommand();
            KillPhotonProcess.Subscribe(_ =>
            {
                var processes = Process.GetProcessesByName("PhotonSocketServer");
                foreach (var item in processes)
                {
                    item.Kill();
                }
            });

            StartPhotonProcess = ProcessPath.CombineLatest(WorkingDir, (processPath, workingDir) => new { processPath, workingDir })
                .Select(x => !string.IsNullOrWhiteSpace(x.processPath + x.workingDir))
                .CombineLatest(photonProcessExists, (x, y) => x && !y)
                .ToReactiveCommand();
            StartPhotonProcess.Subscribe(_ =>
            {
                try
                {
                    var processPath = ProcessPath.Value;
                    var workingDir = WorkingDir.Value;

                    var pi = new ProcessStartInfo
                    {
                        FileName = ProcessPath.Value,
                        Arguments = ProcessArgument.Value,
                        WorkingDirectory = workingDir
                    };
                    System.Diagnostics.Process.Start(pi);

                    SaveConfiguration(); // can start, save path
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            });

            ReloadDll = DllPath.Select(x => File.Exists(x.Trim('\"'))).ToReactiveCommand(ImmediateScheduler.Instance); // needs Immediate check for InitialLoad(see:bottom code)
            ReloadDll.Subscribe(_ =>
            {
                try
                {
                    HubInfoList.Clear();
                    var hubInfos = HubAnalyzer.LoadHubInfos(DllPath.Value.Trim('\"'));
                    SaveConfiguration(); // can load, save path

                    hubInfoLookup = hubInfos.ToDictionary(x => x.HubId);

                    foreach (var hub in hubInfos)
                    {
                        HubInfoList.Add(hub);
                    }
                    HubInfoListSelectedIndex.Value = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            });

            Connect = peer.Select(x => x.ObserveStatusChanged())
                .Switch()
                .CombineLatest(Address, (x, y) => x != StatusCode.Connect && !string.IsNullOrEmpty(y))
                .ToReactiveCommand();
            Connect.Subscribe(async _ =>
            {
                try
                {
                    peer.Value.Dispose();
                    peer.Value = new UseJsonObservablePhotonPeer(useConnectionType.Value);
                    var b = await peer.Value.ConnectAsync(Address.Value, AppName.Value);
                    Log.WriteLine("Connect:" + b);
                    if (b)
                    {
                        SaveConfiguration(); // can connect, save path

                        // Register Listener
                        listenerSubscription.Disposable = peer.Value.ObserveReceiveEventData().Subscribe(ReceiveEvent);
                    }
                    else
                    {
                        listenerSubscription.Disposable = System.Reactive.Disposables.Disposable.Empty;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Can't connect:" + ex.ToString());
                    listenerSubscription.Disposable = System.Reactive.Disposables.Disposable.Empty;
                }
            });

            Disconnect = peer.Select(x => x.ObserveStatusChanged())
                .Switch()
                .Select(x => x == StatusCode.Connect)
                .ToReactiveCommand();
            Disconnect.Subscribe(_ =>
            {
                try
                {
                    peer.Value.Disconnect();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Can't disconnect:" + ex.ToString());
                }
            });

            LogClear = new ReactiveCommand();
            LogClear.Subscribe(_ =>
            {
                Log.Value = "";
            });

            // Initial Load
            if (ReloadDll.CanExecute())
            {
                ReloadDll.Execute();
            }

            // Initial VersionInfo
            VersionInfo = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version.ToString();
            if (peer.Value.ObserveStatusChanged().FirstAsync().GetAwaiter().GetResult() == StatusCode.Disconnect)
            {
                CurrentConnectionStatus.Value = "PhotonWire.HubInvoker " + VersionInfo;
            }
        }

        void SaveConfiguration()
        {
            SaveCurrentConfiguration("config.json");
        }

        public void SaveCurrentConfiguration(string fileName)
        {
            var conf = new Configuration
            {
                DllPath = DllPath.Value,
                ConnectionAddress = Address.Value,
                ProcessPath = ProcessPath.Value,
                WorkingDirectory = WorkingDir.Value,
                ProcessArgument = ProcessArgument.Value,
                SelectedConnectionTypeIndex = UseConnectionTypeSelectedIndex.Value,
                ApplicationName = AppName.Value
            };
            var saveFile = JsonConvert.SerializeObject(conf, Formatting.Indented);

            File.WriteAllText(System.IO.Path.Combine(directoryBase, fileName), saveFile);
        }

        void LoadDefaultConfiguration()
        {
            var configPath = System.IO.Path.Combine(directoryBase, "config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var text = File.ReadAllText(configPath);
                    var conf = JsonConvert.DeserializeObject<Configuration>(text);
                    DllPath.Value = conf.DllPath;
                    ProcessPath.Value = conf.ProcessPath;
                    WorkingDir.Value = conf.WorkingDirectory;
                    Address.Value = conf.ConnectionAddress;
                    ProcessArgument.Value = conf.ProcessArgument;
                    UseConnectionTypeSelectedIndex.Value = conf.SelectedConnectionTypeIndex;
                    AppName.Value = conf.ApplicationName;
                }
                catch { }
            }
            LoadConfigurations();
        }

        public void LoadConfigurations()
        {
            var configPath = System.IO.Path.Combine(directoryBase, "configuration");
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            Configrations = Directory.GetFiles(configPath)
                .OrderBy(x => x)
                .Select(x =>
                {
                    try
                    {
                        var text = File.ReadAllText(x);
                        var conf = JsonConvert.DeserializeObject<Configuration>(text);
                        var command = new ReactiveCommand();
                        command.Subscribe(_ =>
                        {
                            DllPath.Value = conf.DllPath;
                            ProcessPath.Value = conf.ProcessPath;
                            WorkingDir.Value = conf.WorkingDirectory;
                            Address.Value = conf.ConnectionAddress;
                            ProcessArgument.Value = conf.ProcessArgument;
                            UseConnectionTypeSelectedIndex.Value = conf.SelectedConnectionTypeIndex;
                            AppName.Value = conf.ApplicationName;
                        });

                        return Tuple.Create(
                            Path.GetFileNameWithoutExtension(x),
                            command
                        );
                    }
                    catch
                    {
                        MessageBox.Show("Can't Load Config:" + x);
                        return null;
                    }
                })
                .Where(x => x != null)
                .ToArray();
        }

        void ReceiveEvent(EventData eventData)
        {
            short hubId;
            {
                object hubIdObj;
                if (!eventData.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                {
                    return;
                }
                hubId = (short)hubIdObj;
            }

            var opCode = eventData.Code;

            OperationInfo targetOperation = null;
            {
                HubInfo hub;
                if (hubInfoLookup.TryGetValue(hubId, out hub))
                {
                    targetOperation = hub.ClientOpertaions.FirstOrDefault(x => x.OperationId == opCode);
                }
            }

            string parameterDump;
            if (targetOperation != null)
            {
                parameterDump = string.Join(", ",
                    targetOperation.Parameters.Zip(eventData.Parameters, (x, y) => x.Name + ":" + JsonPhotonSerializer.Deserialize(y.Value)));
            }
            else
            {
                parameterDump = string.Join(", ",
                    eventData.Parameters.Select(x => x.Key + ":" + JsonPhotonSerializer.Deserialize(x.Value)));
            }

            Log.WriteLine($"- {targetOperation?.Hub.HubName ?? hubId.ToString()}.{targetOperation?.OperationName ?? opCode.ToString()}:{parameterDump}");
        }

        public void Dispose()
        {
            listenerSubscription.Dispose();
            peer.Value.Dispose();
            KillPhotonProcess.Dispose();
            StartPhotonProcess.Dispose();
        }
    }

    public class Configuration
    {
        public string ProcessPath { get; set; }
        public string ProcessArgument { get; set; }
        public string WorkingDirectory { get; set; }
        public string ConnectionAddress { get; set; }
        public string DllPath { get; set; }
        public string ApplicationName { get; set; }
        public int SelectedConnectionTypeIndex { get; set; }
    }

    internal static class Extensions
    {
        public static void WriteLine(this ReactiveProperty<string> rp, string value)
        {
            rp.Value += value + Environment.NewLine;
        }
    }
}