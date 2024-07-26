
using System.Data;
using Microsoft.Data.Sqlite;
using Serilog.Core;
using suap.miniagent;

namespace qoldau.suap.miniagent.localDb {
    public class SqlLiteDbManager {

        private readonly string _localDbFolder;
        public SqlLiteDbManager(string localDbFolder) {
            _localDbFolder = localDbFolder;
        }


        private string getPathToDb(DateTime dateTime) {
            var pathToDb = $"{_localDbFolder}/{dateTime:dd_MM_yyyy}_miniagent.db";
            return pathToDb;
        }

        private string getConnStr(DateTime dateTime) {
            var connStr = $"Data Source={getPathToDb(dateTime)}";
            return connStr;
        }

        public void CreateTodayDbIfNotExists() {
            var now = DateTime.Now;

            using (var connection = new SqliteConnection(getConnStr(now))) {
                connection.Open();

                var command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = $@"
CREATE TABLE IF NOT EXISTS TbDeviceValues (
    _id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    flStampDate TEXT NOT NULL, --as ISO8601 strings (""YYYY-MM-DD HH:MM:SS.SSS"")
    flTableName TEXT NOT NULL,
    flJson TEXT NOT NULL,
    flDeviceIndicatorCode TEXT NOT NULL,
    -------------------
    flSentToAlcoTrackDate TEXT, --as ISO8601 strings (""YYYY-MM-DD HH:MM:SS.SSS"")
    flIsSentToAlcoTrack BOOLEAN NOT NULL
);";
                command.ExecuteNonQuery();
            }
        }


        public void Insert(DeviceValue deviceValue) {
            if (!File.Exists(getPathToDb(DateTime.Now))) {
                CreateTodayDbIfNotExists();
            }

            using (var connection = new SqliteConnection(getConnStr(DateTime.Now))) {
                connection.Open();

                var command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = $@"INSERT INTO TbDeviceValues 
(flStampDate, flTableName, flJson, flDeviceIndicatorCode, flSentToAlcoTrackDate, flIsSentToAlcoTrack) VALUES 
(@flStampDate1, @flTableName1, @flJson1, @flDeviceIndicatorCode1, @flSentToAlcoTrackDate1, @flIsSentToAlcoTrack1);";

                command.CommandType = CommandType.Text;
                command.Parameters.AddWithValue("flStampDate1", deviceValue.StampDate.ToString("O"));
                command.Parameters.AddWithValue("flTableName1", deviceValue.TableName.ToString());
                command.Parameters.AddWithValue("flJson1", deviceValue.Json);
                command.Parameters.AddWithValue("flDeviceIndicatorCode1", deviceValue.DeviceIndicatorCode);
                command.Parameters.AddWithValue("flSentToAlcoTrackDate1", deviceValue.SentToAlcoTrackDate?.ToString("O") ?? string.Empty);
                command.Parameters.AddWithValue("flIsSentToAlcoTrack1", deviceValue.IsSentToAlcoTrack.ToString()); 

                command.ExecuteNonQuery();
            }
        }


        public void MarkUsSentToAlcotrack(int id, DateTime sentDate) {
            if (!File.Exists(getPathToDb(DateTime.Now))) {
                CreateTodayDbIfNotExists();
            }

            using (var connection = new SqliteConnection(getConnStr(DateTime.Now))) {
                connection.Open();

                var command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = $@"UPDATE TbDeviceValues 
SET flSentToAlcoTrackDate = @flSentToAlcoTrackDate1, flIsSentToAlcoTrack = 'True'
WHERE _id = @flId";

                command.CommandType = CommandType.Text;
                command.Parameters.AddWithValue("flSentToAlcoTrackDate1", sentDate.ToString("O"));
                command.Parameters.AddWithValue("flId", id);

                command.ExecuteNonQuery();
            }
        }

        public NeedToSendDeviceValue[] GetNotSendedToAlcotrackValues(int count) {
            if (!File.Exists(getPathToDb(DateTime.Now))) {
                CreateTodayDbIfNotExists();
            }

            var values = new List<NeedToSendDeviceValue>();
            using (var connection = new SqliteConnection(getConnStr(DateTime.Now))) {
                connection.Open();

                var command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = $@"
SELECT ""_id"", flTableName, flJson, flDeviceIndicatorCode FROM TbDeviceValues 
WHERE flIsSentToAlcoTrack = 'False'
LIMIT {count};";

                using (var reader = command.ExecuteReader()) {
                    if (reader.HasRows){
                        while (reader.Read()) {
                            var id = reader.GetInt32("_id");
                            var tableName = reader.GetString("flTableName");
                            var json = reader.GetString("flJson");
                            var deviceIndicatorCode = reader.GetString("flDeviceIndicatorCode");

                            values.Add(new NeedToSendDeviceValue(id, Enum.Parse<TableName>(tableName), json, deviceIndicatorCode));
                        }
                    }
                }
            }

            return values.ToArray();
        }

        public record NeedToSendDeviceValue(int Id, TableName TableName, string Json, string DeviceIndicatorCode);
        public record DeviceValue(DateTime StampDate, TableName TableName, string Json, string DeviceIndicatorCode, DateTime? SentToAlcoTrackDate = null, bool IsSentToAlcoTrack = false);
    }
}
