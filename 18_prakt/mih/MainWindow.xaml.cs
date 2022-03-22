using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace mih
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        bool IsServActive = false;
        //прослушиваемый порт
        int port = 8888;
        //объект, прослушивающий порт
        TcpListener listener;
        Thread listenThread;
        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;
        }

        private void Start_Serv(object sender, RoutedEventArgs e)
        {
            IsServActive = true;
            startServ = sender as Button;
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Start();
            listenThread = new Thread(() => listen());
            listenThread.Start();
            startServ.IsEnabled = false;
            stopServ.IsEnabled = true;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            StoppingServer();
        }

        //обработка сообщений от клиента
        public void Process(TcpClient tcpClient)
        {
            TcpClient client = tcpClient;
            NetworkStream stream = null; //получение канала связи с клиентом

            try //означает что в случае возникновении ошибки, управление перейдёт к блоку catch
            {
                //получение потока для обмена сообщениями
                stream = client.GetStream(); //получение канала связи с клиентом
                                             // буфер для получаемых данных
                byte[] data = new byte[64];
                //цикл ожидания и отправки сообщений
                while (true)
                {
                    //==========================получение сообщения============================
                    //объект, для формирования строк
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    //до тех пор, пока в потоке есть данные
                    do
                    {
                    //из потока считываются 64 байта и записываются в data начиная с 0
                    bytes = stream.Read(data, 0, data.Length);
                        //из считанных данных формируется строка
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);
                    if (IsServActive==false)
                    {
                        return;
                    }
                    //преобразование сообщения
                    string message = builder.ToString();
                    //вывод сообщения в лог сервера
                    Dispatcher.BeginInvoke(new Action(() => log.Items.Add(message)));
                    string[] mes = message.Split(':');
                    char[] chars = mes[1].ToCharArray();
                    Array.Reverse(chars);
                    mes[1]=new string(chars);
                    //==========================отправка сообщения=============================
                    //преобразование сообщения в набор байт
                    data = Encoding.Unicode.GetBytes(String.Join(":",mes));
                    //отправка сообщения обратно клиенту
                    stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex) //если возникла ошибка, вывести сообщение об ошибке
            {
                Dispatcher.BeginInvoke(new Action(() => log.Items.Add(ex.Message)));
            }
            finally //после выхода из бесконечного цикла
            {
                //освобождение ресурсов при завершении сеанса
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
        }
        //функция ожидания и приёма запросов на подключение
        void listen()
        {
            //цикл подключения клиентов
            while (true)
            {
                try
                {
                    //принятие запроса на подключение
                    TcpClient client = listener.AcceptTcpClient();
                    string s="client connected to " + IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()) + " on port number " + ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString();
                    Dispatcher.BeginInvoke(new Action(() => log.Items.Add(s)));
                    //создание нового потока для обслуживания нового клиента
                    Thread clientThread = new Thread(() => Process(client));
                    clientThread.Start();
                } catch  { break; }
            }
        }

        private void Stop_Serv(object sender, RoutedEventArgs e)
        {
            StoppingServer();
            stopServ.IsEnabled = false;
            startServ.IsEnabled = true;
        }
        private void StoppingServer()
        {
            IsServActive = false;
            listenThread?.Interrupt();
            listener?.Stop();
        }
    }
}
