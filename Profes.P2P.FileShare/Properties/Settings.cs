using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ComponentModel;
using Profes.P2P.FileShare.ServiceModel;
using Profes.P2P.FileShare.FileShareControl;
using System.Windows.Controls;
using System.Collections;
using System.Windows;
using System.IO;

namespace Profes.P2P.FileShare.Properties
{
    class Settings : Profes.Configuration.SettingsBase
    {
        private static Settings defaultInstance = ((Settings)(Profes.Configuration.SettingsBase.Synchronized(new Settings())));

        Settings()
            : base()
        {
            FileShareFilter_GridViewColumn_Name_Width = 100;
            FileShareFilter_GridViewColumn_Category_Width = 100;
            FileShareFilter_GridViewColumn_ID_Width = 100;
            FileShareFilter_GridViewColumn_LimitSize_Width = 100;
            FileShareFilter_GridViewColumn_LowerSize_Width = 100;
            FileShareFilter_GridViewColumn_Hash_Width = 100;

            FileShareTrigger_GridViewColumn_Name_Width = 100;
            FileShareTrigger_GridViewColumn_Category_Width = 100;
            FileShareTrigger_GridViewColumn_ID_Width = 100;
            FileShareTrigger_GridViewColumn_LimitSize_Width = 100;
            FileShareTrigger_GridViewColumn_LowerSize_Width = 100;
            FileShareTrigger_GridViewColumn_Hash_Width = 100;

            FileShareNode_GridViewColumn_CommunicationType_Width = 100;
            FileShareNode_GridViewColumn_Node_Width = 100;
            FileShareNode_GridViewColumn_Description_Width = 100;
            FileShareNode_GridViewColumn_ConnectionTime_Width = 100;

            GridViewColumn_Filename_Width = 100;
            GridViewColumn_ID_Width = 100;
            GridViewColumn_Size_Width = 100;
            GridViewColumn_Status_Width = 100;
            GridViewColumn_Hash_Width = 100;

            Grid_ColumnDefinitions_Width = new GridLength(207);
            DownloadDirectoryPath = "";
            IsServiceStarted = false;

            Sign = "";
            PrivateKey = "";
            PublicKey = "";

            Query_GridViewColumn_Filename_Width = 100;
            Query_GridViewColumn_CategoryList_Width = 100;
            Query_GridViewColumn_Sign_Width = 100;
            Query_GridViewColumn_ID_Width = 100;
            Query_GridViewColumn_Size_Width = 100;
            Query_GridViewColumn_Status_Width = 100;
            Query_GridViewColumn_CreationTime_Width = 100;
            Query_GridViewColumn_Hash_Width = 100;
            Query_GridViewColumn_Review_Width = 100;

            RefineSearchTreeViewItemSettingWindow_GridViewColumn_Include_Width = 100;
            RefineSearchTreeViewItemSettingWindow_GridViewColumn_Value_Width = 100;

            _cacheController = new CacheController();
            _routeTable = new RouteTable();
            _keyController = new KeyController();

            TabRadioButton = false;
            TreeRadioButton = true;

            DisplaySize_1 = true;
            DisplaySize_2 = false;
            DisplaySize_3 = false;

            QueryTimerMaxCount = 2;
            StoreTimerMaxCount = 2;
            DownloadTimerMaxCount = 20;
            UploadTimerMaxCount = 2;

            SelectedIndexCategory = -1;
            _categoryDir = new Dictionary<string, string[]>();
            _categoryList = new BindingList<string>();
            _filterList = new BindingList<Profes.P2P.FileShare.FileShareControl.Filter>();
            _triggerList = new BindingList<Profes.P2P.FileShare.FileShareControl.Trigger>();
            _nodeShowList = new BindingList<NodeListViewItem>();
            UploadDiffusionList = new List<UploadDiffusion>();
            _downloadList = new BindingList<CacheListViewItem>();
            _uploadList = new BindingList<CacheListViewItem>();
            SearchTreeViewItem = new RefineSearchTreeViewItem { RefineSearchName = "Search", IsExpanded = true, };
            QueryList = new string[] { "test" };

            CommentsWindow_Height = 358;
            CommentsWindow_Width = 727;

            DownloadHistory = new List<CacheListViewItem>();

            ConnectionType = ConnectionType.Direct;

            DirectConnectionInformation = new DirectConnectionInformation()
            {
                IPAddress = "",
                Port = new Random().Next(1024, 65536),
            };

            UpnpConnectionInformation = new UpnpConnectionInformation()
            {
                GlobalIPAddress = "",
                MachineIPAddress = "",
                GatewayIPAddress = "",
                ExternalPort = new Random().Next(1024, 65536),
                InternalPort = new Random().Next(1024, 65536),
            };

            OtherConnectionInformation = new OtherConnectionInformation()
            {
                IPAddress = "",
                Port = new Random().Next(1024, 65536),
            };

            LogCheckBox_IsChecked = false;

            try
            {
                Load();
            }
            catch { }
        }

