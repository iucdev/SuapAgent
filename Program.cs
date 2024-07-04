using System.Collections.Concurrent;
using System.IO.Ports;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using AlcotrackApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NModbus.Utility;
using qoldau.suap.miniagent.localDb;
using Serilog;
using Serilog.Core;
using suap.miniagent.S7.Types;
using static qoldau.suap.miniagent.localDb.SqlLiteDbManager;

namespace suap.miniagent {
    public class Program {

        public static async Task Main(string[] args) {
            try {
                var configJson = File.ReadAllText("appsettings.json");
                var config = JsonConvert.DeserializeObject<Config>(configJson);
                var db = new SqlLiteDbManager(config.LocalDbFolder);

                var loggerConfiguration = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File(config.LogsPath, rollingInterval: RollingInterval.Day);

                if (config.ShowDebugLogs) {
                    loggerConfiguration.MinimumLevel.Debug();
                } else {
                    loggerConfiguration.MinimumLevel.Information();
                }

                using var logger = loggerConfiguration.CreateLogger();
                var ukmScannerManager = new UkmScannerManager(db, logger);


                logger.Information("Starting...");



                while (true) {
                    //1. создание локальной sqlite базы на каждый день
                    db.CreateTodayDbIfNotExists(logger);

                    #region чтение данных
                    foreach (var device in config.Devices) {
                        try {

                            //2. читаем данные и записываем в локальную бд sqlite
                            switch (device.Type) {
                                case DeviceType.Plc:
                                case DeviceType.Energy: {
                                    readDataFromDeviceAndSave(device, db, logger);
                                    break;
                                }
                                case DeviceType.Techvision: {
                                    startScanningAndSavingUkm(ukmScannerManager, device);
                                    break;
                                }
                                default:
                                    throw new NotImplementedException(device.Type.ToString());
                            }

                        } catch (Exception readDataFromDeviceEx) {
                            logger.Error($"readDataFromDeviceAndSave error -> {readDataFromDeviceEx}");
                        }
                    }
                    #endregion

                    #region отправка данных в AlcotrackApi
                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.BearerTokenFromAlcotrack);
                    var alcotrackApiClient = new AlcotrackApiClient(httpClient) { BaseUrl = config.AlcotrackApiUrl };
                    try {
                        var sentValuesCount = 0;
                        var needToSendDeviceValues = db.GetNotSendedToAlcotrackValues(config.SentToAlcotrackValuesCount);
                        foreach (var needToSendDeviceValue in needToSendDeviceValues) {
                            //3. Отправляем данные из локальной sqlite бд в AlcoTrack Api
                            await sentFromLocalDbToAlcotrack(db, needToSendDeviceValue, alcotrackApiClient);
                            sentValuesCount++;
                        }
                        logger.Information($"Sent values count to Alcotrack Api -> {sentValuesCount}");
                    } catch (Exception sentFromLocalDbToAlcotrackEx) {
                        logger.Error($"sentFromLocalDbToAlcotrack error -> {sentFromLocalDbToAlcotrackEx}");
                    }
                    #endregion


                    logger.Information($"Sleep. Start after -> {config.SleepIntervalInMs} ms");
                    Thread.Sleep(config.SleepIntervalInMs);
                }

            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Невозможно продолжить работу агента");
                Console.WriteLine(e.ToString());
            }
            
            Console.ReadLine();
        }

