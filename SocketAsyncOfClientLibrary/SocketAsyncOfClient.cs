using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketAsyncOfClientLibrary
{ 
    /// <summary>
    /// 异步客户端端Socket
    /// </summary>
    public class SocketAsyncOfClient
    {
        #region 字段属性
        /// <summary>
        /// 客户端Socket句柄
        /// </summary>
        private Socket _Client = null;
        /// <summary>
        /// Socket客户端活跃度
        /// </summary>
        private bool _Alive;
        /// <summary>
        /// Socket客户端首次连接
        /// </summary>
        private bool _FirstConnected;
        /// <summary>
        /// Socket客户端正常关闭
        /// </summary>
        private bool _NormalClosed;
        /// <summary>
        /// Socket远程服务端IP
        /// </summary>
        public string IP { get; set; }
        /// <summary>
        /// Socket远程服务端端口
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// Socket客户端自动重连
        /// </summary>
        public bool AutoReconnect { get; set; }
        /// <summary>
        /// Socket客户端自动重启时间间隔 单位：毫秒
        /// </summary>
        public int Interval { get; set; }
        /// <summary>
        ///  Socket数据传输缓存大小 默认：2K
        /// </summary>
        public long BufSize { get; set; } = 2048;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">Socket远程服务端IP</param>
        /// <param name="port">Socket远程服务端端口</param>
        /// <param name="autoreconnect">Socket客户端自动重连</param>
        /// <param name="interval"> Socket客户端自动重连时间间隔 单位：毫秒</param>
        public SocketAsyncOfClient(string ip, int port, bool autoreconnect = false, int interval = 1000)
        {
            this.IP = ip;
            this.Port = port;
            this.AutoReconnect = autoreconnect;
            this.Interval = interval;
        }
        #endregion

        #region 获取本机设备唯一ID
        public byte[] GetMachineUniqueID()
        {
            string id = string.Empty;
            SelectQuery query = new SelectQuery("select * from Win32_ComputerSystemProduct");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                foreach (var item in searcher.Get())
                {
                    using (item) id = item["UUID"].ToString();
                }
            }

            return Encoding.UTF8.GetBytes(id);
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            _Alive = false;
            _FirstConnected = true;
            _NormalClosed = false;
        }
        #endregion

        #region 创建Socket客户端实例
        /// <summary>
        /// 创建Socket客户端实例
        /// </summary>
        /// <returns></returns>
        private bool CreatInstanceOfClientSocket()
        {
            bool done = false;
            try
            {
                IPAddress ip = IPAddress.Parse(this.IP);
                IPEndPoint ipe = new IPEndPoint(ip, (int)this.Port);
                _Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _Client.Connect(ipe);
                done = true;
            }
            catch (Exception ex)
            {
                //启动失败
                if (ExceptionOccured != null)
                {
                    ExceptionOccured(this, new SocketEventArgs() { Message = ex.Message });
                }
            }
            return done;
        }
        #endregion

        #region 启动Socket客户端
        /// <summary>
        /// 启动Socket客户端
        /// </summary>
        public void Start()
        {
            Init();
            Task.Factory.StartNew(() =>
            {
                while (_FirstConnected || (AutoReconnect && !_NormalClosed))
                {
                    _FirstConnected = false;
                    _Alive = CreatInstanceOfClientSocket();
                    //启动连接成功
                    if (_Alive)
                    {
                        if (Connected != null)
                        {
                            Connected(this, new SocketEventArgs() { });
                        }
                    }
                    while (_Alive)
                    {
                        try
                        {
                            byte[] bytes = new byte[BufSize];
                            //获取信息
                            int len = _Client.Receive(bytes);
                            //转换
                            string msg = Encoding.UTF8.GetString(bytes, 0, len);
                            if (Received != null)
                            {
                                Received(this, new SocketEventArgs() { Buffer = Encoding.UTF8.GetBytes(msg.ToCharArray(), 0, len) });
                            }
                        }
                        catch (Exception ex)
                        {
                            //尝试关闭失效的Socket客户端
                            Dispose(new List<dynamic>() { _Client });
                            _Alive = false;
                            if (Closed != null)
                            {
                                Closed(this, new SocketEventArgs() { Message = ex.Message });
                            }
                        }
                        finally
                        {
                            if (AutoReconnect && !_NormalClosed)
                            {
                                Thread.Sleep(Interval);
                            }
                        }
                    }
                }
            });
        }
        #endregion

        #region 数据发送
        /// <summary>
        /// 数据发送
        /// </summary>
        public void Send(byte[] buffer)
        {
            try
            {
                _Client.Send(buffer);
            }
            catch (Exception ex)
            {
                Dispose((new List<dynamic>() { _Client }));
                if (ExceptionOccured != null)
                {
                    ExceptionOccured(this, new SocketEventArgs() { Message = ex.Message });
                }
            }
        }
        #endregion

        #region 释放Socket
        private void Dispose(List<dynamic> res)
        {
            for (int i = 0; i < res.Count(); i++)
            {
                if (res[i] != null)
                {
                    try
                    {
                        res[i].Dispose();
                    }
                    catch { }
                    finally
                    {
                        res[i] = null;
                    }
                }
            }
        }
        #endregion

        #region 关闭Socket客户端
        /// <summary>
        /// 关闭Socket
        /// </summary>
        public void Close()
        {
            _NormalClosed = true;
            Dispose(new List<dynamic>() { _Client });
        }
        #endregion

        #region 事件
        /// <summary>
        /// 用于完成Socket客户端连接操作的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> Connected;
        /// <summary>
        /// 用于完成Socket远程终结点接收操作的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> Received;
        /// <summary>
        /// 用于处理Socket客户端异常发生的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> ExceptionOccured;
        /// <summary>
        /// 用于完成Socket客户端关闭操作的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> Closed;
        #endregion
    }
}
