using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Data;

namespace Profes.Window
{
    class LanguageManager
    {
        private static LanguageManager defaultInstance = new LanguageManager();

        private static Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();
        private static string _useLanguage = "";
        private static ObjectDataProvider provider;

        static LanguageManager()
        {
            Directory.CreateDirectory("Language");
            Load("Language");
        }

        private static void Load(string directoryPath)
        {
            _dic.Clear();

            foreach (string path in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                _dic[Path.GetFileNameWithoutExtension(path)] = Read(path);
            }
        }

        private static Dictionary<string, string> Read(string fileName)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            using (XmlTextReader reader = new XmlTextReader(fileName))
            {
                string key = "";
                string value = "";

                try
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.LocalName.Equals("KeyValuePair"))
                            {
                            }
                            else if (reader.LocalName.Equals("Key"))
                            {
                                key = reader.ReadString();
                            }
                            else if (reader.LocalName.Equals("Value"))
                            {
                                value = reader.ReadString();
                            }
                        }
                        else if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            if (reader.LocalName.Equals("KeyValuePair"))
                            {
                                dic.Add(key, value);
                            }
                        }
                    }
                }
                catch (XmlException) { }
            }

            return dic;
        }

        public static LanguageManager GetInstance()
        {
            return defaultInstance;
        }

        /// <summary>
        /// 言語の切り替えメソッド
        /// </summary>
        /// <param name="language">使用言語を指定する</param>
        public static void ChangeLanguage(string language)
        {
            if (_dic.ContainsKey(language))
            {
                _useLanguage = language;
            }
            else
            {
                if (_dic.Count != 0)
                {
                    _useLanguage = _dic.Keys.ToArray()[0];
                }
            }

            ResourceProvider.Refresh();
        }

        /// <summary>
        /// 使用できる言語リスト
        /// </summary>
        public string[] Languages
        {
            get
            {
                var list = _dic.Keys.ToList();

                list.Sort(delegate(string x, string y)
                {
                    return x.CompareTo(y);
                });

                return list.ToArray();
            }
        }

        public static ObjectDataProvider ResourceProvider
        {
            get
            {
                if (System.Windows.Application.Current != null)
                    provider = (ObjectDataProvider)System.Windows.Application.Current.FindResource("ResourcesInstance");
                return provider;
            }
        }

        public string Cache { get { return _dic[_useLanguage]["Cache"]; } }
        public string CacheDirectory_C { get { return _dic[_useLanguage]["CacheDirectory_C"]; } }
        public string Cancel { get { return _dic[_useLanguage]["Cancel"]; } }
        public string CategoryList { get { return _dic[_useLanguage]["CategoryList"]; } }
        public string CreateKey { get { return _dic[_useLanguage]["CreateKey"]; } }
        public string CreationTime { get { return _dic[_useLanguage]["CreationTime"]; } }
        public string Download { get { return _dic[_useLanguage]["Download"]; } }
        public string DownloadDirectory_D { get { return _dic[_useLanguage]["DownloadDirectory_D"]; } }
        public string DownloadRate { get { return _dic[_useLanguage]["DownloadRate"]; } }
        public string FileName { get { return _dic[_useLanguage]["FileName"]; } }
        public string FileShare { get { return _dic[_useLanguage]["FileShare"]; } }
        public string Filter { get { return _dic[_useLanguage]["Filter"]; } }
        public string Hash { get { return _dic[_useLanguage]["Hash"]; } }
        public string ID { get { return _dic[_useLanguage]["ID"]; } }
        public string IpAddress_I { get { return _dic[_useLanguage]["IpAddress_I"]; } }
        public string Language { get { return _dic[_useLanguage]["Language"]; } }
        public string Node { get { return _dic[_useLanguage]["Node"]; } }
        public string NodeSetting { get { return _dic[_useLanguage]["NodeSetting"]; } }
        public string Ok { get { return _dic[_useLanguage]["Ok"]; } }
        public string PortNamber_O { get { return _dic[_useLanguage]["PortNamber_O"]; } }
        public string Privatekey_R { get { return _dic[_useLanguage]["Privatekey_R"]; } }
        public string Publickey_U { get { return _dic[_useLanguage]["Publickey_U"]; } }
        public string Query { get { return _dic[_useLanguage]["Query"]; } }
        public string Review { get { return _dic[_useLanguage]["Review"]; } }
        public string Setting { get { return _dic[_useLanguage]["Setting"]; } }
        public string Settings { get { return _dic[_useLanguage]["Settings"]; } }
        public string Sign { get { return _dic[_useLanguage]["Sign"]; } }
        public string Size { get { return _dic[_useLanguage]["Size"]; } }
        public string Skin { get { return _dic[_useLanguage]["Skin"]; } }
        public string Status { get { return _dic[_useLanguage]["Status"]; } }
        public string Trigger { get { return _dic[_useLanguage]["Trigger"]; } }
        public string UpdateTime { get { return _dic[_useLanguage]["UpdateTime"]; } }
        public string Upload { get { return _dic[_useLanguage]["Upload"]; } }
    }
}