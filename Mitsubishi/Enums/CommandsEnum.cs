namespace qoldau.suap.miniagent.Mitsubishi.Enums {
    public enum CommandsEnum {
        BatchRead = 0x0401, // MultipleBlockBatchRead
        BatchWrite = 0x1401,
        RandomRead = 0x0403,
        RandomWirte = 0x1402,
        MonitorDataRegistration = 0x0801,
        Monitor = 0x0802,
        MultipleBlockBatchRead = 0x0406,
        MultipleBlockBatchWrite = 0x1406
    }

    public enum SubcommandsEnum {
        BitUnits = 0x0001,
        WordUnits = 0x0000
    }
}