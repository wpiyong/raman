using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SpinnakerNET;
using SpinnakerNET.GUI.WPFControls;

namespace RamanMapping.View
{
    /// <summary>
    /// Interaction logic for CameraSettings.xaml
    /// </summary>
    public partial class CameraSettings : Window
    {
        PropertyGridControl gridControl;

        public CameraSettings(IManagedCamera cam)
        {
            InitializeComponent();

            gridControl = new PropertyGridControl();

            Grid.SetRow(gridControl, 1);

            LayoutLeft.Children.Add(gridControl);

            SetCamera(cam);
        }

        /// <summary>
        /// Connect ImageDrawingControl and PropertyGridControl with IManagedCamera
        /// </summary>
        /// <param name="cam"></param>
        void SetCamera(IManagedCamera cam, bool startStreaming = false)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                //cam.Init();
                gridControl.Connect(cam);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("There was a problem connecting to IManagedCamera.\n{0}", ex.Message));
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void WindowClosing(object sender, EventArgs e)
        {
            try // Disconnect any connected component
            {
                gridControl.Disconnect();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
        }
    }
}
