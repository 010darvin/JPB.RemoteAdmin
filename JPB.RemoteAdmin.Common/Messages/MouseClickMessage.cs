using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPB.RemoteAdmin.Common.Messages
{
    [Serializable]
    public class MouseClickMessage
    {
        public MouseOperations.MouseEventFlags MouseButton { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public double XImage { get; set; }
        public double YImage { get; set; }
    }
}