        public void Load()
        {
            base.Load("Profes.P2P.FileShare.dll.config");

            var KeyList = (string[])this["_categoryList_Keys"];
            var ValueList = (string[][])this["_categoryList_Values"];

            for (int i = 0; i < KeyList.Length; i++)
            {
                _categoryDir.Add(KeyList[i], ValueList[i]);
            }

            foreach (string c in (string[])this["_categoryList"])
            {
                _categoryList.Add(c);
            }

            foreach (Profes.P2P.FileShare.FileShareControl.Filter c in (Profes.P2P.FileShare.FileShareControl.Filter[])this["_filterList"])
            {
                _filterList.Add(c);
            }

            foreach (Profes.P2P.FileShare.FileShareControl.Trigger c in (Profes.P2P.FileShare.FileShareControl.Trigger[])this["_triggerList"])
            {
                _triggerList.Add(c);
            }

            foreach (var c in (UploadDiffusion[])this["UploadDiffusionList"])
            {
                UploadDiffusionList.Add(c);
            }

            foreach (var c in (CacheListViewItem[])this["_downloadList"])
            {
                _downloadList.Add(c);
            }

            foreach (var c in (CacheListViewItem[])this["_uploadList"])
            {
                _uploadList.Add(c);
            }

            Grid_ColumnDefinitions_Width = new System.Windows.GridLength((double)this["Grid_ColumnDefinitions_Width"]);

            foreach (var c in (CacheListViewItem[])this["DownloadHistory"])
            {
                DownloadHistory.Add(c);
            }
        }

        public void Save()
        {
            string[] KeyList = new string[_categoryDir.Count];
            string[][] ValueList = new string[_categoryDir.Count][];

            int i = 0;
            foreach (string key in _categoryDir.Keys)
            {
                KeyList[i] = key;
                ValueList[i] = _categoryDir[key].ToArray();
                i++;
            }

            this["_categoryList_Keys"] = KeyList.ToArray();
            this["_categoryList_Values"] = ValueList.ToArray();

            this["_categoryList"] = _categoryList.ToArray();
            this["_filterList"] = _filterList.ToArray();
            this["_triggerList"] = _triggerList.ToArray();
            this["UploadDiffusionList"] = UploadDiffusionList.ToArray();
            this["_downloadList"] = _downloadList.ToArray();
            this["_uploadList"] = _uploadList.ToArray();
            this["Grid_ColumnDefinitions_Width"] = Grid_ColumnDefinitions_Width.Value;
            this["DownloadHistory"] = DownloadHistory.ToArray();

            base.Save("Profes.P2P.FileShare.dll.config");
        }

        public static Settings Default
        {
            get
            {
                lock (defaultInstance)
                {
                    return defaultInstance;
                }
            }
        }

        public ServiceHost _serviceHost { get; set; }

        public Dictionary<string, string[]> _categoryDir { get; set; }

        public BindingList<string> _categoryList { get; set; }
        public BindingList<Profes.P2P.FileShare.FileShareControl.Filter> _filterList { get; set; }
        public BindingList<Profes.P2P.FileShare.FileShareControl.Trigger> _triggerList { get; set; }
        public BindingList<NodeListViewItem> _nodeShowList { get; set; }
        public List<UploadDiffusion> UploadDiffusionList { get; set; }
        public BindingList<CacheListViewItem> _downloadList { get; set; }
        public BindingList<CacheListViewItem> _uploadList { get; set; }
        public System.Windows.GridLength Grid_ColumnDefinitions_Width { get; set; }
        public List<CacheListViewItem> DownloadHistory { get; set; }

        public RefineSearchTreeViewItem SearchTreeViewItem
        {
            get { return ((RefineSearchTreeViewItem)(this["SearchTreeViewItem"])); }
            set { this["SearchTreeViewItem"] = value; }
        }

        public int SelectedIndexCategory
        {
            get
            {
                var i = this["SelectedIndexCategory"] as int?;
                if (i != null) return (int)i;
                else return -1;
            }
            set { this["SelectedIndexCategory"] = value; }
        }

