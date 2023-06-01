using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.Wpf;
using System.Printing;

namespace LlamaBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            Browser.MenuHandler = new Other.DisableMenuCustom();
            Browser.AddressChanged += Browser_AddressChanged;

            Browser.LoadingStateChanged += Browser_LoadingStateChanged;


            this.Loaded += MainWindow_Loaded;

        }

        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
           if(!e.IsLoading)
            {


            }


        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Browser.Address = "https://google.com";
        }

        private void Browser_AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UriEntry.Text = Browser.Address;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            Browser.Address = UriEntry.Text;

        }

        private static CookieContainer GetCookies(ChromiumWebBrowser llama)
        {
            CookieContainer cookieJar = new CookieContainer();
            try
            {
                string value = System.Configuration.ConfigurationManager.AppSettings["CookieDomain"];

                var domainUri = new Uri(value);
                cookieJar = GetCookiesFromURI(new Uri(domainUri.GetLeftPart(UriPartial.Authority)));
            }
            catch (Exception ex)
            {
                
                
            }
            return cookieJar;
        }

        private static CookieContainer GetCookiesFromURI(Uri uri)
        {
            CookieContainer cookies = new CookieContainer();
            int datasize = 8192 * 16;
            StringBuilder cookieData = new StringBuilder(datasize);

            //call once to set datasize to fit cookie
            if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
            {
                //this means we have no cookies
                if (datasize < 0)
                    return cookies;

                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new StringBuilder(datasize);

                //call a second time to get cookie
                if (!InternetGetCookieEx(
                    uri.ToString(),
                    null, cookieData,
                    ref datasize,
                    InternetCookieHttponly,
                    IntPtr.Zero))
                    return cookies;
            }
            if (cookieData.Length > 0)
            {
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }

            return cookies;
        }

        private string _personid;

        private async void ClickOk()
        {
            
            var script = @"document.getElementById('SubmitForm').click();";
            var ContentFrame = Browser.GetBrowser().GetFrame("content");
            ContentFrame.ExecuteJavaScriptAsync(script);



        }


        private async void ReadField(object sender, RoutedEventArgs e)
        {
            try
            {
                string script = "document.getElementsByName('inpersonid')[0].value";
                List<string> frameidents = Browser.GetBrowser().GetFrameNames();
                

                foreach (var item in frameidents)
                {

                    var test2 = Browser.GetBrowser().GetFrame(item);
                    JavascriptResponse response = await test2.EvaluateScriptAsync(script);


                    if (response.Result != null)
                    {
                        string resultS = response.Result.ToString(); // whatever you need
                        _personid = resultS;
                        MessageBox.Show(resultS, $"Personid found in Frame {item}");
                    }
                    else
                    {
                        MessageBox.Show("not found", $"No PersonID Found in Frame {item}");

                    }
                }


             
             
                

            }
            catch (Exception Err)
            {



            }
         }







        [DllImport("wininet.dll", SetLastError = true)]

        private static extern bool InternetGetCookieEx(
            string url,
            string cookieName,
            StringBuilder cookieData,
            ref int size,
            Int32 dwFlags,
            IntPtr lpReserved);

        private const Int32 InternetCookieHttponly = 0x00002000;

        private void ReadCookie_Click(object sender, RoutedEventArgs e)
        {
            CookieContainer results = GetCookies(Browser);
            
        }



        private static void GeneratePostData(string personId, string fileType, string imageFormat, string sBoundary, byte[] data, out int trueStreamLength, out byte[] postBuffer)
        {

            using (MemoryStream oPostStream = new MemoryStream())
            {
                using (BinaryWriter oPostData = new BinaryWriter(oPostStream, Encoding.UTF8))
                {
                    var fileName = fileType + personId + ".";
                    var imageContentType = "";

                    switch (imageFormat.ToUpper())
                    {
                        default:
                        case "JPEG":
                            fileName = fileName + "JPG";
                            imageContentType = imageFormat;
                            break;
                        case "PNG":
                            fileName = fileName + "PNG";
                            imageContentType = "x-png";
                            break;
                    }


                    //write the image data
                    oPostData.Write(Encoding.UTF8.GetBytes("--" + sBoundary + "\r\n" + "Content-Disposition: form-data; name=\"infile\"; filename=\"" + fileName + "\"\r\nContent-Type: image/" + imageContentType + "\r\n\r\n"));
                    //oPostData.Write(Encoding.UTF8.GetBytes("--" + sBoundary + "\r\n" + "Content-Disposition: form-data; name=\"infile\"; filename=\"" + FileName + "\"\r\nContent-Type: image/x-png\r\n\r\n"));                        
                    oPostData.Write(data);
                    oPostData.Write(Encoding.UTF8.GetBytes("\r\n"));
                    //write the personid data
                    oPostData.Write(Encoding.UTF8.GetBytes("--" + sBoundary + "\r\n" + "Content-Disposition: form-data; name=\"inpersonid\"\r\n\r\n"));
                    oPostData.Write(Encoding.UTF8.GetBytes(personId));
                    oPostData.Write(Encoding.UTF8.GetBytes("\r\n"));
                    //write the file type data
                    oPostData.Write(Encoding.UTF8.GetBytes("--" + sBoundary + "\r\n" + "Content-Disposition: form-data; name=\"infiletype\"\r\n\r\n"));
                    oPostData.Write(Encoding.UTF8.GetBytes(fileType));
                    oPostData.Write(Encoding.UTF8.GetBytes("\r\n"));

                    oPostData.Write(Encoding.UTF8.GetBytes("--" + sBoundary + "--\r\n"));
                    // Added 6/7/2011 pem.  trueStreamLength will be used to limit the data in the file for the HttpRequest
                    trueStreamLength = Convert.ToInt32(oPostStream.Length);
                    oPostData.Close();
                    oPostStream.Close();
                    postBuffer = oPostStream.GetBuffer();
                }
            }
        }



        public static void PostImageToSite(ChromiumWebBrowser llama, string url, string PersonID, string FileType, string ImageFormat, BitmapSource image)
        {




            int trueStreamLength;
            string sBoundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] postBuffer;
            GeneratePostData(PersonID, FileType, ImageFormat, sBoundary, ConvertImageToBlob(image), out trueStreamLength, out postBuffer);
            string contentType = "multipart/form-data; boundary=" + sBoundary;
            PostToSite(llama, contentType, url, trueStreamLength, postBuffer);
            MessageBox.Show("Upload Complete", "Image Upload", MessageBoxButton.OK);
        }




        public static void PostToSite(ChromiumWebBrowser llama, string ContentType, string url, int trueStreamLength, byte[] PostBuffer, bool showReponse = false)
        {
            try
            {
                
                HttpWebRequest oHttpRequest = (HttpWebRequest)WebRequest.Create(url);
                
                oHttpRequest.ContentType = ContentType;
                oHttpRequest.Method = "POST";
                oHttpRequest.Referer = "TestClient";
                oHttpRequest.Accept = "application/x-ms-application, image/jpeg, application/xaml+xml, image/gif, image/pjpeg, application/x-ms-xbap, */*";
                oHttpRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko";
                oHttpRequest.AllowWriteStreamBuffering = true;
                oHttpRequest.KeepAlive = true;
                oHttpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                oHttpRequest.Credentials = CredentialCache.DefaultNetworkCredentials;
                ServicePointManager.Expect100Continue = false;
                CookieContainer userCookies = GetCookies(llama);
                if (userCookies.Count > 0)
                {
                    oHttpRequest.CookieContainer = userCookies;
                }
                using (Stream oRequestStream = oHttpRequest.GetRequestStream())
                {

                    oRequestStream.Write(PostBuffer, 0, trueStreamLength);
                    oRequestStream.Flush();
                    oRequestStream.Close();
                }
                HttpWebResponse oHttpResponse = (HttpWebResponse)oHttpRequest.GetResponse();
                
            }
            catch (WebException ex1)
            {
                HttpWebResponse response = ex1.Response as HttpWebResponse;

                using (StreamReader oResponseStream = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1252)))
                {
                    string sResponse = oResponseStream.ReadToEnd();
                    //ShowPage(sResponse);
                    oResponseStream.Close();
                }
            }
            catch (Exception ex)
            {
                var innerExc = ex.InnerException?.ToString();


            }
        }



        public static byte[] ConvertImageToBlob(BitmapSource image)
        {
            byte[] RetVal = null;

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));

                    encoder.Save(stream);
                    stream.Flush();
                    RetVal = stream.GetBuffer();

                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                
            }

            return RetVal;
        }
        private BitmapSource selectedfile;

        private void UploadImg_Click(object sender, RoutedEventArgs e)
        {

            if(String.IsNullOrEmpty(_personid))
            {
                MessageBox.Show("Cannot upload. No PersonID Found.");
                return; 
            }
            else if(selectedfile == null)
            {
                MessageBox.Show("No image selected.");
                return;
            }
            else
            {
                PostImageToSite(Browser, "", _personid, "P", "jpg", selectedfile);
                ClickOk();

            }
            

        }

        private void getImg_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            openFileDlg.Title = "Select Img";
            openFileDlg.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
        "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
        "Portable Network Graphic (*.png)|*.png";
            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            
            if (result == true)
            {
                selectedfile = new BitmapImage(new Uri(openFileDlg.FileName));
                tempImg.Source = selectedfile;
                
            }

        }
    }
}
