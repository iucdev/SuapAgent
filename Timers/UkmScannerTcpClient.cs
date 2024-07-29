
using System.Net.Sockets;
using System.Net;
using System.Text;
using Serilog;
using System.Text.RegularExpressions;
using suap.miniagent;
using Newtonsoft.Json.Linq;
using static qoldau.suap.miniagent.localDb.SqlLiteDbManager;
using qoldau.suap.miniagent.localDb;

namespace qoldau.suap.miniagent.Timers {


    public class UkmScannerTcpClientTimer0 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer0(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer1 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer1(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer2 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer2(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer3 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer3(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer4 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer4(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer5 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer5(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer6 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer6(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer7 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer7(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer8 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer8(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer9 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer9(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }
    public class UkmScannerTcpClientTimer10 : UkmScannerTcpClientTimer {
        public UkmScannerTcpClientTimer10(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base(techvisionIndicator, ip, port, db, sleepDelay, logger) {
        }
    }

    public abstract class UkmScannerTcpClientTimer : BaseTimerService {
        private readonly TechvisionIndicator _techvisionIndicator;
        private readonly string _ip;
        private readonly int _port;
        private readonly SqlLiteDbManager _db;
        private readonly ILogger _logger; 
        public UkmScannerTcpClientTimer(TechvisionIndicator techvisionIndicator, string ip, int port, SqlLiteDbManager db, TimeSpan sleepDelay, ILogger logger) : base($"{techvisionIndicator.DeviceIndicatorCode}_Timer", new(sleepDelay), logger) {
            _techvisionIndicator = techvisionIndicator;
            _ip = ip;
            _port = port;
            _db = db;
            _logger = logger;
            _scannedValues = new Dictionary<string, UkmScannedValue>();
        }


        private readonly Dictionary<string, UkmScannedValue> _scannedValues;

        public override async Task DoAction() {
            if(_scannedValues.Count > 0) {
                saveScannedValues(_techvisionIndicator);
            }

            _logger.Information($"do work from {this.TimerName}");

            var tcpClient = new TcpClient();
            tcpClient.Connect(new IPEndPoint(IPAddress.Parse(_ip), _port));
            if (!tcpClient.Connected) {
                throw new Exception($"Can't connect to {_ip}: {_port}");
            }

            var response = new byte[1024];

            using (var stm = tcpClient.GetStream()) {
                while (tcpClient.Connected) {
                    if(_scannedValues.Count > 10) {
                        saveScannedValues(_techvisionIndicator);
                    }

                    // Read data from socket stream
                    var bytesCount = await stm.ReadAsync(response, 0, 1024);

                    if (bytesCount > 0) {
                        var responseStr = Encoding.ASCII.GetString(response, 0, bytesCount);

                        _logger.Debug("Received : " + responseStr);
                        var ukmValues = deserializeStampDataMV440(responseStr);
                        foreach (var ukmValue in ukmValues) {
                            if (_scannedValues.ContainsKey(ukmValue.barcodeValue)) {
                                _scannedValues.Add(ukmValue.barcodeValue, new UkmScannedValue(ukmValue.barcodeValue, DateTime.Now));
                            }
                        }

                        _logger.Debug("ukmValues: " + string.Join(", ", ukmValues.Select(x => x.barcodeValue)));
                    }
                }
            }
        }

        private void saveScannedValues(TechvisionIndicator indicator) {
            var allScannedValueKeys = _scannedValues.Select(c => c.Key).ToArray();
            foreach (var scannedValueKey in allScannedValueKeys) {
                _scannedValues.Remove(scannedValueKey, out var scannedValue);
                
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

            _logger.Debug($"UkmScannerTcp: {indicator.DeviceIndicatorCode}: {allScannedValueKeys.Length} values saved to db");
        }

        private (string barcodeValue, int barcodeStatus)[] deserializeStampDataMV440(string rawData) {
            var rawDataWithoutBranches = rawData.Trim().Replace('{', ' ').Replace('}', ' ').ToUpper();
            var fields = rawDataWithoutBranches.Split(new[] { "\r\n", ";" }, StringSplitOptions.RemoveEmptyEntries);

            const string left = "LEFT:";
            const string right = "RIGHT:";

            var ukmValues = new List<(string barcodeValue, int barcodeStatus)>();
            foreach (var field in fields) {
                if (field.Contains(left)) {
                    int length = left.Length;
                    var serial = field.Substring(length, field.Length - length).Trim();
                    var barcodeValue = serial;
                    var barcodeStatus = serial.Length == 2 && Regex.IsMatch(serial, @"^[a-zA-Z]+$") ? 1 : 0;
                    ukmValues.Add((barcodeValue, barcodeStatus));
                } else if (field.Contains(right)) {
                    int length = right.Length;
                    var number = field.Substring(length, field.Length - length).Trim();
                    var barcodeValue = number;
                    var barcodeStatus = number.Length == 9 && Regex.IsMatch(number, @"^[0-9]+$") ? 1 : 0;
                    ukmValues.Add((barcodeValue, barcodeStatus));
                } else {
                    var barcodeValue = field;
                    var barcodeStatus = field.Length == 9 && Regex.IsMatch(field, @"^[0-9]+$") ? 1 : 0;
                    ukmValues.Add((barcodeValue, barcodeStatus));
                }
            }

            return ukmValues.ToArray();
        }
    }

}