        public bool DisplaySize_1
        {
            get
            {
                bool? b = (bool?)this["DisplaySize_1"];
                if (b != null)
                    return ((bool)(b));
                else
                    return false;
            }
            set { this["DisplaySize_1"] = value; }
        }

        public bool DisplaySize_2
        {
            get
            {
                bool? b = (bool?)this["DisplaySize_2"];
                if (b != null)
                    return ((bool)(b));
                else
                    return false;
            }
            set { this["DisplaySize_2"] = value; }
        }

        public bool DisplaySize_3
        {
            get
            {
                bool? b = (bool?)this["DisplaySize_3"];
                if (b != null)
                    return ((bool)(b));
                else
                    return false;
            }
            set { this["DisplaySize_3"] = value; }
        }

        public bool TreeRadioButton
        {
            get
            {
                bool? b = (bool?)this["TreeRadioButton"];
                if (b != null)
                    return ((bool)(b));
                else
                    return false;
            }
            set { this["TreeRadioButton"] = value; }
        }

        public bool TabRadioButton
        {
            get
            {
                bool? b = (bool?)this["TabRadioButton"];
                if (b != null)
                    return ((bool)(b));
                else
                    return false;
            }
            set { this["TabRadioButton"] = value; }
        }

        public bool LogCheckBox_IsChecked
        {
            get
            {
                bool? b = (bool?)this["LogCheckBox_IsChecked"];
                if (b != null)
                    return ((bool)(b));
                else
                    return false;
            }
            set { this["LogCheckBox_IsChecked"] = value; }
        }

        public string[] TabQueryList
        {
            get { return ((string[])(this["TabQueryList"])); }
            set { this["TabQueryList"] = value; }
        }

        public string[] QueryList
        {
            get { return ((string[])(this["QueryList"])); }
            set { this["QueryList"] = value; }
        }

        public int StoreTimerMaxCount
        {
            get
            {
                int? b = (int?)this["StoreTimerMaxCount"];
                if (b != null)
                    return ((int)(b));
                else
                    return 0;
            }
            set { this["StoreTimerMaxCount"] = value; }
        }

        public int QueryTimerMaxCount
        {
            get
            {
                int? b = (int?)this["QueryTimerMaxCount"];
                if (b != null)
                    return ((int)(b));
                else
                    return 0;
            }
            set { this["QueryTimerMaxCount"] = value; }
        }

        public int DownloadTimerMaxCount
        {
            get
            {
                int? b = (int?)this["DownloadTimerMaxCount"];
                if (b != null)
                    return ((int)(b));
                else
                    return 0;
            }
            set { this["DownloadTimerMaxCount"] = value; }
        }

        public int UploadTimerMaxCount
        {
            get
            {
                int? b = (int?)this["UploadTimerMaxCount"];
                if (b != null)
                    return ((int)(b));
                else
                    return 0;
            }
            set { this["UploadTimerMaxCount"] = value; }
        }

        public double CommentsWindow_Height
        {
            get { return ((double)(this["CommentsWindow_Height"])); }
            set { this["CommentsWindow_Height"] = value; }
        }

        public double CommentsWindow_Width
        {
            get { return ((double)(this["CommentsWindow_Width"])); }
            set { this["CommentsWindow_Width"] = value; }
        }

        public double RefineSearchTreeViewItemSettingWindow_GridViewColumn_Include_Width
        {
            get { return ((double)(this["RefineSearchTreeViewItemSettingWindow_GridViewColumn_Include_Width"])); }
            set { this["RefineSearchTreeViewItemSettingWindow_GridViewColumn_Include_Width"] = value; }
        }
        public double RefineSearchTreeViewItemSettingWindow_GridViewColumn_Value_Width
        {
            get { return ((double)(this["RefineSearchTreeViewItemSettingWindow_GridViewColumn_Value_Width"])); }
            set { this["RefineSearchTreeViewItemSettingWindow_GridViewColumn_Value_Width"] = value; }
        }

