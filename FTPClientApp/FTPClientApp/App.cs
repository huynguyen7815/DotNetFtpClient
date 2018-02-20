using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;

namespace FTPClientApp
{
    class App
    {
        public static string ftp_address = "192.168.100.12";
        public static string ftp_login = "ftpuser";
        public static string ftp_pass = "password";
        public static string local_working = @"D:/FTPLocalFolder";
        public static string local_done = @"D:/FTPLocalFolder/done";
        public static string settingFile = "Settings.cfg";
        public static string[] uploadFiles = null;

        static void Main(string[] args)
        {
            Console.WriteLine("FTPClientApp Start");
            // Read setting (optional)
            if (File.Exists(settingFile))
            {
                readSetting();
            }
            FTPClient fptClient = new FTPClient(ftp_address, ftp_login, ftp_pass);

            // Test FTP connection ( first time only)
            if (fptClient.testConnect())
            {
                Console.WriteLine("Test Connect success to FTP server");
            }
            else
            {
                Console.WriteLine("Connect error to FTP server");
                return;
            }


            // Read file in working folder and send to FTP server
            uploadFiles = FileManager.FindFiles(local_working);
            if (uploadFiles != null && uploadFiles.Length > 0)
            {
                Console.WriteLine(String.Format("Found {0} files in working folders", uploadFiles.Length));

                // Generate sub folders
                string subFolders = FileManager.getSubPath('/');// return:  yyyy/mm/dd/hh

                // Make pathToCreate in FTP server
                fptClient.MakeFTPDir(subFolders);


                foreach (string source_file in uploadFiles)
                {
                    // Get filename only
                    string fileName = (new FileInfo(source_file)).Name;

                    // Upload to FTP server
                    int cnt = 0;
                    while (cnt <= 3)
                    {
                        try
                        {
                            Console.WriteLine("Start upload file:" + source_file);
                            // Upload source file to FTP server
                            fptClient.uploadFile(source_file, subFolders);

                            // Upload success, move  source file to done folder
                            if (File.Exists(source_file))
                            {
                                // Create sub folder in local server\done: \yyyymmdd
                                string dest_folder = FileManager.createSubFolders(local_done, subFolders, '/');

                                string dest_file = dest_folder + @"\" + fileName + "_" + getDateTime();// done\yyyy\mm\dd\filename_yyyymmddhhmmssmmm
                                FileManager.moveFile(source_file, dest_file);

                                Console.WriteLine(String.Format("Upload success and moved file to done foler: {0}", dest_file));
                            }
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error:" + ex.Message);
                            Console.WriteLine(String.Format("Upload failed, sleep 5 seconds then retry, cnt {0}", cnt));
                            Thread.Sleep(5 * 1000);// sleep 5 seconds
                        }
                        cnt++;
                    }

                }

            }
            Console.WriteLine("Press any key!");
            Console.ReadLine();
        }

        private static void readSetting()
        {
            Console.WriteLine("readSetting Start");
            Properties properties = new Properties("Settings.cfg");

            ftp_address = properties.get("ftp_address");
            ftp_login = properties.get("ftp_login");
            ftp_pass = properties.get("ftp_pass");
            local_working = properties.get("local_working");
            local_done = properties.get("local_done");


            Console.WriteLine("ftp_login:" + ftp_login);
            Console.WriteLine("local_working:" + local_working);
            Console.WriteLine("local_done:" + local_done);
        }
        private static string getDateTime()
        {
            DateTime d = DateTime.Now;
            // string str = String.Format("{0:00}/{1:00}/{2:0000} {3:00}:{4:00}:{5:00}.{6:000}", d.Month, d.Day, d.Year, d.Hour, d.Minute, d.Second, d.Millisecond);
            // I got this result: "02/23/2015 16:42:38.234"
            string str = String.Format("{0:0000}{1:00}{2:00}{3:00}{4:00}{5:00}{6:000}", d.Year, d.Month, d.Day,d.Hour, d.Minute, d.Second, d.Millisecond);
            
            return str;
        }
    }
}
