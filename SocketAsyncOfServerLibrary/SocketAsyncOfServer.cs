using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketAsyncOfServerLibrary
{
    /// <summary>
    /// 异步服务端Socket
    /// </summary>
    public class SocketAsyncOfServer
    {
        #region 字段属性
        /// <summary>
        /// 监听Socket
        /// </summary>
        private Socket _Listener;
        /// <summary>
        /// Socket服务端活跃度
        /// </summary>
        private bool _Alive;
        /// <summary>
        /// Socket服务端首次启动
        /// </summary>
        private bool _FirstStarted;
        /// <summary>
        /// Socket服务端正常关闭
        /// </summary>
        private bool _NormalClosed;
        /// <summary>
        /// Socket远程终结点令牌的管理池对象
        /// </summary>
        public SocketUserTokenPool Pool { get; private set; }
        /// <summary>
        /// Socket服务端IP
        /// </summary>
        public string IP { get; set; }
        /// <summary>
        /// Socket服务端端口
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// Socket服务端自动重启
        /// </summary>
        public bool AutoRestart { get; set; }
        /// <summary>
        /// Socket服务端自动重启时间间隔 单位：毫秒
        /// </summary>
        public int Interval { get; set; }
        /// <summary>
        ///  Socket远程终结点数据传输缓存大小 默认：2K
        /// </summary>
        public long BufSize { get; set; } = 2048;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">Socket服务端IP</param>
        /// <param name="port">Socket服务端端口</param>
        /// <param name="autorestart">Socket服务端自动重启</param>
        /// <param name="interval"> Socket服务端自动重启时间间隔 单位：毫秒</param>
        public SocketAsyncOfServer(string ip, int port, bool autorestart = false, int interval = 1000)
        {
            this.IP = ip;
            this.Port = port;
            this.AutoRestart = autorestart;
            this.Interval = interval;
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            Pool = new SocketUserTokenPool();
            _Listener = null;
            _Alive = false;
            _FirstStarted = true;
            _NormalClosed = false;           
        }
        #endregion

        #region 创建监听Socket实例
        /// <summary>
        /// 创建监听Socket实例
        /// </summary>
        /// <returns></returns>
        private bool CreatInstanceOfListenerSocket()
        {
            bool done = false;
            try
            {
                IPAddress ip = IPAddress.Parse(this.IP);
                IPEndPoint ipe = new IPEndPoint(ip, (int)this.Port);
                _Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //IPV6
                if (ipe.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _Listener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    _Listener.Bind(new IPEndPoint(IPAddress.IPv6Any, ipe.Port));
                }
                //IPV4
                else
                {
                    _Listener.Bind(ipe);
                }
                //控制客户端连接数量  0:无限制
                _Listener.Listen(0);
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

        #region Socket远程终结点异步通讯
        /// <summary>
        /// Socket远程终结点异步通讯
        /// </summary>
        /// <param name="server"></param>
        private void AsyncCommunicating(Socket socket)
        {
            Task.Factory.StartNew(() =>
            {
                SocketEventArgs arg = new SocketEventArgs()
                {
                    SocketUserToken = new SocketUserToken(socket),
                    Buffer = null,
                    Message = null
                };
                try
                {
                    if (ConnectedToRemoteEndPoint != null) {
                        ConnectedToRemoteEndPoint(this, arg);
                    }
                    Pool.Add(arg.SocketUserToken);
                    while (true)
                    {
                        byte[] bytes = new byte[BufSize];
                        //获取信息
                        int len = socket.Receive(bytes);
                        //转换
                        string msg = Encoding.UTF8.GetString(bytes, 0, len);
                        if (ReceivedFromRemoteEndPoint != null)
                        {
                            arg.Buffer = Encoding.UTF8.GetBytes(msg.ToCharArray(), 0, len);
                            arg.Message = null;
                            ReceivedFromRemoteEndPoint(this, arg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispose((new List<dynamic>() { socket }));
                    //该异常不是由监听Socket的关闭操作引起时
                    if (_Alive)
                    {
                        if (ExceptionOccuredOfRemoteEndPoint != null)
                        {
                            arg.Buffer = null;
                            arg.Message = ex.Message;
                            ExceptionOccuredOfRemoteEndPoint(this, arg);
                        }
                    }
                    //从Socket管理池中移除失效的Socket远程终结点句柄
                    Pool.Remove(arg.SocketUserToken);
                }
            });
        }
        #endregion

        #region 启动监听Socket
        /// <summary>
        /// 启动服务端Socket
        /// </summary>
        public void Start()
        {
            Init();
            Task.Factory.StartNew(() =>
            {             
                while (_FirstStarted || (AutoRestart && !_NormalClosed))
                {
                    _FirstStarted = false;
                    _Alive = CreatInstanceOfListenerSocket();
                    //启动成功
                    if (_Alive)
                    {
                        if (Started != null)
                        {
                            Started(this, new SocketEventArgs() { });
                        }
                    }
                    while (_Alive)
                    {
                        Socket socket = null;
                        try
                        {
                            socket = _Listener.Accept();
                        }
                        catch (Exception ex)
                        {
                            //尝试关闭监听Socket
                            Dispose(new List<dynamic>() { _Listener });
                            //关闭所有Socket远程终结点
                            CloseAllRemoteEndPoints();
                            //等待操作完成
                            while (Pool.Count() > 0)
                            {
                                //wait
                            }
                            _Alive = false;
                            if (Closed != null)
                            {
                                Closed(this, new SocketEventArgs() { Message = ex.Message });
                            }
                            
                        }
                        finally
                        {
                            if (_Alive)
                            {
                                AsyncCommunicating(socket);
                            }
                            else if (AutoRestart && !_NormalClosed)
                            {
                                Thread.Sleep(Interval);
                            }
                        }
                    }
                }
            });
        }
        #endregion

        #region Socket远程终结点数据发送
        /// <summary>
        /// Socket远程终结点数据发送
        /// </summary>
        public void SendToRemoteEndPoint(SocketUserTokenPool pool, byte[] buffer)
        {
            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < pool.Count(); i++)
                {
                    try
                    {
                        pool[i].Socket.Send(buffer);
                    }
                    catch
                    {
                        Dispose((new List<dynamic>() { pool[i].Socket }));
                    }
                }
            });        
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

        #region 关闭所有Socket远程终结点
        /// <summary>
        /// 关闭所有Socket远程终结点
        /// </summary>
        public void CloseAllRemoteEndPoints()
        {
            List<dynamic> res = new List<dynamic>();
            foreach (var item in Pool)
            {
                res.Add(item.Socket);
            }
            Dispose(res);
        }
        #endregion

        #region 关闭Socket服务端
        /// <summary>
        /// 关闭Socket
        /// </summary>
        public void Close()
        {
            _NormalClosed = true;
            Dispose(new List<dynamic>() { _Listener });
        }
        #endregion

        #region 事件(Socket服务端)
        /// <summary>
        /// 用于完成Socket服务端启动操作的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> Started;
        /// <summary>
        /// 用于处理Socket服务端异常发生的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> ExceptionOccured;
        /// <summary>
        /// 用于完成Socket服务端关闭操作的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> Closed;
        #endregion

        #region 事件(Socket远程终结点)
        /// <summary>
        /// 用于完成Socket远程终结点连接操作的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> ConnectedToRemoteEndPoint;
        /// <summary>
        /// 用于完成Socket远程终结点接收操作的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> ReceivedFromRemoteEndPoint;
        /// <summary>
        /// 用于处理Socket远程终结点异常发生的事件
        /// </summary>
        public event EventHandler<SocketEventArgs> ExceptionOccuredOfRemoteEndPoint;
        #endregion
    }
}
