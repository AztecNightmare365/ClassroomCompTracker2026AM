using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using ClassroomComputerTracker.Models;

namespace ClassroomComputerTracker.Data
{
    public class DatabaseManager
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public DatabaseManager(string databasePath)
        {
            _databasePath = databasePath;
            _connectionString = $"Data Source={_databasePath};Version=3;";
            InitializeDatabase();
        }

        // ─────────────────────────────────────────────────────────
        // Schema init + migration
        // ─────────────────────────────────────────────────────────
        private void InitializeDatabase()
        {
            if (!File.Exists(_databasePath))
                SQLiteConnection.CreateFile(_databasePath);

            using var con = new SQLiteConnection(_connectionString);
            con.Open();

            // Computers table (CREATE IF NOT EXISTS is safe on fresh DBs)
            Exec(con, @"
                CREATE TABLE IF NOT EXISTS Computers (
                    ComputerId       INTEGER PRIMARY KEY AUTOINCREMENT,
                    ComputerName     TEXT    NOT NULL,
                    Model            TEXT    DEFAULT '',
                    SerialNumber     TEXT    DEFAULT '',
                    LastUpdated      DATETIME,
                    ConnectedItems   TEXT    DEFAULT '',
                    Notes            TEXT    DEFAULT '',
                    RoomPositionX    INTEGER DEFAULT 10,
                    RoomPositionY    INTEGER DEFAULT 10,
                    Status           TEXT    DEFAULT 'Active',
                    Location         TEXT    DEFAULT 'Classroom',
                    BrokenReason     TEXT    DEFAULT '',
                    HotSwapAvailable INTEGER DEFAULT 0,
                    LastModifiedBy   TEXT    DEFAULT ''
                )");

            // Migrate: add new columns to existing databases (silently ignored if already present)
            MigrateColumn(con, "Computers", "Location",         "TEXT    DEFAULT 'Classroom'");
            MigrateColumn(con, "Computers", "BrokenReason",     "TEXT    DEFAULT ''");
            MigrateColumn(con, "Computers", "HotSwapAvailable", "INTEGER DEFAULT 0");
            MigrateColumn(con, "Computers", "LastModifiedBy",   "TEXT    DEFAULT ''");

            Exec(con, @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId       INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username     TEXT    NOT NULL UNIQUE,
                    PasswordHash TEXT    NOT NULL,
                    CreatedAt    DATETIME
                )");

            Exec(con, @"
                CREATE TABLE IF NOT EXISTS ServiceCalls (
                    ServiceCallId  INTEGER PRIMARY KEY AUTOINCREMENT,
                    ComputerId     INTEGER NOT NULL,
                    Description    TEXT    DEFAULT '',
                    Status         TEXT    DEFAULT 'Open',
                    SolutionNotes  TEXT    DEFAULT '',
                    OpenedAt       DATETIME,
                    ClosedAt       DATETIME,
                    OpenedByUser   TEXT    DEFAULT '',
                    ResolvedByUser TEXT    DEFAULT ''
                )");

            Exec(con, @"
                CREATE TABLE IF NOT EXISTS HotSwapItems (
                    HotSwapItemId  INTEGER PRIMARY KEY AUTOINCREMENT,
                    ItemName       TEXT    NOT NULL,
                    ItemType       TEXT    DEFAULT 'Monitor',
                    SerialNumber   TEXT    DEFAULT '',
                    Location       TEXT    DEFAULT '',
                    Condition      TEXT    DEFAULT 'Good',
                    Available      INTEGER DEFAULT 1,
                    Notes          TEXT    DEFAULT '',
                    LastUpdated    DATETIME,
                    LastModifiedBy TEXT    DEFAULT ''
                )");

            Exec(con, @"
                CREATE TABLE IF NOT EXISTS ChangeLog (
                    LogId      INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username   TEXT    DEFAULT '',
                    Action     TEXT    DEFAULT '',
                    EntityType TEXT    DEFAULT '',
                    EntityId   INTEGER DEFAULT 0,
                    EntityName TEXT    DEFAULT '',
                    Detail     TEXT    DEFAULT '',
                    Timestamp  DATETIME
                )");
        }

        private static void Exec(SQLiteConnection con, string sql)
        {
            using var cmd = new SQLiteCommand(sql, con);
            cmd.ExecuteNonQuery();
        }

        private static void MigrateColumn(SQLiteConnection con, string table, string column, string typeDef)
        {
            try
            {
                using var cmd = new SQLiteCommand(
                    $"ALTER TABLE {table} ADD COLUMN {column} {typeDef}", con);
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException)
            {
                // Column already exists — safe to ignore
            }
        }

        // ─────────────────────────────────────────────────────────
        // Password hashing
        // ─────────────────────────────────────────────────────────
        public static string HashPassword(string password) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();

        // ─────────────────────────────────────────────────────────
        // Users
        // ─────────────────────────────────────────────────────────
        public User? GetUserByUsername(string username)
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                "SELECT * FROM Users WHERE Username=@u COLLATE NOCASE", con);
            cmd.Parameters.AddWithValue("@u", username);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapUser(r) : null;
        }

        public bool CreateUser(string username, string password)
        {
            if (GetUserByUsername(username) != null) return false;
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                "INSERT INTO Users (Username,PasswordHash,CreatedAt) VALUES (@u,@ph,@ca)", con);
            cmd.Parameters.AddWithValue("@u",  username.Trim());
            cmd.Parameters.AddWithValue("@ph", HashPassword(password));
            cmd.Parameters.AddWithValue("@ca", DateTime.Now);
            cmd.ExecuteNonQuery();
            return true;
        }

        public User? ValidateUser(string username, string password)
        {
            var user = GetUserByUsername(username);
            if (user == null) return null;
            return user.PasswordHash == HashPassword(password) ? user : null;
        }

        private static User MapUser(SQLiteDataReader r) => new()
        {
            UserId       = Convert.ToInt32(r["UserId"]),
            Username     = r["Username"]?.ToString() ?? "",
            PasswordHash = r["PasswordHash"]?.ToString() ?? "",
            CreatedAt    = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now
        };

        // ─────────────────────────────────────────────────────────
        // Computers
        // ─────────────────────────────────────────────────────────
        public List<Computer> GetAllComputers()
        {
            var list = new List<Computer>();
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand("SELECT * FROM Computers ORDER BY ComputerName", con);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapComputer(r));
            return list;
        }

