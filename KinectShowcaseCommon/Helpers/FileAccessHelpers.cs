using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.Helpers
{
    public static class FileAccessHelpers
    {
        public static string GetContentsOfFile(string aFullPath)
        {
            string result = null;
            if (File.Exists(aFullPath))
            {
                using (StreamReader fileStream = new StreamReader(aFullPath))
                {
                    //get the description in the 
                    result = fileStream.ReadToEnd();
                    fileStream.Close();
                }

            }
            else
            {
                Debug.WriteLine("FileAccessHelpers - WARN - File doesn't exist: " + aFullPath);
            }

            return result;
        }
    }
}
