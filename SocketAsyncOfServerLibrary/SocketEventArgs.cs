using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketAsyncOfServerLibrary
{
    public class SocketEventArgs : System.EventArgs
    {
        /// <summary>
        /// Socket远程终结点令牌
        /// </summary>
        public SocketUserToken SocketUserToken { get; set; }
        /// <summary>
        /// Socket数据传输缓存
        /// </summary>
        public byte[] Buffer { get; set; }
        /// <summary>
        /// 获取描述当前异常的消息
        /// </summary>
        public string Message { get; set; }
    }
}
