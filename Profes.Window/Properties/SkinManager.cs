using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Data;
using System.Windows;
using System.Windows.Markup;

namespace Profes.Window
{
    class SkinManager
    {
        private static SkinManager defaultInstance = new SkinManager();
        private static ResourceDictionary _rd = new ResourceDictionary();

        static SkinManager()
        {
            Directory.CreateDirectory("Skin");
            Load("Skin");
        }

        private static void Load(string directoryPath)
        {
            _rd.Clear();

            foreach (string path in Directory.GetFiles(directoryPath, "*.xaml", SearchOption.AllDirectories))
            {
                ResourceDictionary skinXaml = null;

                try
                {
                    skinXaml = (ResourceDictionary)XamlReader.Load(new FileStream(path, FileMode.Open));
                }
                catch (XamlParseException) { }

                if (skinXaml == null) continue;

                _rd[Path.GetFileNameWithoutExtension(path)] = skinXaml;
            }
        }

        public static SkinManager GetInstance()
        {
            return defaultInstance;
        }

        private static int? _skinIndex;

        /// <summary>
        /// スキンの切り替えメソッド
        /// </summary>
        /// <param name="skin">使用するスキンを指定する</param>
        public static void ChangeSkin(string skin)
        {
            if (_skinIndex == null)
            {
                System.Windows.Application.Current.Resources.MergedDictionaries.Add((ResourceDictionary)_rd[skin]);
                _skinIndex = System.Windows.Application.Current.Resources.MergedDictionaries.Count - 1;
            }
            else
            {
                System.Windows.Application.Current.Resources.MergedDictionaries[(int)_skinIndex] = (ResourceDictionary)_rd[skin];
            }
        }

        /// <summary>
        /// 使用できるスキンリスト
        /// </summary>
        public string[] Skins
        {
            get
            {
                var list = _rd.Keys.OfType<string>().ToList();

                list.Sort(delegate(string x, string y)
                {
                    return x.CompareTo(y);
                });

                return list.ToArray();
            }
        }
    }
}