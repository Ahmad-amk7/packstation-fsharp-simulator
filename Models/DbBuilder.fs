namespace PackstationFsharp.Models

open System
open System.Threading.Tasks
open MySqlConnector
open PackstationFsharp

type DbBuilder(connection: DbManageConnection) =

    let executeAsync (query: string) : Task<unit> =
        task {
            do! connection.OpenConnAsync()
            use command = new MySqlCommand(query, connection.GetConnection())
            do! DbHelpers.ignoreTask (command.ExecuteNonQueryAsync())
            do! connection.CloseConnAsync()
        }

    /// Executes a statement on the already-open connection (used by GetCreateTableAsync).
    let executeOnOpenConnectionAsync (query: string) : Task<unit> =
        task {
            do! connection.OpenConnAsync()
            use command = new MySqlCommand(query, connection.GetConnection())
            do! DbHelpers.ignoreTask (command.ExecuteNonQueryAsync())
        }

    /// Checks a column on the already-open connection, without closing it.
    let columnExistsAsync (table: string) (column: string) : Task<bool> =
        task {
            do! connection.OpenConnAsync()
            let query =
                @"SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column"
            use command = new MySqlCommand(query, connection.GetConnection())
            command.Parameters.AddWithValue("@table", table) |> ignore
            command.Parameters.AddWithValue("@column", column) |> ignore
            let! result = command.ExecuteScalarAsync()
            let count = DbHelpers.intOr 0 result
            return count > 0
        }

    member _.GetCreateDatabaseAsync() : Task<unit> =
        executeAsync
            $"CREATE DATABASE IF NOT EXISTS `{connection.DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci"

    member _.GetCreateTableAsync() : Task<unit> =
        task {
            do! connection.OpenConnAsync()
            connection.GetConnection().ChangeDatabase(connection.DatabaseName)

            // UserAccount table
            let createUserTable =
                @"CREATE TABLE IF NOT EXISTS UserAccount(
                    UserId NVARCHAR(100) NOT NULL,
                    UserName NVARCHAR(50),
                    UserEmail NVARCHAR(150),
                    UserPassword NVARCHAR(90),
                    Certificate NVARCHAR(100) NOT NULL DEFAULT '',
                    IsActivated BIT NOT NULL DEFAULT 0,
                    IsRestricted BIT NOT NULL DEFAULT 0,
                    Role INT NOT NULL DEFAULT 0,
                    PhoneNumber NVARCHAR(30),
                    ProfileImage NVARCHAR(700),
                    AddressStreet NVARCHAR(150),
                    AddressHouseNo NVARCHAR(50),
                    AddressCity NVARCHAR(100),
                    AddressPostalCode NVARCHAR(50),
                    AddressCountry NVARCHAR(100),
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY(UserId)
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync createUserTable

            // Add columns if not exist
            let! roleExists = columnExistsAsync "UserAccount" "Role"
            if not roleExists then
                do! executeOnOpenConnectionAsync "ALTER TABLE UserAccount ADD COLUMN Role INT NOT NULL DEFAULT 0"

            let! phoneExists = columnExistsAsync "UserAccount" "PhoneNumber"
            if not phoneExists then
                do! executeOnOpenConnectionAsync "ALTER TABLE UserAccount ADD COLUMN PhoneNumber NVARCHAR(30)"

            let! imageExists = columnExistsAsync "UserAccount" "ProfileImage"
            if not imageExists then
                do! executeOnOpenConnectionAsync "ALTER TABLE UserAccount ADD COLUMN ProfileImage NVARCHAR(700)"

            // SubscriptionPlan
            let createPlanTable =
                @"CREATE TABLE IF NOT EXISTS SubscriptionPlan(
                    PlanId NVARCHAR(100) NOT NULL,
                    PlanName NVARCHAR(100),
                    Price DECIMAL(10,2),
                    ParcelLimit INT,
                    DurationDays INT,
                    Description NVARCHAR(500),
                    IsActive BIT DEFAULT 1,
                    PRIMARY KEY(PlanId)
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync createPlanTable

            // UserSubscription
            let createSubTable =
                @"CREATE TABLE IF NOT EXISTS UserSubscription(
                    SubscriptionId NVARCHAR(100) NOT NULL,
                    UserId NVARCHAR(100),
                    PlanId NVARCHAR(100),
                    StartDate DATETIME,
                    EndDate DATETIME,
                    Status INT DEFAULT 0,
                    ParcelsUsed INT DEFAULT 0,
                    PRIMARY KEY(SubscriptionId),
                    FOREIGN KEY(UserId) REFERENCES UserAccount(UserId) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync createSubTable

            // Locker
            let createLockerTable =
                @"CREATE TABLE IF NOT EXISTS Locker(
                    LockerId NVARCHAR(100) NOT NULL,
                    LockerName NVARCHAR(100),
                    Address NVARCHAR(300),
                    IpAddress NVARCHAR(50),
                    Port INT DEFAULT 5001,
                    ApiKey NVARCHAR(200),
                    Status INT DEFAULT 0,
                    UseSimulation BIT DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY(LockerId)
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync createLockerTable

            // Compartment
            let createCompTable =
                @"CREATE TABLE IF NOT EXISTS Compartment(
                    CompartmentId NVARCHAR(100) NOT NULL,
                    LockerId NVARCHAR(100),
                    CompartmentNumber INT,
                    Status INT DEFAULT 0,
                    CurrentParcelId NVARCHAR(100),
                    OccupiedSince DATETIME NULL,
                    PRIMARY KEY(CompartmentId),
                    FOREIGN KEY(LockerId) REFERENCES Locker(LockerId) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync createCompTable

            // Parcel
            let createParcelTable =
                @"CREATE TABLE IF NOT EXISTS Parcel(
                    ParcelId NVARCHAR(100) NOT NULL,
                    TrackingNumber NVARCHAR(50),
                    SenderUserId NVARCHAR(100),
                    RecipientFullName NVARCHAR(150),
                    RecipientEmail NVARCHAR(150),
                    RecipientPhone NVARCHAR(30),
                    RecipientStreet NVARCHAR(150),
                    RecipientHouseNo NVARCHAR(50),
                    RecipientCity NVARCHAR(100),
                    RecipientPostalCode NVARCHAR(50),
                    RecipientCountry NVARCHAR(100),
                    ParcelDescription NVARCHAR(500),
                    ParcelImage NVARCHAR(700),
                    Status INT DEFAULT 0,
                    LockerId NVARCHAR(100),
                    CompartmentId NVARCHAR(100),
                    DepositedByEmployeeId NVARCHAR(100),
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    DepositedAt DATETIME NULL,
                    AwaitingPickupAt DATETIME NULL,
                    PickedUpAt DATETIME NULL,
                    AutoClosedAt DATETIME NULL,
                    Notes NVARCHAR(500),
                    PRIMARY KEY(ParcelId)
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync createParcelTable

            // PickupCode
            let createCodeTable =
                @"CREATE TABLE IF NOT EXISTS PickupCode(
                    CodeId NVARCHAR(100) NOT NULL,
                    ParcelId NVARCHAR(100),
                    CompartmentId NVARCHAR(100),
                    Code NVARCHAR(10),
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    ExpiresAt DATETIME,
                    IsUsed BIT DEFAULT 0,
                    UsedAt DATETIME NULL,
                    PRIMARY KEY(CodeId)
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync createCodeTable

            // PickupLog
            let createLogTable =
                @"CREATE TABLE IF NOT EXISTS PickupLog(
                    LogId NVARCHAR(100) NOT NULL,
                    ParcelId NVARCHAR(100),
                    CompartmentId NVARCHAR(100),
                    UserId NVARCHAR(100),
                    Action NVARCHAR(50),
                    ActionAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IpAddress NVARCHAR(50),
                    Details NVARCHAR(500),
                    PRIMARY KEY(LogId)
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync createLogTable

            // EmailActivation
            let createEmailActTable =
                @"CREATE TABLE IF NOT EXISTS EmailActivation(
                    UserId NVARCHAR(100) NOT NULL,
                    UserEmail NVARCHAR(150) NOT NULL,
                    RequestDateTime DATETIME NOT NULL,
                    ActivationCode NVARCHAR(6) NOT NULL,
                    Certificate NVARCHAR(100) NOT NULL,
                    PRIMARY KEY(UserId),
                    FOREIGN KEY(UserId) REFERENCES UserAccount(UserId) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync createEmailActTable

            // Ensure table exists (force create if missing)
            let ensureEmailAct =
                @"CREATE TABLE IF NOT EXISTS emailactivation(
                    UserId NVARCHAR(100) NOT NULL,
                    UserEmail NVARCHAR(150) NOT NULL,
                    RequestDateTime DATETIME NOT NULL,
                    ActivationCode NVARCHAR(6) NOT NULL,
                    Certificate NVARCHAR(100) NOT NULL,
                    PRIMARY KEY(UserId)
                ) ENGINE=InnoDB;"

            do! executeOnOpenConnectionAsync ensureEmailAct

            do! connection.CloseConnAsync()
        }
