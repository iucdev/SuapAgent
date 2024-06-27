using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace suap.miniagent.S7.Types {
    public class DatetimeS7
    {
        public ushort Year;
        public byte Month;
        public byte Day;
        public byte Weekday;
        public byte Hour;
        public byte Minute;
        public byte Second;
        public uint Nanosecond;

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2} {3}:{4}:{5}.{6}", Day, Month, Year, Hour, Minute, Second, Nanosecond);
        }

        public DateTime GetDateTime()
        {
            return new DateTime(Year, Month, Day, Hour, Minute, Second, (int)(Nanosecond / 1000000));
        }

        public int GetSize()
        {
            return System.Runtime.InteropServices.Marshal.SizeOf(this);
        }
    }
}
