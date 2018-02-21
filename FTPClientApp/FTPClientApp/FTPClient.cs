using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FTPClientApp
{
    class FTPClient
    {
        string ftpAddress = null;
        string login = null;
        string password = null;

        public FTPClient(string _ftpAddress, string _login, string _password)
        {
            ftpAddress = _ftpAddress;
            login = _login;
            password = _password;

        }
        /// <summary>
        /// Neu file da ton tai tren FTP server thi delete di va upload lai
        /// Neu dang upload ma bi loi thi se retry 3 lan ( tro 5 seconds), sau do moi return khoi function
        /// </summary>
        /// <param name="file">testupload.zip</param>
        /// <param name="pathToCreate">yyyy/mm/dd</param>
        public void uploadFile(string file, string pathToCreate)
        {
            FileStream fs = null;
            Stream rs = null;

            try
            {
                if (!File.Exists(file))
                {
                    throw new Exception(String.Format("Upload source file does not exsit:{0}", file));
                }
                // Get file name
                FileInfo info = new FileInfo(file);
                string uploadFileName = info.Name;
                
                // Upload URL: ftp://192.168.100.12/yyyy/mm/dd/testupload.zip
                string ftpUrl = string.Format("ftp://{0}/{1}/{2}", ftpAddress, pathToCreate, uploadFileName);

                // Read local file to upload
                fs = new FileStream(file, FileMode.Open, FileAccess.Read);

                // Create FTP web request 
                FtpWebRequest requestObj = FtpWebRequest.Create(ftpUrl) as FtpWebRequest;
                requestObj.Method = WebRequestMethods.Ftp.UploadFile;
                requestObj.Credentials = new NetworkCredential(login, password);
                requestObj.KeepAlive = false;// Close khi complete
                requestObj.UseBinary = true;
                requestObj.UsePassive = true; // De phong client bi firewall


                // Doc file va write vao ftp request stream
                rs = requestObj.GetRequestStream();
                //byte[] buffer = new byte[8092];
                //int read = 0;
                //while ((read = fs.Read(buffer, 0, buffer.Length)) != 0)
                //{
                //    rs.Write(buffer, 0, read);
                //}
                //rs.Flush();

                // Write to request stream
                fs.CopyTo(rs);
                rs.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("File upload/transfer Failed.\r\nError Message:\r\n" + ex.Message);
                throw ex;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }

                if (rs != null)
                {
                    rs.Close();
                    rs.Dispose();
                }
            }

        }
        /// <summary>
        ///  Active FTP :
        ///  http://slacksite.com/other/ftp.html
        /// command : client >1023 -> server 21
        /// data    : client >1023 <- server 20
        ///     Neu port 1023 bi chan firewall tu ngoai vao thi chuyen sang dung Passive FTP
        /// Passive FTP : 
        /// command : client >1023 -> server 21
        /// data    : client >1024 -> server >1023
        /// </summary>
        public bool testConnect()
        {
            try
            {
                if ( ftpAddress != null && login != null && password != null)
                {
                    string ftpUrl = string.Format("ftp://{0}", ftpAddress);
                    FtpWebRequest requestObj = FtpWebRequest.Create(ftpUrl) as FtpWebRequest;
                    requestObj.Method = WebRequestMethods.Ftp.ListDirectory;
                    requestObj.Credentials = new NetworkCredential(login, password);
                    requestObj.KeepAlive = false;
                    requestObj.UsePassive = true;// Xem chu thic o tren
                    requestObj.GetResponse();
                    // Create success web request object
                    return true;
                }
                else
                {
                    Console.WriteLine("FTP Connect information is null");
                }
               
            }
            catch (Exception ex)
            {
                Console.WriteLine("List FTP folder Failed.\r\nError Message:\r\n" + ex.Message);
            }
            return false;
        }


       
        /// <summary>
        /// Create FTP sub folder
        ///     yyyy/mm/dd
        /// </summary>
        public void MakeFTPDir(string pathToCreate)
        {
            Console.WriteLine("MakeFTPDir start");
            Console.WriteLine("pathToCreate:"+ pathToCreate);
            Stream ftpStream = null;

            string[] subDirs = pathToCreate.Split('/');

            string currentDir = string.Format("ftp://{0}", ftpAddress);

            foreach (string subDir in subDirs)
            {
                try
                {
                    currentDir = currentDir + "/" + subDir;
                    Console.WriteLine("currentDir:" + currentDir);

                    FtpWebRequest reqFTP = FtpWebRequest.Create(currentDir) as FtpWebRequest;
                    reqFTP.Method = WebRequestMethods.Ftp.MakeDirectory;
                    reqFTP.Credentials = new NetworkCredential(login, password);
                    reqFTP.KeepAlive = false;

                    FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                    ftpStream = response.GetResponseStream();
                    ftpStream.Close();

                    response.Close();
                }
                catch (Exception ex)
                {
                    // Console.WriteLine("ignored, folder alread exist");
                    // Ignore if directory already exist.
                }
            }
        }
        /// <summary>
        /// Copy code from: http://www.dreamincode.net/forums/topic/278703-help-with-ftp-upload-with-resume-using-ftpwebrequest/
        /// Save code o day, chua test
        /// Function nay ho tro resume upload neu file da ton tais
        /// </summary>
        /// <param name="FTPAddress"></param>
        /// <param name="filePath"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        private void uploadFileExt(string FTPAddress, string filePath, string username, string password)
        {
            long resumeOffset = 0;
            try
            {
                //Check if file exists for resume and offset
                FtpWebRequest reqSize = (FtpWebRequest)FtpWebRequest.Create(FTPAddress + "/" + Path.GetFileName(filePath));
                reqSize.Credentials = new NetworkCredential(username, password);
                reqSize.Method = WebRequestMethods.Ftp.GetFileSize;
                reqSize.UseBinary = true;

                FtpWebResponse loginresponse = (FtpWebResponse)reqSize.GetResponse();
                FtpWebResponse respSize = (FtpWebResponse)reqSize.GetResponse();
                respSize = (FtpWebResponse)reqSize.GetResponse();
                resumeOffset = respSize.ContentLength;

                respSize.Close();
            }
            catch
            {
                resumeOffset = 0;
            }
            try
            {

                ////Create FTP request
                //Load the file
                FileStream stream = File.OpenRead(filePath);
                int buffLength = 2048;
                byte[] buffer = new byte[buffLength];

                //Upload file

                int contentLen = 0; //stream.Read(buffer, 0, buffLength);
                long progress = 0;
                int percent = 0;

                //Create FTP request
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FTPAddress + "/" + Path.GetFileName(filePath));

                if (resumeOffset == stream.Length)
                {
                    Console.WriteLine("File is already on server.");
                }
                else
                {
                    try
                    {
                        if (resumeOffset == 0)
                        {
                            request.Method = WebRequestMethods.Ftp.UploadFile;
                            request.Credentials = new NetworkCredential(username, password);
                            request.UsePassive = true;
                            request.UseBinary = true;
                            request.KeepAlive = false;
                            request.Timeout = 3000;

                            Stream reqStream = request.GetRequestStream();

                            contentLen = stream.Read(buffer, 0, buffLength);

                            Console.WriteLine("Upload from 0%.");
                            while (contentLen != 0)
                            {
                                reqStream.Write(buffer, 0, contentLen);
                                contentLen = stream.Read(buffer, 0, buffLength);
                                progress += contentLen;
                                percent = (int)(progress * 100 / stream.Length);
                            }
                            percent = 100;
                            reqStream.Close();
                            Console.WriteLine("Uploaded Successfully");
                        }
                        else
                        {
                            request.Method = WebRequestMethods.Ftp.AppendFile;
                            request.Credentials = new NetworkCredential(username, password);
                            request.UsePassive = true;
                            request.UseBinary = true;
                            request.KeepAlive = false;
                            request.Timeout = 3000;

                            Stream reqStream = request.GetRequestStream();

                            stream.Seek(resumeOffset, SeekOrigin.Begin);

                            contentLen = stream.Read(buffer, 0, buffLength);

                            Console.WriteLine("Resuming upload from " + resumeOffset.ToString());

                            progress = resumeOffset;
                            while (contentLen != 0)
                            {
                                reqStream.Write(buffer, 0, contentLen);
                                contentLen = stream.Read(buffer, 0, buffLength);
                                progress += contentLen;
                                percent = (int)(progress * 100 / stream.Length);
                            }
                            percent = 100;
                            reqStream.Close();
                            Console.WriteLine("Uploaded Successfully");
                        }
                    }
                    catch
                    {
                        try
                        {
                            request.Abort();
                            stream.Close();
                        }
                        catch
                        {
                        }
                        Console.WriteLine("Uploaded Failed, try again.");
                    }
                }
                stream.Close();
            }
            catch
            {

            }
        }



    }

}