        private static async Task sentFromLocalDbToAlcotrack(SqlLiteDbManager db, NeedToSendDeviceValue needToSendDeviceValue, AlcotrackApiClient alcotrackApiClient) {
            SuapApiResponse response;
            switch (needToSendDeviceValue.TableName) {
                case TableName.TbAccBeerCounter: {
                    var m = JsonConvert.DeserializeObject<SuapBeerApiModel>(needToSendDeviceValue.Json);
                    response = await alcotrackApiClient.SendBeerCounterAsync(m);
                    break;
                }
                case TableName.TbAccColumnStateCounter: {
                    var m = JsonConvert.DeserializeObject<SuapColumnStateApiModel>(needToSendDeviceValue.Json);
                    response = await alcotrackApiClient.SendColumnStateCounterAsync(m);
                    break;
                }
                case TableName.TbAccEnergyCounter: {
                    var m = JsonConvert.DeserializeObject<SuapEnegeryApiModel>(needToSendDeviceValue.Json);
                    response = await alcotrackApiClient.SendEnegeryCounterAsync(m);
                    break;
                }
                case TableName.TbAccPackCounter: {
                    var m = JsonConvert.DeserializeObject<SuapPackApiModel>(needToSendDeviceValue.Json);
                    response = await alcotrackApiClient.SendPackCounterAsync(m);
                    break;
                }
                case TableName.TbAccProductCounter: {
                    var m = JsonConvert.DeserializeObject<SuapProductApiModel>(needToSendDeviceValue.Json);
                    response = await alcotrackApiClient.SendProductCounterAsync(m);
                    break;
                }
                case TableName.TbAccReservoirCounter: {
                    var m = JsonConvert.DeserializeObject<SuapReservoirApiModel>(needToSendDeviceValue.Json);
                    response = await alcotrackApiClient.SendReservoirCounterAsync(m);
                    break;
                }
                case TableName.TbAccStampInfo: {
                    var m = JsonConvert.DeserializeObject<SuapStampInfoApiModel>(needToSendDeviceValue.Json);
                    response = await alcotrackApiClient.SendStampInfoAsync(m);
                    break;
                }
                default:
                    throw new NotImplementedException();
            }

            if (response.Code != 200) {
                throw new Exception(response.Message);
            }

            db.MarkUsSentToAlcotrack(needToSendDeviceValue.Id, sentDate: DateTime.Now);
        }

        private static void readDataFromDeviceAndSave(Device device, SqlLiteDbManager db, Logger logger) {
            var deviceValues = device.Type switch {
                DeviceType.Plc => readDataFromPlc(device, logger),
                DeviceType.Energy => readDataFromEnergyCounter(device, logger),
                DeviceType.Techvision => throw new NotImplementedException("Данные с сканеров УКМ нужно считывать постоянно через serialPort"),
                _ => throw new NotImplementedException(),
            };


            foreach (var deviceValue in deviceValues) {
                db.Insert(deviceValue);
            }
            logger.Information($"Inserted in local db rows count -> {deviceValues.Length}");
        }
        private static void startScanningAndSavingUkm(UkmScannerManager ukmScannerManager, Device device) {
            foreach (var indicator in device.TechvisionIndicators) {
                if (!ukmScannerManager.HasScanStarted(indicator)) {
                    ukmScannerManager.StartScanning(device.ComConfig, indicator);
                }

                if(ukmScannerManager.GetScannedValuesCount(indicator) > 10) {
                    ukmScannerManager.SaveScannedValues(indicator);
                }
            }
        }


        #region readDataFromPlc
        private static DeviceValue[] readDataFromPlc(Device device, Logger logger) {
            var bytesFromDevice = device.Model switch {
                "S71200" => getBytesFromS71200(device.TcpConfig, logger),
                _ => throw new NotImplementedException()
            };
            logger.Debug($"bytes from device in Base64 -> {Convert.ToBase64String(bytesFromDevice)}");

            var deviceValues = new List<DeviceValue>();
            foreach (var indicator in device.PlcIndicators) {
                var deviceIndicatorCode = indicator.DeviceIndicatorCode;
                var tableName = indicator.TableName;

                var j = new JObject();
                j["Guid"] = Guid.NewGuid().ToString();
                j["DeviceIndicatorCode"] = deviceIndicatorCode;

                foreach (var field in indicator.Fields) {

                    if (field.HardValue != null) {
                        j[field.Name] = JValue.Parse(field.HardValue.ToString());
                    } else {
                        var value = Class.FromBytesToType(field.DataType.ToString(), bytesFromDevice, field.NeedToSkipBytesFromStart, isDirectOrder: false);
                        j[field.Name] = field.DataType switch {
                            FieldDataType.Int16 => (short)value,
                            FieldDataType.UInt16 => (ushort)value,

                            FieldDataType.Int32 => (int)value,
                            FieldDataType.UInt32 => (uint)value,

                            FieldDataType.Int64 => (long)value,
                            FieldDataType.UInt64 => (ulong)value,

                            FieldDataType.Float => (float)value,
                            FieldDataType.Double => (double)value,

                            FieldDataType.DatetimeS7 =>
                                //не у всех заводов нормально настроены дата и время на plc
                                indicator.UsePlcDateTime
                                    ? ((DatetimeS7)value).GetDateTime()
                                    : DateTime.Now,

                            _ => throw new NotImplementedException(field.DataType.ToString())
                        };
                    }


                    logger.Debug($"{indicator.DeviceIndicatorCode} -> {field.Name}: {j[field.Name]}");
                }

                deviceValues.Add(new DeviceValue(DateTime.Now, tableName, j.ToString(), deviceIndicatorCode));
            }

            return deviceValues.ToArray();
        }

