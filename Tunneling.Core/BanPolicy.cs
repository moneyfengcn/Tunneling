using System;
using System.Collections.Generic;
using System.Text;

namespace Tunneling.Core
{
    /// <summary>
    /// 封IP的策略
    /// </summary>
    public class BanPolicy
    {
        public TimeSpan Time { get; set; }
        public int Threshold { get; set; } = 0;
    }
}
