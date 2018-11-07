using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketAsyncOfClientLibrary
{
    public class SocketEventArgs
    {
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
