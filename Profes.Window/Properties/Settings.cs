using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Profes.P2P.FileShare.ServiceModel;
using Profes.P2P.FileShare.FileShareControl;
using System.Windows.Controls;
using System.Collections;
using System.Windows;
using System.IO;
using System.Globalization;

namespace Profes.Window.Properties
{
    class Settings : Profes.Configuration.SettingsBase
    {
        private static Settings defaultInstance = ((Settings)(Profes.Configuration.SettingsBase.Synchronized(new Settings())));

        Settings()
            : base()
        {
            Window_Height = 800;
            Window_Width = 1000;
            Window_Top = 0;
            Window_Left = 0;
            Window_WindowState = WindowState.Normal;
            CultureComboBox_SelectedItem = new System.Globalization.CultureInfo("ja-JP");
            TaskTrayCheckBox_IsChecked = false;
            ShutdownQuestionCheckBox_IsChecked = false;
            Ribbon_IsMinimized = true;

            try
            {
                Load();
            }
            catch { }
        }

        public void Load()
        {
            base.Load("Profes.Window.exe.2.config");
        }

        public void Save()
        {
            base.Save("Profes.Window.exe.2.config");
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

        public double Window_Height
        {
            get { return ((double)(this["Window_Height"])); }
            set { this["Window_Height"] = value; }
        }

        public double Window_Width
        {
            get { return ((double)(this["Window_Width"])); }
            set { this["Window_Width"] = value; }
        }

        public double Window_Top
        {
            get { return ((double)(this["Window_Top"])); }
            set { this["Window_Top"] = value; }
        }

        public double Window_Left
        {
            get { return ((double)(this["Window_Left"])); }
            set { this["Window_Left"] = value; }
        }

        public WindowState Window_WindowState
        {
            get { return ((WindowState)(this["Window_WindowState"])); }
            set { this["Window_WindowState"] = value; }
        }

        public CultureInfo CultureComboBox_SelectedItem
        {
            get { return ((CultureInfo)(this["CultureComboBox_SelectedItem"])); }
            set { this["CultureComboBox_SelectedItem"] = value; }
        }

        public string SkinComboBox_SelectedItem
        {
            get { return ((string)(this["SkinComboBox_SelectedItem"])); }
            set { this["SkinComboBox_SelectedItem"] = value; }
        }

        public bool TaskTrayCheckBox_IsChecked
        {
            get
            {
                bool? b = (bool?)this["TaskTrayCheckBox_IsChecked"];
                if (b != null)
                    return ((bool)(b));
                else
                    return false;
            }
            set { this["TaskTrayCheckBox_IsChecked"] = value; }
        }

        public bool ShutdownQuestionCheckBox_IsChecked
        {
            get
            {
                bool? b = (bool?)this["ShutdownQuestionCheckBox_IsChecked"];
                if (b != null)
                    return ((bool)(b));
                else
                    return false;
            }
            set { this["ShutdownQuestionCheckBox_IsChecked"] = value; }
        }

        public bool Ribbon_IsMinimized
        {
            get
            {
                bool? b = (bool?)this["Ribbon_IsMinimized"];
                if (b != null)
                    return ((bool)(b));
                else
                    return false;
            }
            set { this["Ribbon_IsMinimized"] = value; }
        }
    }
}