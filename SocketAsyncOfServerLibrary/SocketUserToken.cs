using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketAsyncOfServerLibrary
{
    /// <summary>
    /// 描述服务端Socket远程终结点的令牌
    /// </summary>
    public class SocketUserToken
    {
        /// <summary>
        /// 服务端远程终结点Socket
        /// </summary>
        public Socket Socket { get; set; }
        /// <summary>
        /// 远程终结点
        /// </summary>
        public EndPoint RemoteEndPoint { get; set; }
        /// <summary>
        /// 客户端设备唯一ID
        /// </summary>
        public string MachineUniqueId { get; set; }
        /// <summary>
        /// 客户端账号
        /// </summary>
        public string UserAccount { get; set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="socket">服务端远程终结点Socket</param>
        /// <param name="machineuniqueid">客户端设备唯一ID</param>
        /// <param name="useraccount">客户端账号</param>
        public SocketUserToken(Socket socket, string machineuniqueid = null, string useraccount = null)
        {
            Socket = socket;
            RemoteEndPoint = socket.RemoteEndPoint;
            MachineUniqueId = machineuniqueid;
            UserAccount = useraccount;        
        }
    }
}
