// https://msdn.microsoft.com/en-us/library/dn940021.aspx

// VSTU 1.8.0 supports T4 template but sometimes lost .tt,
// this hook file force include .tt in generated project.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using UnityEngine;
using UnityEditor;

using SyntaxTree.VisualStudio.Unity.Bridge;

namespace PhotonWire
{
    [InitializeOnLoad]
    public class ProjectFileHook
    {
        // necessary for XLinq to save the xml project file in utf8
        class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }

        static ProjectFileHook()
        {
            ProjectFilesGenerator.ProjectFileGeneration += (string name, string content) =>
            {
                // parse the document and make some changes
                var document = XDocument.Parse(content);

                IncludeT4Template(document);

                // save the changes using the Utf8StringWriter
                var str = new Utf8StringWriter();
                document.Save(str);

                return str.ToString();
            };
        }

        static void IncludeT4Template(XDocument document)
        {
            var currentDir = Directory.GetCurrentDirectory();

            // Gets include in file
            var targetChildren = document.Root.Nodes()
                .OfType<XElement>()
                .Where(x => x.Name.LocalName == "ItemGroup")
                .Where(x => x.FirstNode != null)
                .Select(x => x.FirstNode)
                .OfType<XElement>()
                .Where(x => x.Name.LocalName == "Compile" || x.Name.LocalName == "None");

            var targetNodes = targetChildren.Select(x => x.Parent).Where(x => x != null).ToArray();
            if (!targetNodes.Any()) return;

            // get node
            var includeNodes = targetNodes.SelectMany(x => x.Descendants())
                .Where(x => x.Attribute("Include") != null)
                .Where(x => !string.IsNullOrEmpty(x.Attribute("Include").Value))
                .ToArray();

            var ns = document.Root.Name.Namespace;
            var directoryUri = new Uri(Path.Combine(currentDir, "Assets"));

            var compileElements = document.Descendants(ns + "Compile")
                .Where(x => x.Attribute("Include") != null)
                .ToArray();

            // search tt under current dir
            var files = Directory.GetFiles(Path.Combine(currentDir, "Assets"), "*.tt", SearchOption.AllDirectories)
                .Select(x => new
                {
                    Template = x,
                    TemplateRelative = directoryUri.MakeRelativeUri(new Uri(x)).ToString().Replace('/', '\\'), // relative
                    Generated = Path.GetFileNameWithoutExtension(x) + ".Generated.cs",
                })
                .ToArray();

            // remove exists template
            foreach (var fileName in files.Select(file => Path.GetFileName(file.Template)))
            {
                document.Descendants(ns + "None").Where(x => x.Attribute("Include").Value.EndsWith(fileName)).Remove();

                var dependentUponNodes = includeNodes
                    .Where(x => x.Descendants(ns + "DependentUpon")
                        .Where(y => !string.IsNullOrEmpty(y.Value))
                        .Any(y => y.Value.ToLower() == fileName.ToLower()));
                dependentUponNodes
                    .SelectMany(x => x.Descendants(ns + "DependentUpon")
                        .Concat(x.Descendants(ns + "AutoGen"))
                        .Concat(x.Descendants(ns + "DesignTime")))
                        .Remove();
            }

            // get generated files
            var generatedEelements = files.SelectMany(x => compileElements
                .Where(y => y.Attribute("Include").Value.EndsWith(x.Generated))
                .Select(y => new { x.Template, Element = y }));

            // append dependentupon
            foreach (var generatedEelement in generatedEelements)
            {
                var fileName = Path.GetFileName(generatedEelement.Template);
                generatedEelement.Element.Add(
                    new XElement(ns + "AutoGen", "True"),
                    new XElement(ns + "DesignTime", "True"),
                    new XElement(ns + "DependentUpon", fileName));
            }

            // add element
            var elements = files.Select(x =>
            {
                var element = new XElement(ns + "None");
                element.SetAttributeValue("Include", x.TemplateRelative);
                var generator = new XElement(ns + "Generator", "TextTemplatingFileGenerator");
                element.Add(generator);
                var lastGenOutput = new XElement(ns + "LastGenOutput", x.Generated);
                element.Add(lastGenOutput);
                return element;
            })
                .ToArray();

            var targetnode = targetNodes.First();
            foreach (var element in elements)
            {
                targetnode.Add(element);
            }

        }
    }
}