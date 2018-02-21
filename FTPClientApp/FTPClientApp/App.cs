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
        public static string local_error = @"D:/FTPLocalFolder/error";
        public static string settingFile = "Settings.cfg";
        public static string[] uploadFiles = null;

        public static bool is_alive = true;
        public static string exit_flag= "exit";
        public static string running_lock = "running";


        static void Main(string[] args)
        {
            Console.WriteLine("FTPClientApp Start");
            if (File.Exists(running_lock))
            {
                Console.WriteLine("FTPClientApp cannot start, other process is running");
                return;
            }
            Console.WriteLine("FTPClientApp create running lock file");
            Console.WriteLine("FTPClientApp, create file \"exit\" in same folder to finish");

            var lockFile = File.Create(running_lock);
            lockFile.Close();//release resources that it's using

            // Read setting (optional)
            if (File.Exists(settingFile))
            {
                Console.WriteLine("FTPClientApp read Settings.cfg file");
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
                try { 
                    File.Delete(running_lock);
                }catch (Exception e)
                {

                }
                return;
            }
            // Start thread dinh ky kiem tra file exit, update is_alive flag
            ThreadStart testThreadStart = new ThreadStart(new App().ThreadRun);
            Thread testThread = new Thread(testThreadStart);
            testThread.Start();

            // Main process: doc danh sach file trong local folder va upload len FTP
            while (is_alive)
            {
                // Read file in working folder and send to FTP server
                uploadFiles = FileManager.FindFiles(local_working);
                if (uploadFiles != null && uploadFiles.Length > 0)
                {
                    Console.WriteLine(String.Format("Found {0} files in working folders", uploadFiles.Length));

                    // Generate sub folders
                    string subFolders = FileManager.getSubPath('/');// return:  yyyy/mm/dd/hh

                    // Make pathToCreate in FTP server
                    fptClient.MakeFTPDir(subFolders);

                    
                    foreach (string file in uploadFiles)
                    {
                        // Get filename only
                        string fileName = (new FileInfo(file)).Name;

                        // Upload to FTP server
                        int cnt = 0;
                        bool success = false;
                        // retry 3 times
                        while (cnt <= 3)
                        {
                            try
                            {
                                success = false;
                                Console.WriteLine("Start upload file:" + file);
                                // Upload source file to FTP server
                                fptClient.uploadFile(file, subFolders);

                                // Upload success, move  source file to done folder
                                if (File.Exists(file))
                                {
                                    // Create sub folder in local server\done: \yyyymmdd
                                    string dest_folder = FileManager.createSubFolders(local_done, subFolders, '/');

                                    string dest_file = dest_folder + @"\" + fileName + "_" + getDateTime();// done\yyyy\mm\dd\filename_yyyymmddhhmmssmmm
                                    FileManager.moveFile(file, dest_file);

                                    Console.WriteLine(String.Format("Upload success and moved file to done foler: {0}", dest_file));
                                    success = true;
                                }
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error:" + ex.Message);
                                Console.WriteLine(String.Format("Upload failed, sleep 5 seconds then retry, cnt {0}", cnt));
                                Thread.Sleep(5000);// sleep 5000 seconds
                            }
                            cnt++;
                        }
                        
                        // Neu upload error, move file to error folder
                        if (!success)
                        {
                            Console.WriteLine("Upload failed!");
                            // Upload success, move  source file to done folder
                            if (File.Exists(file))
                            {
                                // Create sub folder in local server\done: \yyyymmdd
                                string dest_folder = local_error;

                                string dest_file = dest_folder + @"\" + fileName + "_" + getDateTime();// done\yyyy\mm\dd\filename_yyyymmddhhmmssmmm
                                FileManager.moveFile(file, dest_file);

                                Console.WriteLine(String.Format("Moved file to error foler: {0}", dest_file));
                            }
                        }
                        


                    }

                }
                Console.WriteLine("FTPClientApp: Main thread is running");
                Thread.Sleep(10000);// sleep 10 seconds
            }
            Console.WriteLine("is_alive = false => loop  exited");
            try
            {
                Console.WriteLine("Try delete running lock");
                File.Delete(running_lock);
            }
            catch (Exception e)
            {
                Console.WriteLine("Delete running lock error:"+e.Message);
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
        /// <summary>
        /// Thread dinh ky 5s kiem tra file exit co ton tai hay khong?
        /// Neu ton tai, update co static: is_alive = false
        /// </summary>
        public void ThreadRun()
        {
            //executing in thread
            Console.WriteLine(" *****  Thread1 Start ***** ");
            while (true)
            {
                if (File.Exists(exit_flag))
                {
                    is_alive = false;
                    Console.WriteLine("Found exit_flag, Monitor Thread exit");
                    return;
                }
            }
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
            local_error = properties.get("local_error");


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
