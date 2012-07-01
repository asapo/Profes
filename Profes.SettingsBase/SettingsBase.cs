using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Soap;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;

namespace Profes.Configuration
{
    public class SettingsBase
    {
        private Dictionary<string, object> _dic = new Dictionary<string, object>();

        public SettingsBase() { }

        public static SettingsBase Synchronized(SettingsBase settingsBase)
        {
            lock (settingsBase)
            {
                return settingsBase;
            }
        }

        protected void Load(string path)
        {
            var formatter = new BinaryFormatter();

            using (Stream stream = new FileStream(path, FileMode.Open))
            {
                var i = (int)formatter.Deserialize(stream);

                for (int j = 0; j < i; j++)
                {
                    try
                    {
                        var Key = (string)formatter.Deserialize(stream);
                        var Value = (object)formatter.Deserialize(stream);

                        _dic[Key] = Value;
                    }
                    catch { }
                }
            }
        }

        protected void Save(string path)
        {
            var formatter = new BinaryFormatter();
            string tempPath = FileNameWithoutRepetition(Directory.GetCurrentDirectory() + @"\SettingsBase.temp");

            try
            {
                using (Stream stream = new FileStream(tempPath, FileMode.Create))
                {
                    var KeyList = new string[_dic.Count];
                    var ValueList = new object[_dic.Count];

                    int i = 0;
                    foreach (string key in _dic.Keys)
                    {
                        KeyList[i] = key;
                        ValueList[i] = _dic[key];
                        i++;
                    }

                    formatter.Serialize(stream, i);

                    for (int j = 0; j < i; j++)
                    {
                        formatter.Serialize(stream, KeyList[j]);
                        formatter.Serialize(stream, ValueList[j]);
                    }
                }

                File.Delete(path);
                File.Move(tempPath, path);
            }
            catch (IOException)
            {
                MessageBox.Show(path + "設定ファイルが保存できませんでした。",
                    "Profes, Profes.Configuration.dll",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                this.Save(path);
                return;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);

                this.Save(path);
                return;
            }
        }

        /// <summary>
        /// 重複のないファイル名を生成する
        /// </summary>
        private string FileNameWithoutRepetition(string path)
        {
            if (File.Exists(path) == false) return path;

            for (int index = 1; ; index++)
            {
                string text = string.Format(@"{0}\{1}({2}){3}",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    index,
                    Path.GetExtension(path));

                if (File.Exists(text) == false) return text;
            }
        }

        public object this[string propertyName]
        {
            get
            {
                if (!_dic.ContainsKey(propertyName)) return null;
                return _dic[propertyName];
            }
            set
            {
                _dic[propertyName] = value;
            }
        }
    }
}