        public double FileShareFilter_GridViewColumn_Name_Width
        {
            get { return ((double)(this["FileShareFilter_GridViewColumn_Name_Width"])); }
            set { this["FileShareFilter_GridViewColumn_Name_Width"] = value; }
        }
        public double FileShareFilter_GridViewColumn_Category_Width
        {
            get { return ((double)(this["FileShareFilter_GridViewColumn_Category_Width"])); }
            set { this["FileShareFilter_GridViewColumn_Category_Width"] = value; }
        }
        public double FileShareFilter_GridViewColumn_ID_Width
        {
            get { return ((double)(this["FileShareFilter_GridViewColumn_ID_Width"])); }
            set { this["FileShareFilter_GridViewColumn_ID_Width"] = value; }
        }
        public double FileShareFilter_GridViewColumn_LimitSize_Width
        {
            get { return ((double)(this["FileShareFilter_GridViewColumn_LimitSize_Width"])); }
            set { this["FileShareFilter_GridViewColumn_LimitSize_Width"] = value; }
        }
        public double FileShareFilter_GridViewColumn_LowerSize_Width
        {
            get { return ((double)(this["FileShareFilter_GridViewColumn_LowerSize_Width"])); }
            set { this["FileShareFilter_GridViewColumn_LowerSize_Width"] = value; }
        }
        public double FileShareFilter_GridViewColumn_Hash_Width
        {
            get { return ((double)(this["FileShareFilter_GridViewColumn_Hash_Width"])); }
            set { this["FileShareFilter_GridViewColumn_Hash_Width"] = value; }
        }

        public double FileShareTrigger_GridViewColumn_Name_Width
        {
            get { return ((double)(this["FileShareTrigger_GridViewColumn_Name_Width"])); }
            set { this["FileShareTrigger_GridViewColumn_Name_Width"] = value; }
        }
        public double FileShareTrigger_GridViewColumn_Category_Width
        {
            get { return ((double)(this["FileShareTrigger_GridViewColumn_Category_Width"])); }
            set { this["FileShareTrigger_GridViewColumn_Category_Width"] = value; }
        }
        public double FileShareTrigger_GridViewColumn_ID_Width
        {
            get { return ((double)(this["FileShareTrigger_GridViewColumn_ID_Width"])); }
            set { this["FileShareTrigger_GridViewColumn_ID_Width"] = value; }
        }
        public double FileShareTrigger_GridViewColumn_LimitSize_Width
        {
            get { return ((double)(this["FileShareTrigger_GridViewColumn_LimitSize_Width"])); }
            set { this["FileShareTrigger_GridViewColumn_LimitSize_Width"] = value; }
        }
        public double FileShareTrigger_GridViewColumn_LowerSize_Width
        {
            get { return ((double)(this["FileShareTrigger_GridViewColumn_LowerSize_Width"])); }
            set { this["FileShareTrigger_GridViewColumn_LowerSize_Width"] = value; }
        }
        public double FileShareTrigger_GridViewColumn_Hash_Width
        {
            get { return ((double)(this["FileShareTrigger_GridViewColumn_Hash_Width"])); }
            set { this["FileShareTrigger_GridViewColumn_Hash_Width"] = value; }
        }

        public double FileShareNode_GridViewColumn_CommunicationType_Width
        {
            get { return ((double)(this["FileShareNode_GridViewColumn_CommunicationType_Width"])); }
            set { this["FileShareNode_GridViewColumn_CommunicationType_Width"] = value; }
        }

        public double FileShareNode_GridViewColumn_Node_Width
        {
            get { return ((double)(this["FileShareNode_GridViewColumn_Node_Width"])); }
            set { this["FileShareNode_GridViewColumn_Node_Width"] = value; }
        }

        public double FileShareNode_GridViewColumn_Description_Width
        {
            get { return ((double)(this["FileShareNode_GridViewColumn_Description_Width"])); }
            set { this["FileShareNode_GridViewColumn_Description_Width"] = value; }
        }

        public double FileShareNode_GridViewColumn_ConnectionTime_Width
        {
            get { return ((double)(this["FileShareNode_GridViewColumn_ConnectionTime_Width"])); }
            set { this["FileShareNode_GridViewColumn_ConnectionTime_Width"] = value; }
        }

        public double GridViewColumn_Filename_Width
        {
            get { return ((double)(this["GridViewColumn_Filename_Width"])); }
            set { this["GridViewColumn_Filename_Width"] = value; }
        }

        public double GridViewColumn_ID_Width
        {
            get { return ((double)(this["GridViewColumn_ID_Width"])); }
            set { this["GridViewColumn_ID_Width"] = value; }
        }

        public double GridViewColumn_Size_Width
        {
            get { return ((double)(this["GridViewColumn_Size_Width"])); }
            set { this["GridViewColumn_Size_Width"] = value; }
        }

        public double GridViewColumn_Status_Width
        {
            get { return ((double)(this["GridViewColumn_Status_Width"])); }
            set { this["GridViewColumn_Status_Width"] = value; }
        }

        public double GridViewColumn_Hash_Width
        {
            get { return ((double)(this["GridViewColumn_Hash_Width"])); }
            set { this["GridViewColumn_Hash_Width"] = value; }
        }

