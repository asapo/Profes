using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Threading;
using System.IO;
using System.Windows.Data;

namespace Profes.Window
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private static Mutex mutex;

        App()
        {
            string[] args = Environment.GetCommandLineArgs();

            string mutex_name = args[0];
            mutex = new Mutex(false, mutex_name.Replace(@"\", "|"));

            if (!mutex.WaitOne(0, false))
            {
                System.Windows.MessageBox.Show("Profesが多重起動されています。", "Profes");
                this.Shutdown();
            }

            //this.StartupUri = new Uri("MainWindow.xaml", System.UriKind.Relative);
        }
    }
}