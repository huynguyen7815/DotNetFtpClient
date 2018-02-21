using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
namespace FTPClientApp
{
    class FileManager
    {
        // Find all files in folder (Not recurse) 
        // 
        public static string[] FindFiles(string folder)
        {
            if (folder == null || !Directory.Exists(folder))
            {
                throw new Exception("Directory not exist:" + folder==null?"null or blank":folder);
            }
            List<string> fileList = new List<string>();
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(folder);
            foreach (string path in fileEntries)
            {
                long length1 = new System.IO.FileInfo(path).Length;
                Thread.Sleep(50);// Cho 50ms roi kiem tra file file size, neu khong thay doi se add vao danh sach xu ly
                long length2 = new System.IO.FileInfo(path).Length;
                if (length1 == length2)
                {
                    fileList.Add(path);
                }

            }
            return fileList.ToArray();
        }

        // Moving file from source to destination
        public static void moveFile(string sourceFile,string destinationFile)
        {
            // To move a file or folder to a new location:
            System.IO.File.Delete(destinationFile);
            System.IO.File.Move(sourceFile, destinationFile);
        }

        /// <summary>
        /// Now: 2018-02-20 12h44
        /// </summary>
        /// <returns>2018/2/20/12/44</returns>
        public static string getSubPath(char path_separator)
        {
            System.DateTime myDate = DateTime.Now;
            string strYear = myDate.Year.ToString();
            string strMonth = myDate.Month.ToString(); // The month component, expressed as a value between 1 and 12.
            string strDay = myDate.Day.ToString();
            string strHour = myDate.Hour.ToString();

            return strYear + path_separator + strMonth +path_separator + strDay + path_separator + strHour;

        }
        /// <summary>
        ///  Kiem tra sub folders co ton tai khong the khong thi tao
        /// </summary>
        /// <param name="baseFolder"></param>
        /// <param name="subPath"></param>
        /// <param name="path_separator"></param>
        /// <returns></returns>
        public static string createSubFolders(string baseFolder, string subPath,char separator)
        {
            string[] subDirs = subPath.Split(separator);

            string currentDir = baseFolder;

            foreach (string subDir in subDirs)
            {
                currentDir = currentDir +@"\" + subDir;

                Console.WriteLine("currentDir:" + currentDir);
                bool exists = System.IO.Directory.Exists(currentDir);
                if (!exists)
                    System.IO.Directory.CreateDirectory(currentDir);

            }
            return currentDir;

        }

       
    }
}
