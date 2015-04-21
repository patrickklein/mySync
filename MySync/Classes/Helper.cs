using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace My_Sync.Classes
{
    static class Helper
    {
        /// <summary>
        /// Method for extract files from the embedded resources of this application
        /// </summary>
        /// <param name="extension">files to extract</param>
        public static void ExtractEmbeddedResource(string extension)
        {
            using (new Logger(extension))
            {
                //Get current assembly reference and all embedded resources
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                string[] arrResources = currentAssembly.GetManifestResourceNames();

                foreach (string resourceName in arrResources)
                {
                    if (resourceName.EndsWith(extension))
                    {
                        //Name of the file saved on disk
                        MainWindow mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                        string saveAsName = resourceName.Replace(mainWindow.GetType().Namespace, "").TrimStart('.').Replace("Resources.", "");
                        string executablePath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

                        FileInfo fileInfoOutputFile = new FileInfo(Path.Combine(executablePath, saveAsName));

                        if (fileInfoOutputFile.Exists) continue;

                        foreach (Process proc in Process.GetProcessesByName(fileInfoOutputFile.Name.Replace(".exe", "")))
                            proc.Kill();

                        //open newly creating file for writing and get the stream to the resources
                        FileStream streamToOutputFile = fileInfoOutputFile.OpenWrite();
                        Stream streamToResourceFile = currentAssembly.GetManifestResourceStream(resourceName);

                        //save resource to folder
                        const int size = 4096;
                        byte[] bytes = new byte[4096];
                        int numBytes;
                        while ((numBytes = streamToResourceFile.Read(bytes, 0, size)) > 0)
                            streamToOutputFile.Write(bytes, 0, numBytes);

                        streamToOutputFile.Close();
                        streamToResourceFile.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the bitmap image (embedded resource) of the given filename
        /// </summary>
        /// <param name="imageName">image which should get retrieved</param>
        /// <param name="extension">file extension of the image resource</param>
        /// <param name="size">sets the size (width & height) of the image</param>
        /// <returns>image object containing the image of the resources</returns>
        public static BitmapImage GetBitmapImageOfAssembly(string imageName, string extension = ".png", int size = 7)
        {
            using (new Logger(imageName, extension, size))
            {
                MainWindow mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                string resourceString = String.Format("{0}.Images.ServerType.{1}{2}", mainWindow.GetType().Namespace, imageName, extension);
                Stream imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceString);

                if (imageStream == null) return null;

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = imageStream;
                bi.EndInit();
                return bi;
            }
        }

        /// <summary>
        /// Gets the image (embedded resource) of the given filename
        /// </summary>
        /// <param name="imageName">image which should get retrieved</param>
        /// <param name="extension">file extension of the image resource</param>
        /// <param name="size">sets the size (width & height) of the image</param>
        /// <returns>image object containing the image of the resources</returns>
        public static Image GetImageOfAssembly(string imageName, string extension = ".png", int size = 7)
        {
            using (new Logger(imageName, extension, size))
            {
                Image image = new Image { Margin = new Thickness(1), Width = size, Height = size };
                image.Source = GetBitmapImageOfAssembly(imageName, extension, size);
                image.Name = imageName;
                return image;
            }
        }
    }

    class SynchronizationPoint
    {
        private Image serverType;
        private string description = "";
        private string folder = "";
        private string server = "";

        #region Getter / Setter

        public Image ServerType
        {
            get { return serverType; }
            set { serverType = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public string Folder
        {
            get { return folder; }
            set { folder = value; }
        }

        public string Server
        {
            get { return server; }
            set { server = value; }
        }

        #endregion
    }

    class ServerTypeImage
    {
        private int id;
        private string serverType;
        private BitmapImage image;

        #region Getter / Setter

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string ServerType
        {
            get { return serverType; }
            set { serverType = value; }
        }

        public BitmapImage Image
        {
            get { return image; }
            set { image = value; }
        }

        #endregion
    }

    class FolderBrowserWindow : IWin32Window
    {
        IntPtr _handle;

        #region Getter / Setter

        IntPtr IWin32Window.Handle
        {
            get { return _handle; }
        }

        #endregion

        public FolderBrowserWindow(IntPtr handle)
        {
            _handle = handle;
        }
    }
}
