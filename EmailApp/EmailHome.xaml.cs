using EmailApp.Classes;
using MahApps.Metro.Controls;
using OpenPop.Common.Logging;
using OpenPop.Mime;
using OpenPop.Pop3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EmailApp
{
    /// <summary>
    /// Interaction logic for EmailHome.xaml
    /// </summary>
    public partial class EmailHome : MetroWindow
    {
        private readonly Dictionary<int, Message> messages = new Dictionary<int, Message>();
        private readonly Pop3Client pop3Client;
        public Boolean sslEnabled { get; set; }
        public String userName { get; set; }
        public String password { get; set; }
        public String popServer { get; set; }
        public Int32 popPort { get; set; }
        public String smtpServer { get; set; }
        public Int32 smtpPort { get; set; }
        public Boolean loggingEnabled { get; set; }
        public Boolean IsDisposed { get; set; }
        private CancellationTokenSource cts;
        ConfigDialog configDialog = null;
        public EmailHome()
        {

            InitializeComponent();
            pop3Client = new Pop3Client();
            IsDisposed = false;
            SetDefaultEmailConfiguration();

            // Here we want to log to a file
            DefaultLogger.SetLog(new FileLogger());

            // Enable file logging and include verbose information
            FileLogger.Enabled = true;
            FileLogger.Verbose = true;
        }

        

        private void EmailList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private async void BtnGetEmails_Click(object sender, RoutedEventArgs e)
        {
            BtnGetEmails.IsEnabled = false;


            //EmailManager em = new EmailManager(
            //    sslEnabled,
            //    userName,
            //    password,
            //    popServer,
            //    popPort,
            //    smtpServer,
            //    smtpPort
            //    );
            
            cts = new CancellationTokenSource();
            try
            {
                await ReceiveEmails(cts);
            }
            catch (Exception)
            {
                //Let's eat the exception for now...

            }
            finally { BtnGetEmails.IsEnabled = true; }
            
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }
        }


        private void SetDefaultEmailConfiguration()
        {
            // This is only for faster debugging purposes

            // This describes how the LoginCreds.txt file should look like
            popServer = "pop.gmail.com"; //await reader.ReadLineAsync();
            popPort = 995;//Convert.ToInt32(await reader.ReadLineAsync()); // Port
            smtpServer = "smtp.gmail.com"; //await reader.ReadLineAsync();
            smtpPort = 993; //Convert.ToInt32(await reader.ReadLineAsync()); // Port
            sslEnabled = true;//bool.Parse(await reader.ReadLineAsync() ?? "true"); // Whether to use SSL or not
            //userName = "getchandan.ghosh@gmail.com";//await reader.ReadLineAsync(); // Username
            //password = "Arnabgh0$h";//await reader.ReadLineAsync(); // Password

            // We will try to load in default values for the hostname, port, ssl, username and password from a file
            //string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            //string file = System.IO.Path.Combine(myDocs, "LoginCreds.txt");
            //if (System.IO.File.Exists(file))
            //{
            //    using (System.IO.StreamReader reader = new System.IO.StreamReader(System.IO.File.OpenRead(file)))
            //    {
            //        // This describes how the LoginCreds.txt file should look like
            //        popServer = "pop.gmail.com"; //await reader.ReadLineAsync();
            //        popPort = 995;//Convert.ToInt32(await reader.ReadLineAsync()); // Port
            //        smtpServer = "smtp.gmail.com"; //await reader.ReadLineAsync();
            //        smtpPort = 993; //Convert.ToInt32(await reader.ReadLineAsync()); // Port
            //        sslEnabled = true;//bool.Parse(await reader.ReadLineAsync() ?? "true"); // Whether to use SSL or not
            //        userName = "getchandan.ghosh@gmail.com";//await reader.ReadLineAsync(); // Username
            //        password = "Arnabgh0$h";//await reader.ReadLineAsync(); // Password
            //    }
            //}
        }

        public async Task ReceiveEmails(CancellationTokenSource cts)
        {
            EmailDownloadProgressBar.Value = 0;

            try
            {

                if (pop3Client.Connected)
                {
                    pop3Client.Disconnect();
                }

                await Task.Run(() =>
                {
                    pop3Client.Connect(popServer, popPort, sslEnabled);
                }, cts.Token);

                await Task.Run(() =>
                {
                    pop3Client.Authenticate(userName, password, AuthenticationMethod.Auto);
                }, cts.Token);

                int count = await Task<Int32>.Run(() =>
                {
                    return pop3Client.GetMessageCount();
                }, cts.Token);

                                
                int success = 0;
                int fail = 0;
                for (int i = count; i >= 1; i -= 1)
                {
                    // Check if the form is closed while we are working. If so, abort
                    if (cts.IsCancellationRequested)
                        return;

                    try
                    {
                        Message message = await Task.Run(()=>
                        {
                           return pop3Client.GetMessage(i);
                        }, cts.Token);

                        // Add the message to the dictionary from the messageNumber to the Message
                        messages.Add(i, message);
                        
                        // Show the built node in our list of messages
                        //listMessages.Nodes.Add(node);
                        EmailList.Items.Add(message.Headers.Subject);

                        success++;
                    }
                    catch (Exception e)
                    {
                        DefaultLogger.Log.LogError(
                            "TestForm: Message fetching failed: " + e.Message + "\r\n" +
                            "Stack trace:\r\n" +
                            e.StackTrace);
                        fail++;
                    }

                    EmailDownloadProgressBar.Value = (int)(((double)(count - i) / count) * 100);
                }

            }
            catch
            {

            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        
        private void BtnConfigurer_Click(object sender, RoutedEventArgs e)
        {
            configDialog = new ConfigDialog();
            configDialog.Owner = this;
            configDialog.Show();
            configDialog.BtnOk.Click += BtnOk_Click;
        }

        void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.userName = configDialog.txtUserName.Text;
            this.password = configDialog.txtPassword.Password;
            configDialog.BtnOk.Click -= BtnOk_Click;
            configDialog.Close();
            
        }
    }
}
