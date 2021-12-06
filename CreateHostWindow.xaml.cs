﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;

namespace Remote_Co_Editor
{
    /// <summary>
    /// Interaction logic for CreateHostWindow.xaml
    /// </summary>
    public partial class CreateHostWindow : Window
    {
        public CreateHostWindow()
        {
            InitializeComponent();
        }

        public void Create(object sender, RoutedEventArgs e)
        {
            // (Application. as CreateHostWindow).Close();
            if (GlobalVars.isConnect == false)
            {
                GlobalVars.isConnect = true;
                GlobalVars.isServer = true;
                GlobalVars.ServerAddreass = new(item1: IP.Text, item2: Port.Text);
                IPAddress ip = IPAddress.Parse(GlobalVars.ServerAddreass.Item1);
                int port = Convert.ToInt32(GlobalVars.ServerAddreass.Item2);
                IPEndPoint ipe = new IPEndPoint(ip, port);

                Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.Bind(ipe);
                int MaxClient = 2;
                ServerSocket.Listen(MaxClient);
                new Thread(() =>
                {
                    for (int i = 0; i < MaxClient; i++)
                    {
                        Socket clientSocket;
                        try
                        {
                            clientSocket = ServerSocket.Accept();
                        }
                        catch
                        {
                            break;
                        }
                        GlobalVars.clientsSocketList.Add(clientSocket);
                        GlobalVars.UserList.Add(new(item1: (clientSocket.RemoteEndPoint as IPEndPoint).Address.ToString(), item2: (clientSocket.RemoteEndPoint as IPEndPoint).Port.ToString()));
                        foreach(var ss in GlobalVars.clientsSocketList)
                        {
                            string str = JsonConvert.SerializeObject(GlobalVars.UserList);
                            ss.Send(Encoding.ASCII.GetBytes("0"+str));
                        }
                        new Thread(() => {
                            Send(ref clientSocket);
                        }).Start();
                        new Thread(() => {
                            Receive(ref clientSocket);
                        }).Start();
                    }
                }).Start();
            }
        }
        public void Send(ref Socket s)
        {
            while (true)
            {
                if (GlobalVars.toSend)
                {
                    if (GlobalVars.isConnect && GlobalVars.isServer)
                    {
                        // send to clients
                        foreach (var ss in GlobalVars.clientsSocketList)
                        {
                            string str = "";
                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                str = JsonConvert.SerializeObject(GlobalVars.Packet);
                            }));
                            ss.Send(Encoding.ASCII.GetBytes("1" + str));
                        }
                    }
                    else if (GlobalVars.isConnect && GlobalVars.isClient)
                    {
                        // send to server
                        string str = "";
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            str = JsonConvert.SerializeObject(GlobalVars.Packet);
                        }));

                        s.Send(Encoding.ASCII.GetBytes("1" + str));
                    }
                    GlobalVars.toSend = false;
                }
            }

        }

        public void Receive(ref Socket s)
        {
            while (true)
            {
                byte[] StrByte = new byte[10000];
                int count = s.Receive(StrByte);
                string RawMessage = Encoding.UTF8.GetString(StrByte.SubArray(0, count));
                if (GlobalVars.isConnect && GlobalVars.isServer)
                {
                    // change RTB and send to clients (only document)
                    string message = RawMessage.Substring(1);
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        (Application.Current.MainWindow as MainWindow).tb.Text = JsonConvert.DeserializeObject<string>(message);
                        GlobalVars.Packet = (Application.Current.MainWindow as MainWindow).tb.Text;
                    }));


                    GlobalVars.toSend = true;
                }
                else if (GlobalVars.isConnect && GlobalVars.isClient)
                {
                    // update UserList
                    if (RawMessage[0] == '0')
                    {
                        string message = RawMessage.Substring(1);
                        GlobalVars.UserList = JsonConvert.DeserializeObject<List<Tuple<string, string>>>(message);
                    }
                    else if (RawMessage[0] == '1')
                    {
                        string message = RawMessage.Substring(1);
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            (Application.Current.MainWindow as MainWindow).tb.Text = JsonConvert.DeserializeObject<string>(message);
                        }));
                    }
                }
            }
            // change RTB (only document)
        }
    }
}
