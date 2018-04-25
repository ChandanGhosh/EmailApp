using OpenPop.Common.Logging;
using OpenPop.Mime;
using OpenPop.Pop3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EmailApp.Classes
{
    class EmailManager
    {
        public Boolean sslEnabled { get; set; }
        public String userName { get; set; }
        public String password { get; set; }
        public String popServer { get; set; }
        public Int32 popPort { get; set; }
        public String smtpServer { get; set; }
        public Int32 smtpPort { get; set; }
        public Boolean loggingEnabled { get; set; }
        public ProgressBar progressBar { get; set; }


        private readonly Dictionary<int, Message> messages = new Dictionary<int, Message>();
		private readonly Pop3Client pop3Client;
        
        public EmailManager() 
        {
            
        }

        public EmailManager(Boolean _sslEnabled, 
            String _userName, 
            String _password, 
            String _popServer, 
            Int32 _popPort, 
            String _smtpServer, 
            Int32 _smtpPort,
            ProgressBar _progressBar = null,
            Boolean _loggingEnabled=false)
        {
            this.sslEnabled = _sslEnabled;
            this.userName = _userName;
            this.password = _password;
            this.popServer = _popServer;
            this.popPort = _popPort;
            this.smtpServer = _smtpServer;
            this.smtpPort = _smtpPort;
            this.loggingEnabled = _loggingEnabled;
            this.progressBar = _progressBar;

            pop3Client = new Pop3Client();

        }



        public async Task ReceiveEmails(Boolean isDisposed)
        {
            progressBar.Value = 0;

            try
            {

                if (pop3Client.Connected)
                    pop3Client.Disconnect();


                await Task.Run(() =>
                {
                    pop3Client.Connect(popServer, popPort, sslEnabled);
                });

                await Task.Run(() =>
                {
                    pop3Client.Authenticate(userName, password, AuthenticationMethod.Auto);
                });
                
                int count = pop3Client.GetMessageCount();



                int success = 0;
				int fail = 0;
                for (int i = count; i >= 1; i -= 1)
                {
                    // Check if the form is closed while we are working. If so, abort
                    if (isDisposed)
                        return;
                    try
                    {
                        Message message = pop3Client.GetMessage(i);

                        // Add the message to the dictionary from the messageNumber to the Message
                        messages.Add(i, message);

                        // Create a TreeNode tree that mimics the Message hierarchy
                        //TreeNode node = new TreeNodeBuilder().VisitMessage(message);

                        // Set the Tag property to the messageNumber
                        // We can use this to find the Message again later
                        //node.Tag = i;

                        // Show the built node in our list of messages
                        //listMessages.Nodes.Add(node);


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

                    progressBar.Value = (int)(((double)(count - i) / count) * 100);
                }

            }
            catch
            {

            }
        }

    }
}
