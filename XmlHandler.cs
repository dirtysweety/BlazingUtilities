using System.Xml;
using System.Xml.Linq;

namespace BlazingUtilities
{
    public class XmlHandler
    {
        private const string Root = "Root";
        private XDocument? _doc;
        private string _address = "";
        private bool _inMemory;

        private void ThrowInit()
        {
            if (_doc == null) throw new Exception("Xml handler not initialized.");
        }

        public Task InitWithFileAsync(string address)
        {
            _address = address;
            _inMemory = false;
            if (File.Exists(_address)) return ReadXmlAsync(_address);
            _doc = new XDocument(CreateElement(Root));
            return WriteXmlAsync(_address);
        }

        public void InitInMemory(string content = "")
        {
            _address = "";
            _inMemory = true;
            _doc = content == "" ? new XDocument(CreateElement(Root)) : XDocument.Parse(content);
        }

        private async Task ReadXmlAsync(string address)
        {
            await using FileStream fs = new FileStream(address, FileMode.Open, FileAccess.Read, FileShare.None);
            _doc = await XDocument.LoadAsync(fs, LoadOptions.None, CancellationToken.None);
        }

        private async Task WriteXmlAsync(string address)
        {
            if (_doc == null) throw new Exception("Xml doc initialized incorrectly.");
            await using FileStream fs = new FileStream(address, FileMode.Create, FileAccess.Write, FileShare.None);
            await _doc.SaveAsync(fs, SaveOptions.None, CancellationToken.None);
        }

        public Task SaveAsync()
        {
            ThrowInit();
            return _inMemory ? Task.CompletedTask : WriteXmlAsync(_address);
        }

        public XElement GetRoot()
        {
            ThrowInit();
            return _doc?.Element(Root) ?? throw new Exception("Xml handler has initialized incorrectly.");
        }

        public static XElement CreateElement(string name)
        {
            return new XElement(name.ToXml());
        }

        public static XAttribute CreateAttribute(string name, string value)
        {
            return new XAttribute(name.ToXml(), value.ToXml());
        }
    }

    public static class XmlHandlerHelperMethods
    {
        internal static string ToXml(this string s)
        {
            return XmlConvert.EncodeName(s);
        }

        internal static string FromXml(this string s)
        {
            return XmlConvert.DecodeName(s);
        }

        public static string Name(this XElement el)
        {
            return el.Name.LocalName.FromXml();
        }

        public static string Name(this XAttribute att)
        {
            return att.Name.LocalName.FromXml();
        }

        public static string GetStr(this XElement el, string attName)
        {
            return el.Attribute(attName).Check().Value.FromXml();
        }

        public static int GetInt(this XElement el, string attName)
        {
            return int.TryParse(el.Attribute(attName).Check().Value.FromXml(), out var result)
                ? result
                : throw new Exception("Int attribute parse failed.");
        }

        public static DateTime GetDateTime(this XElement el, string attName)
        {
            return DateTime.TryParse(el.Attribute(attName).Check().Value.FromXml(), out var result)
                ? result
                : throw new Exception("DateTime attribute parse failed.");
        }

        public static T Get<T>(this XElement el, string attName, Func<string, T> parser)
        {
            return parser(el.Attribute(attName).Check().Value.FromXml());
        }

        public static void SetAttribute(this XElement el, string attName, string attValue)
        {
            el.Attribute(attName.ToXml()).Check().SetValue(attValue.ToXml());
        }

        public static XElement Check(this XElement? target)
        {
            return target ?? throw new Exception("Element is null.");
        }

        public static XAttribute Check(this XAttribute? target)
        {
            return target ?? throw new Exception("Attribute is null.");
        }
    }

}