        public List<Computer> GetComputersByLocation(string location)
        {
            var list = new List<Computer>();
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                "SELECT * FROM Computers WHERE Location=@loc ORDER BY ComputerName", con);
            cmd.Parameters.AddWithValue("@loc", location);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapComputer(r));
            return list;
        }

        public void AddComputer(Computer c, string modifiedBy = "")
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(@"
                INSERT INTO Computers
                    (ComputerName,Model,SerialNumber,LastUpdated,ConnectedItems,Notes,
                     RoomPositionX,RoomPositionY,Status,Location,BrokenReason,HotSwapAvailable,LastModifiedBy)
                VALUES
                    (@name,@model,@serial,@updated,@items,@notes,
                     @x,@y,@status,@loc,@brokenReason,@hotSwap,@modifiedBy)", con);
            BindComputer(cmd, c, modifiedBy);
            cmd.ExecuteNonQuery();
            LogChange(modifiedBy, "Added Computer", "Computer",
                (int)con.LastInsertRowId, c.ComputerName,
                $"Location: {c.Location}  Status: {c.Status}");
        }

        public void UpdateComputer(Computer c, string modifiedBy = "")
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(@"
                UPDATE Computers SET
                    ComputerName=@name, Model=@model, SerialNumber=@serial,
                    LastUpdated=@updated, ConnectedItems=@items, Notes=@notes,
                    RoomPositionX=@x, RoomPositionY=@y, Status=@status,
                    Location=@loc, BrokenReason=@brokenReason,
                    HotSwapAvailable=@hotSwap, LastModifiedBy=@modifiedBy
                WHERE ComputerId=@id", con);
            cmd.Parameters.AddWithValue("@id", c.ComputerId);
            BindComputer(cmd, c, modifiedBy);
            cmd.ExecuteNonQuery();
            if (!string.IsNullOrEmpty(modifiedBy))
                LogChange(modifiedBy, "Updated Computer", "Computer", c.ComputerId, c.ComputerName,
                    $"Status: {c.Status}  Location: {c.Location}");
        }

        // Position-only update — not logged (drag events are too frequent)
        public void UpdateComputerPosition(Computer c)
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE Computers SET RoomPositionX=@x, RoomPositionY=@y WHERE ComputerId=@id", con);
            cmd.Parameters.AddWithValue("@x",  c.RoomPositionX);
            cmd.Parameters.AddWithValue("@y",  c.RoomPositionY);
            cmd.Parameters.AddWithValue("@id", c.ComputerId);
            cmd.ExecuteNonQuery();
        }

        public void DeleteComputer(int id, string modifiedBy = "")
        {
            string name;
            using (var con = new SQLiteConnection(_connectionString))
            {
                con.Open();
                using var nameCmd = new SQLiteCommand(
                    "SELECT ComputerName FROM Computers WHERE ComputerId=@id", con);
                nameCmd.Parameters.AddWithValue("@id", id);
                name = nameCmd.ExecuteScalar()?.ToString() ?? $"ID:{id}";
            }
            using (var con = new SQLiteConnection(_connectionString))
            {
                con.Open();
                using var cmd = new SQLiteCommand(
                    "DELETE FROM Computers WHERE ComputerId=@id", con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            LogChange(modifiedBy, "Deleted Computer", "Computer", id, name, "");
        }

        private static void BindComputer(SQLiteCommand cmd, Computer c, string modifiedBy)
        {
            cmd.Parameters.AddWithValue("@name",         c.ComputerName);
            cmd.Parameters.AddWithValue("@model",        c.Model);
            cmd.Parameters.AddWithValue("@serial",       c.SerialNumber);
            cmd.Parameters.AddWithValue("@updated",      DateTime.Now);
            cmd.Parameters.AddWithValue("@items",        c.ConnectedItems);
            cmd.Parameters.AddWithValue("@notes",        c.Notes);
            cmd.Parameters.AddWithValue("@x",            c.RoomPositionX);
            cmd.Parameters.AddWithValue("@y",            c.RoomPositionY);
            cmd.Parameters.AddWithValue("@status",       c.Status);
            cmd.Parameters.AddWithValue("@loc",          c.Location);
            cmd.Parameters.AddWithValue("@brokenReason", c.BrokenReason);
            cmd.Parameters.AddWithValue("@hotSwap",      c.HotSwapAvailable ? 1 : 0);
            cmd.Parameters.AddWithValue("@modifiedBy",   modifiedBy);
        }

        private static Computer MapComputer(SQLiteDataReader r) => new()
        {
            ComputerId       = Convert.ToInt32(r["ComputerId"]),
            ComputerName     = r["ComputerName"]?.ToString() ?? "",
            Model            = r["Model"]?.ToString() ?? "",
            SerialNumber     = r["SerialNumber"]?.ToString() ?? "",
            LastUpdated      = r["LastUpdated"] != DBNull.Value
                                   ? Convert.ToDateTime(r["LastUpdated"]) : DateTime.Now,
            ConnectedItems   = r["ConnectedItems"]?.ToString() ?? "",
            Notes            = r["Notes"]?.ToString() ?? "",
            RoomPositionX    = r["RoomPositionX"] != DBNull.Value
                                   ? Convert.ToInt32(r["RoomPositionX"]) : 10,
            RoomPositionY    = r["RoomPositionY"] != DBNull.Value
                                   ? Convert.ToInt32(r["RoomPositionY"]) : 10,
            Status           = r["Status"]?.ToString() ?? "Active",
            Location         = r["Location"]?.ToString() ?? "Classroom",
            BrokenReason     = r["BrokenReason"]?.ToString() ?? "",
            HotSwapAvailable = r["HotSwapAvailable"] != DBNull.Value
                                   && Convert.ToInt32(r["HotSwapAvailable"]) == 1,
            LastModifiedBy   = r["LastModifiedBy"]?.ToString() ?? ""
        };

        // ─────────────────────────────────────────────────────────
        // Service Calls
        // ─────────────────────────────────────────────────────────
        public List<ServiceCall> GetAllServiceCalls()
        {
            var list = new List<ServiceCall>();
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(@"
                SELECT sc.*, COALESCE(c.ComputerName,'Unknown') AS ComputerName
                FROM ServiceCalls sc
                LEFT JOIN Computers c ON sc.ComputerId = c.ComputerId
                ORDER BY sc.OpenedAt DESC", con);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapServiceCall(r));
            return list;
        }

        public void AddServiceCall(ServiceCall sc, string modifiedBy = "")
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(@"
                INSERT INTO ServiceCalls
                    (ComputerId,Description,Status,SolutionNotes,OpenedAt,ClosedAt,OpenedByUser,ResolvedByUser)
                VALUES
                    (@cid,@desc,@status,@sol,@opened,@closed,@openedBy,@resolvedBy)", con);
            BindServiceCall(cmd, sc);
            cmd.ExecuteNonQuery();
            LogChange(modifiedBy, "Opened Service Call", "ServiceCall",
                (int)con.LastInsertRowId, sc.ComputerName, sc.Description);
        }

        public void UpdateServiceCall(ServiceCall sc, string modifiedBy = "")
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(@"
                UPDATE ServiceCalls SET
                    ComputerId=@cid, Description=@desc, Status=@status,
                    SolutionNotes=@sol, OpenedAt=@opened, ClosedAt=@closed,
                    OpenedByUser=@openedBy, ResolvedByUser=@resolvedBy
                WHERE ServiceCallId=@id", con);
            cmd.Parameters.AddWithValue("@id", sc.ServiceCallId);
            BindServiceCall(cmd, sc);
            cmd.ExecuteNonQuery();
            LogChange(modifiedBy, $"Updated Service Call → {sc.Status}", "ServiceCall",
                sc.ServiceCallId, sc.ComputerName, sc.Description);
        }

        public void DeleteServiceCall(int id, string modifiedBy = "")
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                "DELETE FROM ServiceCalls WHERE ServiceCallId=@id", con);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            LogChange(modifiedBy, "Deleted Service Call", "ServiceCall", id, "", "");
        }

        private static void BindServiceCall(SQLiteCommand cmd, ServiceCall sc)
        {
            cmd.Parameters.AddWithValue("@cid",        sc.ComputerId);
            cmd.Parameters.AddWithValue("@desc",       sc.Description);
            cmd.Parameters.AddWithValue("@status",     sc.Status);
            cmd.Parameters.AddWithValue("@sol",        sc.SolutionNotes);
            cmd.Parameters.AddWithValue("@opened",     sc.OpenedAt);
            cmd.Parameters.AddWithValue("@closed",
                sc.ClosedAt.HasValue ? (object)sc.ClosedAt.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@openedBy",   sc.OpenedByUser);
            cmd.Parameters.AddWithValue("@resolvedBy", sc.ResolvedByUser);
        }

        private static ServiceCall MapServiceCall(SQLiteDataReader r) => new()
        {
            ServiceCallId  = Convert.ToInt32(r["ServiceCallId"]),
            ComputerId     = Convert.ToInt32(r["ComputerId"]),
            ComputerName   = r["ComputerName"]?.ToString() ?? "",
            Description    = r["Description"]?.ToString() ?? "",
            Status         = r["Status"]?.ToString() ?? "Open",
            SolutionNotes  = r["SolutionNotes"]?.ToString() ?? "",
            OpenedAt       = r["OpenedAt"] != DBNull.Value
                                 ? Convert.ToDateTime(r["OpenedAt"]) : DateTime.Now,
            ClosedAt       = r["ClosedAt"] != DBNull.Value
                                 ? Convert.ToDateTime(r["ClosedAt"]) : (DateTime?)null,
            OpenedByUser   = r["OpenedByUser"]?.ToString() ?? "",
            ResolvedByUser = r["ResolvedByUser"]?.ToString() ?? ""
        };

        // ─────────────────────────────────────────────────────────
        // Hot Swap Items
        // ─────────────────────────────────────────────────────────
        public List<HotSwapItem> GetAllHotSwapItems()
        {
            var list = new List<HotSwapItem>();
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                "SELECT * FROM HotSwapItems ORDER BY ItemType, ItemName", con);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapHotSwapItem(r));
            return list;
        }

        public void AddHotSwapItem(HotSwapItem item, string modifiedBy = "")
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(@"
                INSERT INTO HotSwapItems
                    (ItemName,ItemType,SerialNumber,Location,Condition,Available,Notes,LastUpdated,LastModifiedBy)
                VALUES
                    (@name,@type,@serial,@loc,@cond,@avail,@notes,@updated,@modifiedBy)", con);
            BindHotSwapItem(cmd, item, modifiedBy);
            cmd.ExecuteNonQuery();
            LogChange(modifiedBy, "Added Hot Swap Item", "HotSwapItem",
                (int)con.LastInsertRowId, item.ItemName, $"Type: {item.ItemType}");
        }

        public void UpdateHotSwapItem(HotSwapItem item, string modifiedBy = "")
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(@"
                UPDATE HotSwapItems SET
                    ItemName=@name, ItemType=@type, SerialNumber=@serial, Location=@loc,
                    Condition=@cond, Available=@avail, Notes=@notes,
                    LastUpdated=@updated, LastModifiedBy=@modifiedBy
                WHERE HotSwapItemId=@id", con);
            cmd.Parameters.AddWithValue("@id", item.HotSwapItemId);
            BindHotSwapItem(cmd, item, modifiedBy);
            cmd.ExecuteNonQuery();
            LogChange(modifiedBy, "Updated Hot Swap Item", "HotSwapItem",
                item.HotSwapItemId, item.ItemName, $"Available: {item.Available}");
        }

        public void DeleteHotSwapItem(int id, string modifiedBy = "")
        {
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                "DELETE FROM HotSwapItems WHERE HotSwapItemId=@id", con);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            LogChange(modifiedBy, "Deleted Hot Swap Item", "HotSwapItem", id, "", "");
        }

        private static void BindHotSwapItem(SQLiteCommand cmd, HotSwapItem item, string modifiedBy)
        {
            cmd.Parameters.AddWithValue("@name",       item.ItemName);
            cmd.Parameters.AddWithValue("@type",       item.ItemType);
            cmd.Parameters.AddWithValue("@serial",     item.SerialNumber);
            cmd.Parameters.AddWithValue("@loc",        item.Location);
            cmd.Parameters.AddWithValue("@cond",       item.Condition);
            cmd.Parameters.AddWithValue("@avail",      item.Available ? 1 : 0);
            cmd.Parameters.AddWithValue("@notes",      item.Notes);
            cmd.Parameters.AddWithValue("@updated",    DateTime.Now);
            cmd.Parameters.AddWithValue("@modifiedBy", modifiedBy);
        }

        private static HotSwapItem MapHotSwapItem(SQLiteDataReader r) => new()
        {
            HotSwapItemId  = Convert.ToInt32(r["HotSwapItemId"]),
            ItemName       = r["ItemName"]?.ToString() ?? "",
            ItemType       = r["ItemType"]?.ToString() ?? "Monitor",
            SerialNumber   = r["SerialNumber"]?.ToString() ?? "",
            Location       = r["Location"]?.ToString() ?? "",
            Condition      = r["Condition"]?.ToString() ?? "Good",
            Available      = r["Available"] != DBNull.Value
                                 && Convert.ToInt32(r["Available"]) == 1,
            Notes          = r["Notes"]?.ToString() ?? "",
            LastUpdated    = r["LastUpdated"] != DBNull.Value
                                 ? Convert.ToDateTime(r["LastUpdated"]) : DateTime.Now,
            LastModifiedBy = r["LastModifiedBy"]?.ToString() ?? ""
        };

        // ─────────────────────────────────────────────────────────
        // Change Log
        // ─────────────────────────────────────────────────────────
        public void LogChange(string username, string action, string entityType,
                              int entityId, string entityName, string detail)
        {
            if (string.IsNullOrEmpty(username)) return;
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(@"
                INSERT INTO ChangeLog (Username,Action,EntityType,EntityId,EntityName,Detail,Timestamp)
                VALUES (@u,@a,@et,@eid,@en,@d,@t)", con);
            cmd.Parameters.AddWithValue("@u",   username);
            cmd.Parameters.AddWithValue("@a",   action);
            cmd.Parameters.AddWithValue("@et",  entityType);
            cmd.Parameters.AddWithValue("@eid", entityId);
            cmd.Parameters.AddWithValue("@en",  entityName);
            cmd.Parameters.AddWithValue("@d",   detail);
            cmd.Parameters.AddWithValue("@t",   DateTime.Now);
            cmd.ExecuteNonQuery();
        }

        public List<ChangeLogEntry> GetChangeLog(int limit = 500)
        {
            var list = new List<ChangeLogEntry>();
            using var con = new SQLiteConnection(_connectionString);
            con.Open();
            using var cmd = new SQLiteCommand(
                $"SELECT * FROM ChangeLog ORDER BY Timestamp DESC LIMIT {limit}", con);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapChangeLog(r));
            return list;
        }

        private static ChangeLogEntry MapChangeLog(SQLiteDataReader r) => new()
        {
            LogId      = Convert.ToInt32(r["LogId"]),
            Username   = r["Username"]?.ToString() ?? "",
            Action     = r["Action"]?.ToString() ?? "",
            EntityType = r["EntityType"]?.ToString() ?? "",
            EntityId   = r["EntityId"] != DBNull.Value ? Convert.ToInt32(r["EntityId"]) : 0,
            EntityName = r["EntityName"]?.ToString() ?? "",
            Detail     = r["Detail"]?.ToString() ?? "",
            Timestamp  = r["Timestamp"] != DBNull.Value
                             ? Convert.ToDateTime(r["Timestamp"]) : DateTime.Now
        };
    }
}
