using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiPlug.Windows.Desktop.Update
{
    internal static class Directories
    {
        internal static string SearchForDirectory(string theStartDir, string theTarget)
        {
            var result = Directory.GetDirectories(theStartDir, theTarget, SearchOption.AllDirectories);

            string Returned = "";

            if (result.Any())
            {
                Returned = result.First();
            }

            return Returned;
        }

        internal static bool DirectoryContainsDlls(string theDir)
        {
            string[] files = System.IO.Directory.GetFiles(theDir, "*.dll");

            return files.Any();
        }


    }
}
