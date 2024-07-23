namespace qoldau.suap.miniagent.Mitsubishi.Enums {
    // Types with ♀ are not supported by the driver
    // Types with * may use space or * in ASCII protocol frame
    public enum DeviceCodesEnum {
        SM = 0x91, // special relay ♀
        SD = 0xA9, // special register ♀
        X = 0x9C, // In *
        Y = 0x9D, // Out *
        M = 0x90, // Internal relay *
        L = 0x92, // Latch relay *
        F = 0x93, // Annunciator *
        V = 0x94, // Edge relay *
        B = 0xA0, // Link relay *
        D = 0xA8, // Data register *
        W = 0xB4, // Link register *
        TC = 0xC0, // Timer - coil ♀
        TS = 0xC1, // Timer - contact ♀
        TN = 0xC2, // Timer - current ♀
        SS = 0xC7, // Retenrive timer - contact
        SC = 0xC6, // Retenrive timer - coil
        SN = 0xC8, // Retenrive timer - current
        CS = 0xC4, // Counter - contact ♀
        CC = 0xC3, // Counter - coil ♀
        CN = 0xC5, // Counter - current ♀
        SB = 0xA1, // special link relay
        SW = 0xB5, // special link register
        S = 0x98, // step relay ♀ *
        DX = 0xA2, // Direct input ♀
        DY = 0xA3, // Direct output ♀
        Z = 0xCC, // index register ♀ *
        R = 0xAF, // File register *
        ZR = 0xB0 // unknown
    }
}
