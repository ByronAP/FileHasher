using System.IO;
using System.Windows.Forms;

namespace FileHasher
{
    internal static class GlobalSettings
    {
        internal static bool WinContextMenuEnabled
        {
            get {
                var cd = new CommonApplicationData(Application.CompanyName, Application.ProductName, true);
                if (Directory.Exists(cd.ApplicationFolderPath) && File.Exists(cd.ApplicationFolderPath + "\\1.dat"))
                    return true;
                else
                    return false;
            }
            set {
                var cd = new CommonApplicationData(Application.CompanyName, Application.ProductName, true);
                switch (value)
                {
                    case true:
                        File.WriteAllText(cd.ApplicationFolderPath + "\\1.dat", "");
                        break;
                    case false:
                        if (File.Exists(cd.ApplicationFolderPath + "\\1.dat"))
                            File.Delete(cd.ApplicationFolderPath + "\\1.dat");
                        break;
                }
            }
        }
    }
}
