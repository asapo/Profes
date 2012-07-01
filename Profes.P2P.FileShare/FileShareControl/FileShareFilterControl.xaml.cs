using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.Serialization;
using Profes.P2P.FileShare.Properties;
using System.Collections;
using Profes.P2P.FileShare.ServiceModel;
using System.Text.RegularExpressions;
using Profes.Security.Cryptography;
using Profes.BinaryEditorBase;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FileShareFilterControl.xaml の相互作用ロジック
    /// </summary>
    public partial class FileShareFilterControl : UserControl
    {
        public FileShareFilterControl()
        {
            InitializeComponent();
        }

        public static CacheListViewItem[] CacheListViewItemListFilter(CacheListViewItem[] cacheListViewItemList)
        {
            List<CacheListViewItem> clist = new List<CacheListViewItem>();

            foreach (CacheListViewItem c in cacheListViewItemList)
            {
                if (!CacheListViewItemFilter(c)) clist.Add(c);
            }

            return clist.ToArray();
        }

        private static bool CacheListViewItemFilter(CacheListViewItem cacheListViewItem)
        {
            var flist = Settings.Default._filterList.Where(n => n.Effect == true).ToArray();
            if (flist.Length == 0) return false;

            bool? flag = null;

            foreach (Filter f in flist)
            {
                // ファイル名の比較
                if (f.Name.Trim() != "")
                {
                    if (flag == null) flag = true;

                    if (f.Name.Contains("\""))
                    {
                        foreach (Match m in Regex.Matches(f.Name, "\"(.*?)\""))
                        {
                            string mat = m.Value.Trim('\"');
                            flag &= ((!mat.StartsWith("-") && cacheListViewItem.Name.Contains(mat)) || (mat.StartsWith("-") && !cacheListViewItem.Name.Contains(mat.Substring(1))));
                        }
                    }
                    else
                    {
                        flag &= cacheListViewItem.Name.Contains(f.Name);
                    }
                }

                // カテゴリの比較
                if (f.Category != null)
                {
                    if (flag == null) flag = true;

                    foreach (string ss in f.Category)
                    {
                        if (ss != null && ss.Trim() != "")
                        {
                            flag &= cacheListViewItem.Category.Any(n => n != null && n.ToLower() == ss.ToLower());
                        }
                    }
                }

                // IDの比較
                if (f.ID != null && f.ID.Trim() != "")
                {
                    if (flag == null) flag = true;

                    flag &= f.ID == Convert.ToBase64String(HashFunction.HashCreate(cacheListViewItem.PublicKey));
                }

                // ハッシュの比較
                if (f.Hash != null && f.Hash.Length != 0)
                {
                    if (flag == null) flag = true;

                    flag &= BinaryEditor.ArrayEquals(f.Hash, cacheListViewItem.Hash);
                }

                // サイズ上限
                if (f.LimitSize != 0)
                {
                    if (flag == null) flag = true;

                    flag &= f.LimitSize > cacheListViewItem.Size;
                }

                // サイズ下限
                if (f.LowerSize != 0)
                {
                    if (flag == null) flag = true;

                    flag &= f.LowerSize < cacheListViewItem.Size;
                }
            }

            return flag == true ? true : false;
        }
        
      /*  public static Cache[] CacheListFilter(Cache[] cacheList)
        {
            List<Cache> clist = new List<Cache>();

            foreach (Cache c in cacheList)
            {
                if (!CacheFilter(c)) clist.Add(c);
            }

            return clist.ToArray();
        }

        private static bool CacheFilter(Cache cache)
        {
            var flist = Settings.Default._filterList.Where(n => n.Effect == true).ToArray();
            if (flist.Length == 0) return false;

            bool? flag = null;

            foreach (Filter f in flist)
            {
                // ファイル名の比較
                if (f.Name.Trim() != "")
                {
                    if (flag == null) flag = true;

                    if (f.Name.Contains("\""))
                    {
                        foreach (Match m in Regex.Matches(f.Name, "\"(.*?)\""))
                        {
                            string mat = m.Value.Trim('\"');
                            flag &= ((!mat.StartsWith("-") && cache.Name.Contains(mat)) || (mat.StartsWith("-") && !cache.Name.Contains(mat.Substring(1))));
                        }
                    }
                    else
                    {
                        flag &= cache.Name.Contains(f.Name);
                    }
                }

                // カテゴリの比較
                if (f.Category != null)
                {
                    if (flag == null) flag = true;

                    foreach (string ss in f.Category)
                    {
                        if (ss != null && ss.Trim() != "")
                        {
                            flag &= cache.Category.Any(n => n != null && n.ToLower() == ss.ToLower());
                        }
                    }
                }

                // IDの比較
                if (f.ID != null && f.ID.Trim() != "")
                {
                    if (flag == null) flag = true;

                    flag &= f.ID == Convert.ToBase64String(HashFunction.HashCreate(cache.PublicKey));
                }

                // ハッシュの比較
                if (f.Hash != null && f.Hash.Length != 0)
                {
                    if (flag == null) flag = true;

                    flag &= BinaryEditor.ArrayEquals(f.Hash, cache.Hash);
                }

                // サイズ上限
                if (f.LimitSize != 0)
                {
                    if (flag == null) flag = true;

                    flag &= f.LimitSize > cache.Size;
                }

                // サイズ下限
                if (f.LowerSize != 0)
                {
                    if (flag == null) flag = true;

                    flag &= f.LowerSize < cache.Size;
                }
            }

            return flag == true ? true : false;
        }*/
        
        private void 追加_J_Click(object sender, RoutedEventArgs e)
        {
            using (FilterSettingWindow filterSettingWindow = new FilterSettingWindow())
            {
                filterSettingWindow.ShowDialog();
            }
        }

        private void 編集_E_Click(object sender, RoutedEventArgs e)
        {
            var item = FilterListView.SelectedItem as Filter;

            if (item != null)
            {
                using (FilterSettingWindow filterSettingWindow = new FilterSettingWindow())
                {
                    filterSettingWindow.SetFilter(item);
                    filterSettingWindow.ShowDialog();

                    if (filterSettingWindow.DialogResult == true)
                        Settings.Default._filterList.Remove(item);
                }
            }
        }

        private void 削除_D_Click(object sender, RoutedEventArgs e)
        {
            var items = FilterListView.SelectedItems as IList;

            if (items != null)
            {
                foreach (Filter cl in items.OfType<Filter>().ToArray())
                {
                    Settings.Default._filterList.Remove(cl);
                }
            }
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {

        }
    }

    [Serializable]
    public class Filter : INotifyPropertyChanged, ISerializable
    {
        string _name;
        string[] _category;
        string _id;
        long _limitSize;
        long _lowerSize;
        byte[] _hash;
        bool _effect;

        public Filter() { }

        protected Filter(SerializationInfo info, StreamingContext context)
        {
            Name = (string)info.GetValue("Name", typeof(string));
            Category = (string[])info.GetValue("Category", typeof(string[]));
            ID = (string)info.GetValue("ID", typeof(string));
            LimitSize = (long)info.GetValue("LimitSize", typeof(long));
            LowerSize = (long)info.GetValue("LowerSize", typeof(long));
            Effect = (bool)info.GetValue("Effect", typeof(bool));
        }

        new public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name, typeof(string));
            info.AddValue("Category", Category, typeof(string[]));
            info.AddValue("ID", ID, typeof(string));
            info.AddValue("LimitSize", LimitSize, typeof(long));
            info.AddValue("LowerSize", LowerSize, typeof(long));
            info.AddValue("Effect", Effect, typeof(bool));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string[] Category
        {
            get { return _category; }
            set
            {
                _category = value;
                NotifyPropertyChanged("Category");
            }
        }

        public string ID
        {
            get { return _id; }
            set
            {
                _id = value;
                NotifyPropertyChanged("ID");
            }
        }

        public long LimitSize
        {
            get { return _limitSize; }
            set
            {
                _limitSize = value;
                NotifyPropertyChanged("LimitSize");
            }
        }

        public long LowerSize
        {
            get { return _lowerSize; }
            set
            {
                _lowerSize = value;
                NotifyPropertyChanged("LowerSize");
            }
        }

        public byte[] Hash
        {
            get { return _hash; }
            set
            {
                _hash = value;
                NotifyPropertyChanged("Hash");
            }
        }

        public bool Effect
        {
            get { return _effect; }
            set
            {
                _effect = value;
                NotifyPropertyChanged("Effect");
            }
        }
    }
}
