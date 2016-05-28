using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotonWire.Client;
using Reactive.Bindings;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Input;
using System.Reactive.Subjects;
using Newtonsoft.Json.Linq;

namespace PhotonWire.HubInvoker
{
    // Cheap clipboard monitor:)
    public static class ClipboardMonitor
    {
        public static IObservable<string> CurrentClipboard { get; } = new Subject<string>();

        static ClipboardMonitor()
        {
            Observable.Interval(TimeSpan.FromMilliseconds(500))
                .ObserveOnDispatcher()
                .Subscribe(_ =>
                {
                    try
                    {
                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                        {
                            var txt = Clipboard.GetText(TextDataFormat.UnicodeText);
                            (CurrentClipboard as Subject<string>).OnNext(txt);
                        }
                    }
                    catch { }
                });
        }
    }

    public class ClipboardData
    {
        public string HubName { get; set; }
        public string OperationName { get; set; }
        public string[] Data { get; set; }
    }


    public class OperationItemViewModel : IDisposable
    {
        public OperationInfo Info { get; }
        public ParameterItemViewModel[] ParameterItems { get; }

        public ReactiveCommand CopyCommand { get; }
        public ReactiveCommand PasteCommand { get; }
        public ReactiveCommand SendCommand { get; }

        public OperationItemViewModel(ReactiveProperty<ObservablePhotonPeer> peer, ReactiveProperty<string> log, OperationInfo info)
        {
            Info = info;
            ParameterItems = info.FlatternedParameters
                .Select(x =>
                {
                    var piv = new ParameterItemViewModel
                    {
                        Name = x.Name,
                        TypeName = x.TypeName,
                        Comment = x.Comment,
                        IsNeedTemplate = x.IsNeedTemplate,
                        InsertButtonVisibility = x.IsNeedTemplate ? Visibility.Visible : Visibility.Hidden,
                        Template = (x.IsNeedTemplate) ? x.Template : null
                    };
                    if (x.DefaultValue != null) piv.ParameterValue.Value = x.DefaultValue.ToString();
                    return piv;
                })
                .ToArray();

            CopyCommand = new ReactiveCommand(Observable.Return(ParameterItems.Any()));
            CopyCommand.Subscribe(_ =>
            {
                var data = new ClipboardData
                {
                    HubName = Info.Hub.HubName,
                    OperationName = Info.OperationName,
                    Data = ParameterItems.Select(x => x.ParameterValue.Value).ToArray()
                };
                var value = JsonConvert.SerializeObject(data);

                Clipboard.SetText(value, TextDataFormat.UnicodeText);
            });

            if (!ParameterItems.Any())
            {
                PasteCommand = new ReactiveCommand(Observable.Return(false));
            }
            else
            {
                PasteCommand = ClipboardMonitor.CurrentClipboard
                    .Select(text =>
                    {
                        try
                        {
                            if (text.Contains(nameof(ClipboardData.HubName)) && text.Contains(nameof(ClipboardData.OperationName)))
                            {
                                var cd = JsonConvert.DeserializeObject<ClipboardData>(text);
                                if (cd.HubName == Info.Hub.HubName && cd.OperationName == Info.OperationName) return true;
                            }

                            return false;
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToReactiveCommand(initialValue: false);
                PasteCommand.Subscribe(_ =>
                {
                    try
                    {
                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                        {
                            var text = Clipboard.GetText();

                            var cd = JsonConvert.DeserializeObject<ClipboardData>(text);

                            var index = 0;
                            foreach (var item in cd.Data)
                            {
                                ParameterItems[index].ParameterValue.Value = item;
                                index++;
                            }
                        }
                    }
                    catch { }
                });
            }

            SendCommand = new ReactiveCommand();
            SendCommand.Subscribe(async _ =>
            {
                try
                {
                    byte opCode = Info.OperationId;
                    var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                    parameter.Add(ReservedParameterNo.RequestHubId, Info.Hub.HubId);

                    // grouping
                    var grouping = ParameterItems.GroupBy(x =>
                    {
                        var split = x.Name.Split('.');
                        return split[0];
                    });

                    var index = 0;
                    foreach (var item in grouping)
                    {
                        if (item.Count() == 1)
                        {
                            var p = item.First();
                            parameter.Add((byte)index, JsonPhotonSerializer.Serialize(p.TypeName, p.ParameterValue.Value));
                        }
                        else
                        {
                            // Object
                            var p = BuildJson(item);
                            parameter.Add((byte)index, Encoding.UTF8.GetBytes(p)); // send byte[]
                        }
                        index++;
                    }

                    var response = await peer.Value.OpCustomAsync(opCode, parameter, true);
                    var result = response[ReservedParameterNo.ResponseId];

                    var deserialized = JsonPhotonSerializer.Deserialize(result);
                    log.Value += "+ " + Info.Hub.HubName + "/" + Info.OperationName + ":" + deserialized + "\r\n";
                }
                catch (Exception ex)
                {
                    log.Value += "Send Error:" + ex.ToString() + "\r\n";
                }
            });
        }

        string BuildJson(IEnumerable<ParameterItemViewModel> items)
        {
            var root = new JObject();
            foreach (var item in items)
            {
                var split = item.Name.Split('.').Skip(1).ToArray(); // first is container name, ignore

                JObject rootJo = null;
                Array.Reverse(split);
                for (int i = 0; i < split.Length; i++)
                {
                    // Last
                    if (i == 0)
                    {
                        var o = new JObject();
                        o.Add(split[i], GetJToken(item.TypeName, item.ParameterValue.Value));
                        rootJo = o;
                    }
                    else
                    {
                        // append child
                        var o = new JObject();
                        o.Add(split[i], rootJo);
                        rootJo = o;
                    }
                }
                root.Merge(rootJo); // Merge Json
            }

            return root.ToString(Formatting.None);
        }


        JToken GetJToken(string typeName, string obj)
        {
            // why simply parse:)
            switch (typeName)
            {
                case "Byte":
                    return new JValue(byte.Parse(obj));
                case "Boolean":
                    return new JValue(bool.Parse(obj));
                case "Int16":
                    return new JValue(short.Parse(obj));
                case "Int32":
                    return new JValue(int.Parse(obj));
                case "Int64":
                    return new JValue(long.Parse(obj));
                case "Single":
                    return new JValue(Single.Parse(obj));
                case "Double":
                    return new JValue(double.Parse(obj));
                case "String":
                    return new JValue(obj);
                case "Int32[]":
                    return new JArray(obj.Trim('[', ']').Split(',').Select(x => int.Parse(x)).ToArray());
                case "Byte[]":
                    return new JArray(obj.Trim('[', ']').Split(',').Select(x => byte.Parse(x)).ToArray());
            }

            // others, use string...
            return JValue.Parse(obj);
        }

        public void Dispose()
        {
            // unsubscribe clipboard watch timer
            CopyCommand.Dispose();
            PasteCommand.Dispose();
        }
    }
}

public class ParameterItemViewModel
{
    public string Name { get; set; }
    public string TypeName { get; set; }
    public string Comment { get; set; }
    public bool IsNeedTemplate { get; set; }
    public string Template { get; set; }
    public Visibility InsertButtonVisibility { get; set; }
    public ReactiveCommand InsertTemplate { get; }
    public ReactiveProperty<string> ParameterValue { get; } = new ReactiveProperty<string>();

    public ParameterItemViewModel()
    {
        InsertTemplate = new ReactiveCommand();
        InsertTemplate.Subscribe(_ =>
        {
            ParameterValue.Value = Template;
        });
    }
}
