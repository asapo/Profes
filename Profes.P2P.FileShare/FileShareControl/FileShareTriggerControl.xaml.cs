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
using System.Collections;
using Profes.P2P.FileShare.Properties;
using System.Threading;
using Profes.P2P.FileShare.ServiceModel;
using System.Text.RegularExpressions;
using Profes.BinaryEditorBase;
using Profes.Security.Cryptography;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Threading;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FileShareTriggerControl.xaml の相互作用ロジック
    /// </summary>
    public partial class FileShareTriggerControl : UserControl
    {
        private Timer _triggerTimer;

        public FileShareTriggerControl()
        {
            InitializeComponent();

            _triggerTimer = new Timer(new TimerCallback(_triggerTimer_Clock), null, 0, 60 * 1000);
        }

        private void _triggerTimer_Clock(object sender)
        {
            foreach (CacheListViewItem c in FileShareFilterControl.CacheListViewItemListFilter(Settings.Default._keyController.CacheListViewItemList))
            {
                lock (Settings.Default.DownloadHistory)
                {
                    if (Settings.Default.DownloadHistory.Any(n => BinaryEditor.ArrayEquals(n.Hash, c.Hash))) continue;
                }
                var tlist = Settings.Default._triggerList.Where(n => n.Effect == true).ToArray();

                foreach (Trigger t in tlist)
                {
                    bool? flag = null;

                    // ファイル名の比較
                    if (t.Name.Trim() != "")
                    {
                        if (flag == null) flag = true;

                        if (t.Name.Contains("\""))
                        {
                            foreach (Match m in Regex.Matches(t.Name, "\"(.*?)\""))
                            {
                                string mat = m.Value.Trim('\"');
                                flag &= ((!mat.StartsWith("-") && c.Name.Contains(mat)) || (mat.StartsWith("-") && !c.Name.Contains(mat.Substring(1))));
                            }
                        }
                        else
                        {
                            flag &= c.Name.Contains(t.Name);
                        }
                    }

                    // カテゴリの比較
                    if (t.Category != null)
                    {
                        if (flag == null) flag = true;

                        foreach (string ss in t.Category)
                        {
                            if (ss != null && ss.Trim() != "")
                            {
                                flag &= c.Category.Any(n => n != null && n.ToLower() == ss.ToLower());
                            }
                        }
                    }

                    // IDの比較
                    if (t.ID != null && t.ID.Trim() != "")
                    {
                        if (flag == null) flag = true;

                        flag &= t.ID == Convert.ToBase64String(HashFunction.HashCreate(c.PublicKey));
                    }

                    // ハッシュの比較
                    if (t.Hash != null && t.Hash.Length != 0)
                    {
                        if (flag == null) flag = true;

                        flag &= BinaryEditor.ArrayEquals(t.Hash, c.Hash);
                    }

                    // サイズ上限
                    if (t.LimitSize != 0)
                    {
                        if (flag == null) flag = true;

                        flag &= t.LimitSize > c.Size;
                    }

                    // サイズ下限
                    if (t.LowerSize != 0)
                    {
                        if (flag == null) flag = true;

                        flag &= t.LowerSize < c.Size;
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        if (flag == true && !Settings.Default._downloadList.Any(n => BinaryEditor.ArrayEquals(n.Hash, c.Hash)))
                        {
                            Settings.Default._downloadList.Add(c);
                            if (t.Remove == true) Settings.Default._triggerList.Remove(t);
                        }
                    }));
                }
            }
        }

        private void 追加_J_Click(object sender, RoutedEventArgs e)
        {
            using (TriggerSettingWindow triggerSettingWindow = new TriggerSettingWindow())
            {
                triggerSettingWindow.ShowDialog();
            }
        }

        private void 編集_E_Click(object sender, RoutedEventArgs e)
        {
            var item = TriggerListView.SelectedItem as Trigger;

            if (item != null)
            {
                using (TriggerSettingWindow triggerSettingWindow = new TriggerSettingWindow())
                {
                    triggerSettingWindow.SetTrigger(item);
                    triggerSettingWindow.ShowDialog();

                    if (triggerSettingWindow.DialogResult == true)
                        Settings.Default._triggerList.Remove(item);
                }
            }
        }

        private void 削除_D_Click(object sender, RoutedEventArgs e)
        {
            var items = TriggerListView.SelectedItems as IList;

            if (items != null)
            {
                foreach (Trigger cl in items.OfType<Trigger>().ToArray())
                {
                    Settings.Default._triggerList.Remove(cl);
                }
            }
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {

        }
    }

    [Serializable]
    public class Trigger : INotifyPropertyChanged, ISerializable
    {
        string _name;
        string[] _category;
        string _id;
        long _limitSize;
        long _lowerSize;
        byte[] _hash;
        bool _effect;
        bool _remove;

        public Trigger() { }

        protected Trigger(SerializationInfo info, StreamingContext context)
        {
            Name = (string)info.GetValue("Name", typeof(string));
            Category = (string[])info.GetValue("Category", typeof(string[]));
            ID = (string)info.GetValue("ID", typeof(string));
            LimitSize = (long)info.GetValue("LimitSize", typeof(long));
            LowerSize = (long)info.GetValue("LowerSize", typeof(long));
            Effect = (bool)info.GetValue("Effect", typeof(bool));
            Remove = (bool)info.GetValue("Remove", typeof(bool));
        }

        new public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name, typeof(string));
            info.AddValue("Category", Category, typeof(string[]));
            info.AddValue("ID", ID, typeof(string));
            info.AddValue("LimitSize", LimitSize, typeof(long));
            info.AddValue("LowerSize", LowerSize, typeof(long));
            info.AddValue("Effect", Effect, typeof(bool));
            info.AddValue("Remove", Remove, typeof(bool));
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

        public bool Remove
        {
            get { return _remove; }
            set
            {
                _remove = value;
                NotifyPropertyChanged("Remove");
            }
        }
    }
}