        private static byte[] getBytesFromS71200(TcpConfig config, Logger logger) {
            //only for test
            //return Convert.FromBase64String("AFU/aQBVNclBS0reAAAAAEFJ85LAAAAAQTSQ4cRgRA4+0sL/Qb0QwD9xlUsH6AYTBA8UAwAAAAAA");
            //return Convert.FromBase64String("BvVdvvRIBvGQ8/NrTGEV1EbyOAA/ckhRP3Jp3Eu35NFGcjgAQiDkakIioRpBr1mGQZ/LIwfoBwQFChwMCbLZYAfoBwMEFw==");

            var device = new S7Series(CpuTypeEnum.S71200, config.Ip, config.Port, config.Rack, config.Slot);
            var openRes = device.Open();
            logger.Debug($"S71200: device.Open(): {openRes}");
            logger.Debug($"S71200: device.IsConnected: {device.IsConnected}");
            logger.Debug($"S71200: device.IsAvailable: {device.IsAvailable}");
            var resBytes = device.ReadBytes(startByteAdr: 0, count: Convert.ToInt32(config.ReadBytesCount), config.Db);
            logger.Information($"Reading {config.ReadBytesCount} bytes from S71200 -> {config.Ip}:{config.Port}");
            return resBytes;
        }
        #endregion


        #region readDataFromEnergyCounter
        private static DeviceValue[] readDataFromEnergyCounter(Device device, Logger logger) {
            var deviceValues = new List<DeviceValue>();
            foreach (var indicator in device.EnergyIndicators) {
                var deviceIndicatorCode = indicator.DeviceIndicatorCode;
                var tableName = indicator.TableName;

                var totalKw = device.Model switch {
                    "Mercury230" => getGetTotalKwFromMercury230(device.ComConfig, indicator, logger),
                    "Energomera301" => getGetTotalKwFromEnergomera301(device.ComConfig, indicator, logger),
                    _ => throw new NotImplementedException()
                };

                var now = DateTime.Now;

                var j = new JObject();
                j["Guid"] = Guid.NewGuid().ToString();
                j["DeviceIndicatorCode"] = deviceIndicatorCode;
                j["TotalKw"] = totalKw;
                j["StampDate"] = now;

                logger.Debug($"{indicator.DeviceIndicatorCode} -> TotalKw: {totalKw}");

                deviceValues.Add(new DeviceValue(now, tableName, j.ToString(), deviceIndicatorCode));
            }
            return deviceValues.ToArray();
        }

        public static uint getGetTotalKwFromMercury230(ComConfig config, EnergyIndicator energyIndicator, Logger logger) {
            using var port = new SerialPort(config.PortName);
            port.BaudRate = config.BaudRate;
            port.DataBits = config.DataBits;
            port.Parity = config.Parity;
            port.StopBits = config.StopBits;
            port.Open();

            var address = energyIndicator.Mercury230DeviceAddress;
            byte[] crcLink = ModbusUtility.CalculateCrc(new byte[] { address, 01, 1, 1, 1, 1, 1, 1, 1 });
            var byteLinkArray = new List<byte> { address, 01, 1, 1, 1, 1, 1, 1, 1 };
            byteLinkArray.AddRange(crcLink);
            sendComCmd(port, byteLinkArray);

            byte reqNumber = 5;
            byte listNumber = 0x00;
            byte tarif = 0;
            byte[] crc = ModbusUtility.CalculateCrc(new[] { address, reqNumber, listNumber, tarif });
            var byteArray = new List<byte> { address, reqNumber, listNumber, tarif };
            byteArray.AddRange(crc);
            var total = sendComCmd(port, byteArray);
            logger.Information($"Energy: {energyIndicator.DeviceIndicatorCode} reading from Mercury230 -> {config.PortName}|{config.BaudRate}|{address}");
            var data = getTotalKW(total);
            data = data / 1000;

            return data;
        }   
        public static uint getGetTotalKwFromEnergomera301(ComConfig config, EnergyIndicator energyIndicator, Logger logger) {
            using var port = new SerialPort(config.PortName);
            port.BaudRate = config.BaudRate;
            port.DataBits = config.DataBits;
            port.Parity = config.Parity;
            port.StopBits = config.StopBits;
            port.Open();

            sendComCmd(port, new List<byte> { 0x2F, 0X3F, 0X21, 0X0D, 0X0A });
            sendComCmd(port, new List<byte> { 0X06, 0X30, 0X35, 0X31, 0X0D, 0X0A });
            sendComCmd(port, new List<byte> { 0X01, 0X50, 0X31, 0X02, 0X28, 0X37, 0X37, 0X37, 0X37, 0X37, 0X37, 0X29, 0X03, 0X21 });
            var raw = sendComCmd(port, new List<byte> { 0X01, 0X52, 0X31, 0X02, 0X45, 0X54, 0X30, 0X50, 0X45, 0X28, 0X29, 0X03, 0X37 });
            sendComCmd(port, new List<byte> { 0X01, 0X42, 0X30, 0X03, 0X75 });
            
            logger.Information($"Energy: {energyIndicator.DeviceIndicatorCode} reading from Energomera301 -> {config.PortName}|{config.BaudRate}");

            var output = Encoding.ASCII.GetString(raw).Split('(', '.')[1];
            var data = uint.Parse(output);

            return data;
        }