        public string DownloadDirectoryPath
        {
            get { return ((string)(this["DownloadDirectoryPath"])); }
            set { this["DownloadDirectoryPath"] = value; }
        }

        public bool IsServiceStarted
        {
            get { return ((bool)(this["IsServiceStarted"])); }
            set { this["IsServiceStarted"] = value; }
        }

        public string Sign
        {
            get { return ((string)(this["Sign"])); }
            set { this["Sign"] = value; }
        }

        public string PrivateKey
        {
            get { return ((string)(this["PrivateKey"])); }
            set { this["PrivateKey"] = value; }
        }

        public string PublicKey
        {
            get { return ((string)(this["PublicKey"])); }
            set { this["PublicKey"] = value; }
        }

        public double Query_GridViewColumn_Filename_Width
        {
            get { return ((double)(this["Query_GridViewColumn_Filename_Width"])); }
            set { this["Query_GridViewColumn_Filename_Width"] = value; }
        }

        public double Query_GridViewColumn_CategoryList_Width
        {
            get { return ((double)(this["Query_GridViewColumn_CategoryList_Width"])); }
            set { this["Query_GridViewColumn_CategoryList_Width"] = value; }
        }

        public double Query_GridViewColumn_Sign_Width
        {
            get { return ((double)(this["Query_GridViewColumn_Sign_Width"])); }
            set { this["Query_GridViewColumn_Sign_Width"] = value; }
        }

        public double Query_GridViewColumn_ID_Width
        {
            get { return ((double)(this["Query_GridViewColumn_ID_Width"])); }
            set { this["Query_GridViewColumn_ID_Width"] = value; }
        }

        public double Query_GridViewColumn_Size_Width
        {
            get { return ((double)(this["Query_GridViewColumn_Size_Width"])); }
            set { this["Query_GridViewColumn_Size_Width"] = value; }
        }

        public double Query_GridViewColumn_DownloadRate_Width
        {
            get { return ((double)(this["Query_GridViewColumn_DownloadRate_Width"])); }
            set { this["Query_GridViewColumn_DownloadRate_Width"] = value; }
        }

        public double Query_GridViewColumn_Status_Width
        {
            get { return ((double)(this["Query_GridViewColumn_Status_Width"])); }
            set { this["Query_GridViewColumn_Status_Width"] = value; }
        }

        public double Query_GridViewColumn_CreationTime_Width
        {
            get { return ((double)(this["Query_GridViewColumn_CreationTime_Width"])); }
            set { this["Query_GridViewColumn_CreationTime_Width"] = value; }
        }

        public double Query_GridViewColumn_Hash_Width
        {
            get { return ((double)(this["Query_GridViewColumn_Hash_Width"])); }
            set { this["Query_GridViewColumn_Hash_Width"] = value; }
        }

        public double Query_GridViewColumn_Review_Width
        {
            get { return ((double)(this["Query_GridViewColumn_Review_Width"])); }
            set { this["Query_GridViewColumn_Review_Width"] = value; }
        }

        public string CacheDirectoryPath
        {
            get { return ((string)(this["CacheDirectoryPath"])); }
            set { this["CacheDirectoryPath"] = value; }
        }

        public CacheController _cacheController
        {
            get { return ((CacheController)(this["_cacheController"])); }
            set { this["_cacheController"] = value; }
        }

        public RouteTable _routeTable
        {
            get { return ((RouteTable)(this["_routeTable"])); }
            set { this["_routeTable"] = value; }
        }

        public KeyController _keyController
        {
            get { return (KeyController)(this["_keyController"]); }
            set { this["_keyController"] = value; }
        }

        public ConnectionType ConnectionType
        {
            get { return (ConnectionType)(this["ConnectionType"]); }
            set { this["ConnectionType"] = value; }
        }

        public DirectConnectionInformation DirectConnectionInformation
        {
            get { return (DirectConnectionInformation)(this["DirectConnectionInformation"]); }
            set { this["DirectConnectionInformation"] = value; }
        }

        public UpnpConnectionInformation UpnpConnectionInformation
        {
            get { return (UpnpConnectionInformation)(this["UpnpConnectionInformation"]); }
            set { this["UpnpConnectionInformation"] = value; }
        }

        public OtherConnectionInformation OtherConnectionInformation
        {
            get { return (OtherConnectionInformation)(this["OtherConnectionInformation"]); }
            set { this["OtherConnectionInformation"] = value; }
        }
    }
}