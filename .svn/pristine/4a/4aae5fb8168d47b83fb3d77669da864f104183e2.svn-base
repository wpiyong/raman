using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using RamanMapping.ViewModel;
using RamanMapping.Model;

namespace RamanMapping
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    base.OnStartup(e);

        //    if (LoadSettings())
        //    {
        //        // Create MainWindow View
        //        MainWindow mainWindowVw = new MainWindow();
                
        //        // Create MainWindow ViewModel
        //        var mainWindowVM = new MainWindowVM();

        //        // Set MainWindow View datacontext to MainWindow ViewModel and then show the window
        //        mainWindowVw.DataContext = mainWindowVM;
        //        mainWindowVw.Closing += mainWindowVM.OnWindowClosing;
        //        mainWindowVw.Loaded += mainWindowVM.OnViewLoaded;

        //        mainWindowVw.Show();
        //    } else
        //    {
        //        MessageBox.Show("An error occurred before the application could start.\n\nCould not load settings. ",
        //            "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Stop);
        //        Shutdown();
        //    }
        //}

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (LoadSettings())
            {
                // Create MainWindow View
                MainWindow mainWindowVw = new MainWindow();

                // Create MainWindow ViewModel
                var mainWindowVM = new MainWindowVM();

                // Set MainWindow View datacontext to MainWindow ViewModel and then show the window
                mainWindowVw.DataContext = mainWindowVM;
                mainWindowVw.Closing += mainWindowVM.OnWindowClosing;
                mainWindowVw.Loaded += mainWindowVM.OnViewLoaded;
                mainWindowVw.Activated += mainWindowVM.OnActivated;
                mainWindowVw.Show();
            }
            else
            {
                MessageBox.Show("An error occurred before the application could start.\n\nCould not load settings. ",
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                Shutdown();
            }
        }

        private void Application_Activated(object sender, EventArgs e)
        {
            Console.WriteLine("Application_Activated");
        }

        private bool LoadSettings()
        {
            return GlobalVariables.motorSettings.Load() && GlobalVariables.spectrometerSettings.Load();
        }

        private void Application_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show(e.Exception.ToString(), "Fatal Error, exiting...");
            e.Handled = true;
            App.Current.Shutdown();
        }
    }

    public class GlobalVariables
    {
        public static MotorSettings motorSettings = new MotorSettings();
        public static SpectrometerSettings spectrometerSettings = new SpectrometerSettings();
    }
}