        private static byte[] sendComCmd(SerialPort port, List<byte> cmd) {
            port.Write(cmd.ToArray(), 0, cmd.Count);
            byte[] answer = { };
            System.Threading.Thread.Sleep(1000);
            if (port.BytesToRead > 0) {
                answer = new byte[(int)port.BytesToRead];
                port.Read(answer, 0, port.BytesToRead);
            }
            return answer;
        }
       
        private static uint getTotalKW(byte[] bytes) {
            byte[] data = bytes.Skip(1).ToArray();
            data = data.Reverse().Skip(2).ToArray();
            data = data.Reverse().ToArray();

            int totalKw =
                BitConverter.ToInt32(
                    new byte[]
                    {
                        data.Skip(1).Take(1).FirstOrDefault(), data.Skip(0).Take(1).FirstOrDefault(),
                        data.Skip(3).Take(1).FirstOrDefault(), data.Skip(2).Take(1).FirstOrDefault()
                    }.Reverse()
                        .ToArray(), 0);
            return unchecked((uint)totalKw);
        }
       
        #endregion

    }


    #region config models

    public class Config {
        public int SentToAlcotrackValuesCount { get; set; }
        public string AlcotrackApiUrl { get; set; }
        public string BearerTokenFromAlcotrack { get; set; }
        public string LocalDbFolder { get; set; }
        public string LogsPath { get; set; }
        public int SleepIntervalInMs { get; set; }
        public bool ShowDebugLogs { get; set; }
        public Device[] Devices { get; set; }
    }
    public class Device {
        public string Model { get; set; }
        public DeviceType Type { get; set; }
        public TcpConfig TcpConfig { get; set; }
        public ComConfig ComConfig { get; set; }
        public TechvisionIndicator[] TechvisionIndicators { get; set; }
        public EnergyIndicator[] EnergyIndicators { get; set; }
        public PlcIndicator[] PlcIndicators { get; set; }
    }
    public enum DeviceType {
        Plc,
        Energy,
        Techvision
    }
    public class TcpConfig {
        public string Ip { get; set; }
        public ushort Port { get; set; }
        public short Rack { get; set; }
        public short Slot { get; set; }
        public int Db { get; set; }
        public float ReadBytesCount { get; set; }
    }
    public class ComConfig {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
    }
    public class TechvisionIndicator {
        public string DeviceIndicatorCode { get; set; }
        public TableName TableName { get; set; }
    } 
    public class EnergyIndicator {
        public string DeviceIndicatorCode { get; set; }
        public TableName TableName { get; set; }
        public byte Mercury230DeviceAddress { get; set; }
    }
    public class PlcIndicator {
        public string DeviceIndicatorCode { get; set; }
        public bool UsePlcDateTime { get; set; }
        public TableName TableName { get; set; }
        public IndicatorField[] Fields { get; set; }
    }
    public class IndicatorField {
        public string Name { get; set; }
        public float NeedToSkipBytesFromStart { get; set; }
        public object HardValue { get; set; }
        public FieldDataType DataType { get; set; }
    }
    public enum FieldDataType {
        //Boolean,
        Byte,
        UInt16,
        Int16,
        UInt32,
        Int32,
        Float,
        Double,
        UInt64,
        Int64,
        DatetimeS7,
    }
    public enum TableName {
        TbAccBeerCounter,
        TbAccColumnStateCounter,
        TbAccEnergyCounter,
        TbAccPackCounter,
        TbAccProductCounter,
        TbAccReservoirCounter,
        TbAccStampInfo,
    }

    #endregion


    #region UkmScanerSerialPort

    public class UkmScannerManager {
        private readonly Dictionary<string, UkmScannerSerialPort> _ukmScannerSerialPorts;
        private readonly SqlLiteDbManager _db;
        private readonly Logger _logger;

        public UkmScannerManager(SqlLiteDbManager db, Logger logger) {
            _db = db;
            _logger = logger;
            _ukmScannerSerialPorts = new Dictionary<string, UkmScannerSerialPort>();
        }


        public bool HasScanStarted(TechvisionIndicator indicator) {
            bool hasScanStarted;

            if (!_ukmScannerSerialPorts.ContainsKey(indicator.DeviceIndicatorCode)) {
                hasScanStarted =  false;
            } else {
                var scanner = _ukmScannerSerialPorts[indicator.DeviceIndicatorCode];
                hasScanStarted =  scanner.IsOpen;
            }

            _logger.Debug($"UkmScanner: {indicator.DeviceIndicatorCode}: HasScanStarted: {hasScanStarted}");
            return hasScanStarted;
        }


        public void StartScanning(ComConfig comConfig, TechvisionIndicator indicator) {
            if (!_ukmScannerSerialPorts.ContainsKey(indicator.DeviceIndicatorCode)) {
                var newScanner = new UkmScannerSerialPort(indicator.DeviceIndicatorCode, comConfig.PortName, comConfig.BaudRate, _logger);
                _ukmScannerSerialPorts.Add(indicator.DeviceIndicatorCode, newScanner);
                _logger.Debug($"UkmScanner: {indicator.DeviceIndicatorCode}: new UKM scanner added");
            }


            var scanner = _ukmScannerSerialPorts[indicator.DeviceIndicatorCode];
            scanner.OpenAndStartListen();
        }

        public int GetScannedValuesCount(TechvisionIndicator indicator) {
            var scanner = _ukmScannerSerialPorts[indicator.DeviceIndicatorCode];
            return scanner.ScannedValues.Count;
        }

        public void SaveScannedValues(TechvisionIndicator indicator) {
            var scanner = _ukmScannerSerialPorts[indicator.DeviceIndicatorCode];

            var allScannedValueKeys = scanner.ScannedValues.Select(c => c.Key).ToArray();
            foreach (var scannedValueKey in allScannedValueKeys) {
                scanner.ScannedValues.Remove(scannedValueKey, out var scannedValue);
                //insert scannedValue

                var j = new JObject();
                j["Guid"] = Guid.NewGuid().ToString();
                j["DeviceIndicatorCode"] = indicator.DeviceIndicatorCode;
                j["StampDate"] = scannedValue.Timestamp;
                j["Serial"] = "";
                j["SerialStatus"] = 0;
                j["SequenceNumber"] = "";
                j["SequenceNumberStatus"] = 0;
                j["BarcodeValue"] = scannedValue.Barcode;
                j["BarcodeStatus"] = 0;
                var deviceValue = new DeviceValue(scannedValue.Timestamp, indicator.TableName, j.ToString(), indicator.DeviceIndicatorCode);
                _db.Insert(deviceValue);
            }

            _logger.Debug($"UkmScanner: {indicator.DeviceIndicatorCode}: {allScannedValueKeys.Length} values saved to db");
        }
    }

    public class UkmScannerSerialPort : SerialPort {

        public readonly ConcurrentDictionary<string, UkmScannedValue> ScannedValues;

        private readonly string _deviceName;
        private readonly Logger _logger;
        public UkmScannerSerialPort(string deviceName, string portName, int baudRate, Logger logger) : base() {
            ScannedValues = new ConcurrentDictionary<string, UkmScannedValue>();
            _deviceName = deviceName;
            base.PortName = portName;
            base.BaudRate = baudRate;
            _logger = logger;

            base.DataReceived += serialPort_DataReceived;
        }

        public void OpenAndStartListen() {
            if (!base.IsOpen) {
                base.Open();
                _logger.Debug($"UkmScanner: {_deviceName}: port is opened");
            }
        }


        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e) {
            var barCodeContent = this.ReadLine();
            _logger.Debug($"UkmScanner: {_deviceName}: scanned value {barCodeContent}");

            if (!ScannedValues.ContainsKey(barCodeContent)) {
                if (!ScannedValues.TryAdd(barCodeContent, new UkmScannedValue(barCodeContent, Timestamp: DateTime.Now))) {
                    _logger.Error($"UkmScanner: {_deviceName}: can't add scanned value: {barCodeContent}");
                }
            }

        }
    }

    public record UkmScannedValue(string Barcode, DateTime Timestamp);

    #endregion

    #region S71200
    public class S7Series {

        [NonSerialized]
        private Socket _mSocket; //TCP connection to device

        public string Ip { get; set; }
        public ushort Port { get; set; }

        public CpuTypeEnum Cpu { get; set; }
        public Int16 Rack { get; set; }
        public Int16 Slot { get; set; }

        public bool IsAvailable {
            get {
                try {
                    Ping ping = new Ping();
                    PingReply result = ping.Send(Ip, timeout: 3 * 1000);
                    return result != null && result.Status == IPStatus.Success;
                } catch (Exception e) {
                    Console.WriteLine(e);
                    return false;
                }
            }
        }

        public bool IsDirectOrderOfBytes {
            get { return false; }
        }

        public bool IsConnected { get; private set; }
        public string LastErrorString { get; private set; }
        public PlcErrorCodesEnum LastErrorCode { get; private set; }


        /// <summary>
        /// S7300       Rack=0, Slot=2
        /// S71200/1500 Rack=0, Slot=0
        /// </summary>
        public S7Series(CpuTypeEnum cpu, string ip, ushort port, Int16 rack, Int16 slot) {
            IsConnected = false;
            Ip = ip;
            Port = port;
            Cpu = cpu;
            Rack = rack;
            Slot = slot;
        }

        public bool Open() {
            //_logger.Debug("S7 before opened!");
            var bReceive = new byte[256];

            // check if available
            if (!IsAvailable) {
                LastErrorString = ErrorCode.IpAddressNotAvailable + $"Destination IP-Address '{Ip}' is not available!";
                return (IsConnected = false);
            }

            try {
                // open the channel
                _mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
                _mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);

                var server = new IPEndPoint(IPAddress.Parse(Ip), Port);
                _mSocket.Connect(server);
            } catch (Exception ex) {
                LastErrorString = ErrorCode.ConnectionError + ex.Message;
                return (IsConnected = false);
            }

            try {
                byte[] bSend1 = { 3, 0, 0, 22, 17, 224, 0, 0, 0, 46, 0, 193, 2, 1, 0, 194, 2, 3, 0, 192, 1, 9 };

                switch (Cpu) {
                    case CpuTypeEnum.S7200:
                        //S7200: Chr(193) & Chr(2) & Chr(16) & Chr(0) 'Eigener Tsap
                        bSend1[11] = 193;
                        bSend1[12] = 2;
                        bSend1[13] = 16;
                        bSend1[14] = 0;
                        //S7200: Chr(194) & Chr(2) & Chr(16) & Chr(0) 'Fremder Tsap
                        bSend1[15] = 194;
                        bSend1[16] = 2;
                        bSend1[17] = 16;
                        bSend1[18] = 0;
                        break;
                    case CpuTypeEnum.S71200:
                    case CpuTypeEnum.S7300:
                        //S7300: Chr(193) & Chr(2) & Chr(1) & Chr(0)  'Eigener Tsap
                        bSend1[11] = 193;
                        bSend1[12] = 2;
                        bSend1[13] = 1;
                        bSend1[14] = 0;
                        //S7300: Chr(194) & Chr(2) & Chr(3) & Chr(2)  'Fremder Tsap
                        bSend1[15] = 194;
                        bSend1[16] = 2;
                        bSend1[17] = 3;
                        bSend1[18] = (byte)(Rack * 2 * 16 + Slot);
                        break;
                    case CpuTypeEnum.S7400:
                        //S7400: Chr(193) & Chr(2) & Chr(1) & Chr(0)  'Eigener Tsap
                        bSend1[11] = 193;
                        bSend1[12] = 2;
                        bSend1[13] = 1;
                        bSend1[14] = 0;
                        //S7400: Chr(194) & Chr(2) & Chr(3) & Chr(3)  'Fremder Tsap
                        bSend1[15] = 194;
                        bSend1[16] = 2;
                        bSend1[17] = 3;
                        bSend1[18] = (byte)(Rack * 2 * 16 + Slot);
                        break;
                    default:
                        return (IsConnected = false);
                }

                _mSocket.Send(bSend1, 22, SocketFlags.None);
                Thread.Sleep(1000);
                if (_mSocket.Receive(bReceive, 22, SocketFlags.None) != 22) {
                    //_mSocket.Receive(bReceive, 22, SocketFlags.None).
                    throw new Exception(ErrorCode.WrongNumberReceivedBytes.ToString());
                }

                byte[] bsend2 = { 3, 0, 0, 25, 2, 240, 128, 50, 1, 0, 0, 255, 255, 0, 8, 0, 0, 240, 0, 0, 3, 0, 3, 1, 0 };
                _mSocket.Send(bsend2, 25, SocketFlags.None);

                if (_mSocket.Receive(bReceive, 27, SocketFlags.None) != 27) {
                    throw new Exception(ErrorCode.WrongNumberReceivedBytes.ToString());
                }
            } catch (Exception e) {
                LastErrorString = ErrorCode.ConnectionError + $"Couldn't establish the connection to {Ip}! Exception {e.StackTrace}";
                IsConnected = false;
                return (IsConnected = false);
            }

            return IsConnected = true;
        }

        public bool Close() {
            if (_mSocket != null && _mSocket.Connected) {
                _mSocket.Close();
            }
            return (IsConnected = false);
        }

        public byte[] ReadBytes(int startByteAdr, int count, int db = 0) {
            //_logger.Debug("FAKE ReadBytes");
            return ReadBytes(DataType.DataBlock, db, startByteAdr, count);
        }

        /// <summary>
        /// Read a class from plc. Only properties are readed
        /// </summary>
        /// <param name="sourceClass">Instance of the class that will store the values</param>       
        /// <param name="db">Index of the DB; es.: 1 is for DB1</param>
        public void ReadClass(object sourceClass, int db) {
            Type classType = sourceClass.GetType();
            var numBytes = Class.GetClassSize(classType);
            // now read the package. DATABLOCK?
            var bytes = (byte[])Read(DataType.DataBlock, db, 0, VarType.Byte, numBytes);
            // and decode it
           Class.FromBytes(sourceClass, classType, bytes);
        }

        public void ReadClass(object sourceClass, int db, int startByteAdr) {
            Type classType = sourceClass.GetType();
            int numBytes = Class.GetClassSize(classType);
            // now read the package
            var bytes = (byte[])Read(DataType.DataBlock, db, startByteAdr, VarType.Byte, numBytes);
            //foreach (var b in bytes)
            //    _logger.Debug("byte : {0:X2}", b);

            // and decode it
            Class.FromBytes(sourceClass, classType, bytes);
        }


        private object Read(DataType dataType, int db, int startByteAdr, VarType varType, int varCount) {
            byte[] bytes = null;
            int cntBytes = 0;

            switch (varType) {
                case VarType.Byte:
                    cntBytes = varCount;
                    if (cntBytes < 1)
                        cntBytes = 1;
                    bytes = ReadBytes(dataType, db, startByteAdr, cntBytes);
                    if (bytes == null)
                        return null;
                    if (varCount == 1)
                        return bytes[0];
                    else
                        return bytes;
                case VarType.Word:
                    cntBytes = varCount * 2;
                    bytes = ReadBytes(dataType, db, startByteAdr, cntBytes);
                    if (bytes == null)
                        return null;

                    if (varCount == 1)
                        return Word.FromByteArray(bytes);
                    else
                        return Word.ToArray(bytes);
                case VarType.Int:
                    cntBytes = varCount * 2;
                    bytes = ReadBytes(dataType, db, startByteAdr, cntBytes);
                    if (bytes == null)
                        return null;

                    if (varCount == 1)
                        return Int.FromByteArray(bytes);
                    else
                        return Int.ToArray(bytes);
                case VarType.DWord:
                    cntBytes = varCount * 4;
                    bytes = ReadBytes(dataType, db, startByteAdr, cntBytes);
                    if (bytes == null)
                        return null;

                    if (varCount == 1)
                        return DWord.FromByteArray(bytes);
                    else
                        return DWord.ToArray(bytes);
                case VarType.DInt:
                    cntBytes = varCount * 4;
                    bytes = ReadBytes(dataType, db, startByteAdr, cntBytes);
                    if (bytes == null)
                        return null;

                    if (varCount == 1)
                        return DInt.FromByteArray(bytes);
                    else
                        return DInt.ToArray(bytes);
                case VarType.Real:
                    cntBytes = varCount * 4;
                    bytes = ReadBytes(dataType, db, startByteAdr, cntBytes);
                    if (bytes == null)
                        return null;

                    if (varCount == 1)
                        return S7.Types.Double.FromByteArray(bytes);
                    else
                        return S7.Types.Double.ToArray(bytes);
                case VarType.String:
                    cntBytes = varCount;
                    bytes = ReadBytes(dataType, db, startByteAdr, cntBytes);
                    if (bytes == null)
                        return null;

                    return S7.Types.String.FromByteArray(bytes);
                case VarType.Timer:
                    cntBytes = varCount * 2;
                    bytes = ReadBytes(dataType, db, startByteAdr, cntBytes);
                    if (bytes == null)
                        return null;

                    if (varCount == 1)
                        return S7.Types.Timer.FromByteArray(bytes);
                    else
                        return S7.Types.Timer.ToArray(bytes);
                case VarType.Counter:
                    cntBytes = varCount * 2;
                    bytes = ReadBytes(dataType, db, startByteAdr, cntBytes);
                    if (bytes == null)
                        return null;

                    if (varCount == 1)
                        return Counter.FromByteArray(bytes);
                    else
                        return Counter.ToArray(bytes);
                default:
                    return null;
            }
        }


        #region OldReadBytes
        private byte[] ReadBytes(DataType dataType, int db, int startByteAdr, int count) {
            var bytes = new byte[count];
            try {
                // first create the header
                const int packageSize = 31;
                var package = new ByteArray(packageSize);

                package.Add(new byte[] { 0x03, 0x00, 0x00 });
                package.Add((byte)packageSize);
                package.Add(new byte[]
                {
                    0x02, 0xf0, 0x80,
                    0x32, 0x01, 0x00,
                    0x00, 0x00, 0x00,
                    0x00, 0x0e, 0x00,
                    0x00, 0x04, 0x01,
                    0x12, 0x0a, 0x10
                });
                // package.Add(0x02);  // datenart
                switch (dataType) {
                    case DataType.Timer:
                    case DataType.Counter:
                        package.Add((byte)dataType);
                        break;
                    default:
                        package.Add(0x02);
                        break;
                }

                package.Add(Word.ToByteArray((ushort)(count)));
                package.Add(Word.ToByteArray((ushort)(db)));
                package.Add((byte)dataType);
                package.Add((byte)0);
                switch (dataType) {
                    case DataType.Timer:
                    case DataType.Counter:
                        package.Add(Word.ToByteArray((ushort)(startByteAdr)));
                        break;
                    default:
                        package.Add(Word.ToByteArray((ushort)(startByteAdr * 8)));
                        break;
                }

                _mSocket.Send(package.array, package.array.Length, SocketFlags.None);

                var bReceive = new byte[256];
                int numReceived = _mSocket.Receive(bReceive, 256, SocketFlags.None);

                if (bReceive[21] != 0xff)
                    throw new Exception(ErrorCode.WrongNumberReceivedBytes.ToString());

                for (int cnt = 0; cnt < count; cnt++)
                    bytes[cnt] = bReceive[cnt + 25];

                return bytes;
            } catch (SocketException socketException) {
                IsConnected = false;
                LastErrorString = ErrorCode.WriteData + socketException.ToString();
                return null;
            } catch (Exception exc) {
                LastErrorString = ErrorCode.WriteData + exc.ToString();
                return null;
            }
        }

        #endregion
        
        public void Dispose() {
            if (_mSocket != null) {
                if (_mSocket.Connected) {
                    _mSocket.Close();
                }
            }
        }

        public object Clone() {
            return this.MemberwiseClone();
        }
    }
    public enum CpuTypeEnum {
        S7200 = 0,
        S7300 = 10,
        S7400 = 20,
        S71200 = 30,
    }
    public enum PlcErrorCodesEnum {
        NoError = 0,
        WrongCpuType = 1,
        ConnectionError = 2,
        IpAddressNotAvailable = 3,

        WrongVarFormat = 10,
        WrongNumberReceivedBytes = 11,

        SendData = 20,
        ReadData = 30,

        WriteData = 50
    }
    public enum ErrorCode {
        NoError = 0,
        WrongCpuType = 1,
        ConnectionError = 2,
        IpAddressNotAvailable = 3,

        WrongVarFormat = 10,
        WrongNumberReceivedBytes = 11,

        SendData = 20,
        ReadData = 30,

        WriteData = 50
    }
    public enum DataType {
        Input = 129,
        Output = 130,
        Memory = 131,
        DataBlock = 132,
        Timer = 29,
        Counter = 28
    }
    public enum VarType {
        Bit,
        Byte,
        Word,
        DWord,
        Int,
        DInt,
        Real,
        String,
        Timer,
        Counter
    }
    public class BlockField {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DeviceIndicatorCode { get; set; }
        public string IndicatorProperty { get; set; }
        public string Type { get; set; }
        public int OrderN { get; set; }

        public object Value { get; set; }
    }
    #endregion

}
