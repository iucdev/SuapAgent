
namespace qoldau.suap.miniagent.Mitsubishi {
    //    [StructLayout(LayoutKind.Explicit)]
    //    public struct Mc_3EResponcePacket
    //    {
    //        [FieldOffset(0)] public UInt16 Header;
    //        [FieldOffset(2)] public byte NetworkNo;
    //        [FieldOffset(3)] public byte PcNo;
    //        [FieldOffset(4)] public UInt16 RequestDestIONo;
    //        [FieldOffset(6)] public byte RequestDestModuleStationNo;
    //        [FieldOffset(7)] public UInt16 ResponseDataLength;
    //        [FieldOffset(9)] public UInt16 CompleteCode;
    //        [FieldOffset(11)] public fixed byte Data[6];
    //        [FieldOffset(0)] public byte[] SourceData;
    //    }
    public class Mc_3EResponcePacket {
        public Mc_3EResponcePacket(byte[] receivedData) {
            InitilizePacket(receivedData);
        }

        public UInt16 Header;

        public byte NetworkNo;

        public byte PcNo;

        public UInt16 RequestDestIoNo;

        public byte RequestDestModuleStationNo;

        public UInt16 ResponseDataLength;

        public UInt16 CompleteCode;

        public byte[] Data;

        private void InitilizePacket(byte[] receivedData) {
            Header = BitConverter.ToUInt16(receivedData.Take(2).ToArray(), 0);
            NetworkNo = receivedData[2];
            PcNo = receivedData[3];
            RequestDestIoNo = BitConverter.ToUInt16(receivedData.Skip(4).Take(2).ToArray(), 0);
            RequestDestModuleStationNo = receivedData[6];
            ResponseDataLength = BitConverter.ToUInt16(receivedData.Skip(7).Take(2).ToArray(), 0);
            CompleteCode = BitConverter.ToUInt16(receivedData.Skip(9).Take(2).ToArray(), 0);
            Data = receivedData.Skip(11).Take(ResponseDataLength - 2).ToArray();
        }
    }
}
