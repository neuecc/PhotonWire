using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Mono.Cecil;

namespace PhotonWire.HubInvoker
{
    public static class HubAnalyzer
    {
        public static HubInfo[] LoadHubInfos(string dllPath)
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(dllPath));

            var readerParam = new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = false,
                AssemblyResolver = resolver// new Resolver(dllPath)
            };

            var asm = AssemblyDefinition.ReadAssembly(dllPath, readerParam);

            var baseHubName = "Hub`1";
            Func<TypeDefinition, TypeDefinition> SearchBaseHub = x =>
            {
                while (x != null && x.FullName != "System.Object")
                {
                    if (x.Name == baseHubName) return x;
                    if (x.BaseType == null) return x;
                    if (x.BaseType.Name == baseHubName) return x;
                    x = x.BaseType?.Resolve();
                }
                return null;
            };

            var xmlCommentCache = new Dictionary<string, ILookup<Tuple<string, string>, XmlCommentStructure>>();

            var hubTypes = asm.MainModule.GetTypes()
                .Where(x => SearchBaseHub(x) != null)
                .Where(x => !x.IsAbstract)
                .Where(x => x.CustomAttributes.All(y => y.AttributeType.FullName != "PhotonWire.Server.IgnoreOperationAttribute"))
                .Where(x => x.CustomAttributes.Any(y => y.AttributeType.FullName == "PhotonWire.Server.HubAttribute"));

            var hubInfos = hubTypes
                .Select(hub =>
                {
                    var xDocPath = Path.Combine(Path.GetDirectoryName(hub.Module.FullyQualifiedName), Path.GetFileNameWithoutExtension(hub.Module.FullyQualifiedName)) + ".xml";
                    ILookup<Tuple<string, string>, XmlCommentStructure> xCommentLookup;
                    if (!xmlCommentCache.TryGetValue(xDocPath, out xCommentLookup))
                    {
                        xCommentLookup = BuildXmlCommentStructure(xDocPath);
                        xmlCommentCache.Add(xDocPath, xCommentLookup);
                    }

                    var hubInfo = new HubInfo()
                    {
                        HubName = hub.Name,
                        HubId = (short)hub.CustomAttributes.First(x => x.AttributeType.Name == "HubAttribute").ConstructorArguments[0].Value,
                        ClientOpertaions = new OperationInfo[0]
                    };

                    hubInfo.Operations = hub.Methods.Where(x => x.IsPublic
                            && x.HasCustomAttributes
                            && x.CustomAttributes.All(y => y.AttributeType.FullName != "PhotonWire.Server.IgnoreOperationAttribute"))
                        .Select(x => new { method = x, attr = x.CustomAttributes.FirstOrDefault(y => y.AttributeType.Name == "OperationAttribute") })
                        .Where(x => x.attr != null)
                        .Select(x =>
                        {
                            var doc = xCommentLookup[Tuple.Create(hubInfo.HubName, x.method.Name)].FirstOrDefault();

                            return new OperationInfo
                            {
                                Hub = hubInfo,
                                OperationName = x.method.Name,
                                OperationId = (byte)x.attr.ConstructorArguments[0].Value,
                                Comment = doc?.Summary ?? null,
                                Parameters = x.method.Parameters.Select(y => new ParameterInfo
                                {
                                    Name = y.Name,
                                    TypeName = (y.ParameterType.Name == "Nullable`1") ? (y.ParameterType as GenericInstanceType).GenericArguments[0].Name + "?"
                                        : (y.ParameterType.IsGenericInstance) ? y.ParameterType.Name.Split('`')[0] + "<" + string.Join(", ", (y.ParameterType as GenericInstanceType).GenericArguments.Select(z => z.Name)) + ">"
                                        : y.ParameterType.Name,
                                    DefaultValue = y.HasConstant ? y.Constant : null,
                                    Comment = GetValueOrDefault(doc?.Parameters, y.Name),
                                    TypeDef = y.ParameterType.Resolve(),
                                    TypeRef = y.ParameterType
                                }).ToArray()
                            };
                        })
                        .OrderBy(x => x.OperationId)
                        .ToArray();

                    var baseType = hub?.BaseType as GenericInstanceType;
                    if (baseType != null && baseType.HasGenericArguments)
                    {
                        var clientType = baseType.GenericArguments[0];

                        var typeResolved = clientType.Resolve();
                        if (typeResolved != null)
                        {
                            hubInfo.ClientOpertaions = typeResolved.Methods
                                .Select(x => new
                                {
                                    Method = x,
                                    Name = x.Name,
                                    OperationAttr = x.CustomAttributes.FirstOrDefault(y => y.AttributeType.Name == "OperationAttribute")
                                })
                                .Where(x => x.OperationAttr != null)
                                .Select(x => new OperationInfo
                                {
                                    Hub = hubInfo,
                                    OperationName = x.Name,
                                    OperationId = (byte)x.OperationAttr.ConstructorArguments[0].Value,
                                    Parameters = x.Method.Parameters.Select(y => new ParameterInfo
                                    {
                                        Name = y.Name,
                                        TypeName = (y.ParameterType.Name == "Nullable`1") ? (y.ParameterType as GenericInstanceType).GenericArguments[0].Name + "?"
                                            : (y.ParameterType.IsGenericInstance) ? y.ParameterType.Name.Split('`')[0] + "<" + string.Join(", ", (y.ParameterType as GenericInstanceType).GenericArguments.Select(z => z.Name)) + ">"
                                            : y.ParameterType.Name,
                                        TypeDef = y.ParameterType.Resolve(),
                                        TypeRef = y.ParameterType
                                    }).ToArray()
                                })
                                .OrderBy(x => x.OperationId)
                                .ToArray();
                        }
                    }

                    return hubInfo;
                })
                .OrderBy(x => x.HubId)
                .ToArray();

            return hubInfos;
        }


        static string GetValueOrDefault(Dictionary<string, string> dict, string key)
        {
            if (dict == null) return null;
            string r;
            return dict.TryGetValue(key, out r) ? r : null;
        }

        // Class, MethodName => XmlComment
        static ILookup<Tuple<string, string>, XmlCommentStructure> BuildXmlCommentStructure(string xmlDocumentPath)
        {
            if (!File.Exists(xmlDocumentPath)) return Enumerable.Empty<XmlCommentStructure>().ToLookup(_ => Tuple.Create("", ""));

            var file = File.ReadAllText(xmlDocumentPath);
            var xDoc = XDocument.Parse(file);
            var xDocLookup = xDoc.Descendants("member")
                .Where(x => x.Attribute("name").Value.StartsWith("M:"))
                .Select(x =>
                {
                    var match = Regex.Match(x.Attribute("name").Value, @"(\w+)\.(\w+)?(\(.+\)|$)");

                    var summary = ((string)x.Element("summary")) ?? "";
                    var returns = ((string)x.Element("returns")) ?? "";
                    var remarks = ((string)x.Element("remarks")) ?? "";
                    var parameters = x.Elements("param")
                        .Select(e => Tuple.Create(e.Attribute("name").Value, e))
                        .Distinct(new Item1EqualityCompaerer<string, XElement>())
                        .ToDictionary(e => e.Item1, e => e.Item2.Value?.Trim().Replace("\r\n", " ") ?? "");

                    return new XmlCommentStructure
                    {
                        ClassName = match.Groups[1].Value,
                        MethodName = match.Groups[2].Value,
                        Summary = summary.Trim().Replace("\r\n", " "), // to singleline
                        Remarks = remarks,
                        Parameters = parameters,
                        Returns = returns
                    };
                })
                .ToLookup(x => Tuple.Create(x.ClassName, x.MethodName));

            return xDocLookup;
        }

        class Item1EqualityCompaerer<T1, T2> : EqualityComparer<Tuple<T1, T2>>
        {
            public override bool Equals(Tuple<T1, T2> x, Tuple<T1, T2> y)
            {
                return x.Item1.Equals(y.Item1);
            }

            public override int GetHashCode(Tuple<T1, T2> obj)
            {
                return obj.Item1.GetHashCode();
            }
        }

        class XmlCommentStructure
        {
            public string ClassName { get; set; }
            public string MethodName { get; set; }
            public string Summary { get; set; }
            public string Remarks { get; set; }
            public Dictionary<string, string> Parameters { get; set; }
            public string Returns { get; set; }
        }
    }

    public class HubInfo
    {
        public string HubName { get; set; }
        public short HubId { get; set; }
        public OperationInfo[] Operations { get; set; }
        public OperationInfo[] ClientOpertaions { get; set; }

        public override string ToString()
        {
            return HubName + "[" + HubId + "]";
        }
    }

    public class OperationInfo
    {
        public HubInfo Hub { get; set; }
        public string OperationName { get; set; }
        public byte OperationId { get; set; }
        public string Comment { get; set; }
        public ParameterInfo[] Parameters { get; set; }

        public ParameterInfo[] FlatternedParameters
        {
            get
            {
                return Parameters.SelectMany(x => FlattenParameter(x)).ToArray();
            }
        }

        IEnumerable<ParameterInfo> FlattenParameter(ParameterInfo parameter)
        {
            if (parameter.TypeDef == null)
            {
                yield return parameter;
            }
            else
            {
                // ignore startwith System namespace...
                if (!parameter.TypeDef.HasProperties || parameter.TypeDef.FullName.StartsWith("System."))
                {
                    yield return parameter;
                }
                else if (parameter.TypeName.EndsWith("[]")) // complex type + array is not supported yet...
                {
                    yield return parameter;
                }
                else
                {
                    foreach (var item in parameter.TypeDef.Properties)
                    {
                        var pi = new ParameterInfo
                        {
                            Name = parameter.Name + "." + item.Name,
                            TypeName = (item.PropertyType.Name == "Nullable`1") ? (item.PropertyType as GenericInstanceType).GenericArguments[0].Name + "?"
                                : (item.PropertyType.IsGenericInstance) ? item.PropertyType.Name.Split('`')[0] + "<" + string.Join(", ", (item.PropertyType as GenericInstanceType).GenericArguments.Select(z => z.Name)) + ">"
                                : item.PropertyType.Name,
                            Comment = parameter.Comment,
                            TypeDef = item.PropertyType.Resolve(),
                            TypeRef = item.PropertyType
                        };
                        foreach (var item2 in FlattenParameter(pi))
                        {
                            yield return item2;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return OperationName + "[" + OperationId + "]";
        }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Comment { get; set; }
        public object DefaultValue { get; set; }
        public TypeDefinition TypeDef { get; set; }
        public TypeReference TypeRef { get; set; }

        const int MaxRecursiveCount = 10;

        public bool IsNeedTemplate
        {
            get
            {
                if (TypeRef.IsPrimitive) return false;
                if (TypeRef.Name == "Nullable`1") return false;
                if (TypeRef.Name == "DateTime") return false;
                if (TypeRef.Name == "DateTimeOffset") return false;
                if (TypeRef.Name == "String") return false;
                if (TypeDef.IsEnum) return false;

                return true;
            }
        }

        public string Template
        {
            get
            {
                return GetTemplate(TypeRef, 0);
            }
        }

        string GetTemplate(TypeReference reference, int recursiveCount)
        {
            recursiveCount++;
            if (recursiveCount == MaxRecursiveCount) return "";

            if (reference.IsPrimitive)
            {
                return "<" + reference.Name + ">";
            }
            else if (reference.IsArray)
            {
                var sb = new StringBuilder();
                sb.Append("[");
                var elem = GetTemplate(reference.GetElementType(), recursiveCount);
                sb.Append(elem);
                sb.Append("]");
                return sb.ToString();
            }
            else if (reference.Name == "List`1")
            {
                var gen = reference as GenericInstanceType;
                if (gen == null) return "[]";

                var sb = new StringBuilder();
                sb.Append("[");
                var elem = GetTemplate(gen.GenericArguments[0], recursiveCount);
                sb.Append(elem);
                sb.Append("]");
                return sb.ToString();
            }
            else if (reference.Name == "Nullable`1" || reference.Name == "DateTime" || reference.Name == "DateTimeOffset" || reference.Name == "String")
            {
                return "<" + reference.Name + ">";
            }
            else if (reference.Name == "Dictionary`2")
            {
                var gen = reference as GenericInstanceType;
                if (gen == null) return "{}";

                var key = gen.GenericArguments[0];
                var value = gen.GenericArguments[1];

                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"");
                sb.Append("<" + key.Name + ">");
                sb.Append("\"");
                sb.Append(":");
                var elem = GetTemplate(value, recursiveCount);
                sb.Append(elem);
                sb.Append("}");

                return sb.ToString();
            }
            else
            {
                var t = reference.Resolve();
                if (t == null) return "{}";

                if (t.IsEnum) return "<" + reference.Name + ">";

                var isFirst = true;
                var sb = new StringBuilder();
                sb.Append("{");
                foreach (var item in t.Properties)
                {
                    if (item.SetMethod == null) continue;

                    if (isFirst) isFirst = false;
                    else sb.Append(",");

                    sb.Append("\"");
                    sb.Append(item.Name);
                    sb.Append("\"");
                    sb.Append(":");
                    var elem = GetTemplate(item.PropertyType, recursiveCount);
                    sb.Append(elem);
                }
                sb.Append("}");
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}