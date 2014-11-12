using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace My_Sync.Classes
{
    class SynchronizationPoint
    {
        private Image serverType;
        private string description = "";
        private string server = "";

        #region Getter / Setter

        public Image ServerType
        {
            get { return serverType; }
            set { serverType = value; }
        }

        public string Server
        {
            get { return server; }
            set { server = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        #endregion

        /// <summary>
        /// Gets the image (embedded resource) of the given filename
        /// </summary>
        /// <param name="imageName">image which should get retrieved</param>
        /// <param name="size">sets the size (width & height) of the image</param>
        /// <returns>image object containing the image of the resources</returns>
        public Image GetImageOfAssembly(string imageName, int size = 7)
        {
            using (new Logger(imageName, size))
            {
                MainWindow mainWindow = ((MainWindow)Application.Current.MainWindow);
                string resourceString = String.Format("{0}.Images.ServerType.{1}", mainWindow.GetType().Namespace, imageName);

                Image image = new Image { Margin = new Thickness(1), Width = size, Height = size };
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceString);
                bi.EndInit();
                image.Source = bi;
                return image;
            }
        } 
    }
}
