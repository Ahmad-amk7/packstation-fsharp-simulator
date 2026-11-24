namespace PackstationFsharp.Repositories

open System
open System.Data.Common
open System.Threading.Tasks
open MySqlConnector
open PackstationFsharp
open PackstationFsharp.Models

type ParcelRepository(connection: DbManageConnection) =

    let mapParcel (reader: DbDataReader) =
        Parcel(
            ParcelId = DbHelpers.stringOr "" reader.["ParcelId"],
            TrackingNumber = DbHelpers.stringOr "" reader.["TrackingNumber"],
            SenderUserId = DbHelpers.stringOr "" reader.["SenderUserId"],
            RecipientFullName = DbHelpers.stringOrNull reader.["RecipientFullName"],
            RecipientEmail = DbHelpers.stringOrNull reader.["RecipientEmail"],
            RecipientPhone = DbHelpers.stringOrNull reader.["RecipientPhone"],
            RecipientStreet = DbHelpers.stringOrNull reader.["RecipientStreet"],
            RecipientHouseNo = DbHelpers.stringOrNull reader.["RecipientHouseNo"],
            RecipientCity = DbHelpers.stringOrNull reader.["RecipientCity"],
            RecipientPostalCode = DbHelpers.stringOrNull reader.["RecipientPostalCode"],
            RecipientCountry = DbHelpers.stringOrNull reader.["RecipientCountry"],
            ParcelDescription = DbHelpers.stringOrNull reader.["ParcelDescription"],
            ParcelImage = DbHelpers.stringOrNull reader.["ParcelImage"],
            Status = enum<ParcelStatus> (DbHelpers.intOr 0 reader.["Status"]),
            LockerId = DbHelpers.stringOrNull reader.["LockerId"],
            CompartmentId = DbHelpers.stringOrNull reader.["CompartmentId"],
            DepositedByEmployeeId = DbHelpers.stringOrNull reader.["DepositedByEmployeeId"],
            CreatedAt = DbHelpers.dateTimeOr DateTime.Now reader.["CreatedAt"],
            DepositedAt = DbHelpers.nullableDate reader.["DepositedAt"],
            AwaitingPickupAt = DbHelpers.nullableDate reader.["AwaitingPickupAt"],
            PickedUpAt = DbHelpers.nullableDate reader.["PickedUpAt"],
            AutoClosedAt = DbHelpers.nullableDate reader.["AutoClosedAt"]
        )

    let mapPickupCode (reader: DbDataReader) =
        PickupCode(
            CodeId = DbHelpers.stringOr "" reader.["CodeId"],
            ParcelId = DbHelpers.stringOr "" reader.["ParcelId"],
            CompartmentId = DbHelpers.stringOr "" reader.["CompartmentId"],
            Code = DbHelpers.stringOr "" reader.["Code"],
            CreatedAt = DbHelpers.dateTimeOr DateTime.Now reader.["CreatedAt"],
            ExpiresAt = DbHelpers.dateTimeOr (DateTime.Now.AddHours(1.0)) reader.["ExpiresAt"],
            IsUsed = DbHelpers.boolOr false reader.["IsUsed"]
        )

    let ensureParcelTableSql =
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

    let ensurePickupCodeTableSql =
        @"CREATE TABLE IF NOT EXISTS PickupCode(CodeId NVARCHAR(100) NOT NULL, ParcelId NVARCHAR(100), CompartmentId NVARCHAR(100), Code NVARCHAR(10), CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP, ExpiresAt DATETIME, IsUsed BIT DEFAULT 0, UsedAt DATETIME NULL, PRIMARY KEY(CodeId)) ENGINE=InnoDB;"

    let ensurePickupLogTableSql =
        @"CREATE TABLE IF NOT EXISTS PickupLog(LogId NVARCHAR(100) NOT NULL, ParcelId NVARCHAR(100), CompartmentId NVARCHAR(100), UserId NVARCHAR(100), Action NVARCHAR(50), ActionAt DATETIME DEFAULT CURRENT_TIMESTAMP, IpAddress NVARCHAR(50), Details NVARCHAR(500), PRIMARY KEY(LogId)) ENGINE=InnoDB;"

    let ensureTablesAsync () : Task<unit> =
        task {
            use c1 = new MySqlCommand(ensureParcelTableSql, connection.GetConnection())
            do! DbHelpers.ignoreTask (c1.ExecuteNonQueryAsync())
            use c2 = new MySqlCommand(ensurePickupCodeTableSql, connection.GetConnection())
            do! DbHelpers.ignoreTask (c2.ExecuteNonQueryAsync())
            use c3 = new MySqlCommand(ensurePickupLogTableSql, connection.GetConnection())
            do! DbHelpers.ignoreTask (c3.ExecuteNonQueryAsync())
        }

    interface IParcelRepository with

        member _.AddParcelAsync(parcel: Parcel) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    @"INSERT INTO Parcel(ParcelId, TrackingNumber, SenderUserId, RecipientFullName, RecipientEmail, RecipientPhone,
                      RecipientStreet, RecipientHouseNo, RecipientCity, RecipientPostalCode, RecipientCountry,
                      ParcelDescription, ParcelImage, Status, CreatedAt)
                      VALUES(@ParcelId,@TrackingNumber,@SenderUserId,@RecipientFullName,@RecipientEmail,@RecipientPhone,
                      @RecipientStreet,@RecipientHouseNo,@RecipientCity,@RecipientPostalCode,@RecipientCountry,
                      @ParcelDescription,@ParcelImage,@Status,@CreatedAt)"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@ParcelId", parcel.ParcelId) |> ignore
                cmd.Parameters.AddWithValue("@TrackingNumber", parcel.TrackingNumber) |> ignore
                cmd.Parameters.AddWithValue("@SenderUserId", if isNull parcel.SenderUserId then "" else parcel.SenderUserId) |> ignore
                cmd.Parameters.AddWithValue("@RecipientFullName", if isNull parcel.RecipientFullName then "" else parcel.RecipientFullName) |> ignore
                cmd.Parameters.AddWithValue("@RecipientEmail", if isNull parcel.RecipientEmail then "" else parcel.RecipientEmail) |> ignore
                cmd.Parameters.AddWithValue("@RecipientPhone", DbHelpers.dbValue parcel.RecipientPhone) |> ignore
                cmd.Parameters.AddWithValue("@RecipientStreet", if isNull parcel.RecipientStreet then "" else parcel.RecipientStreet) |> ignore
                cmd.Parameters.AddWithValue("@RecipientHouseNo", if isNull parcel.RecipientHouseNo then "" else parcel.RecipientHouseNo) |> ignore
                cmd.Parameters.AddWithValue("@RecipientCity", if isNull parcel.RecipientCity then "" else parcel.RecipientCity) |> ignore
                cmd.Parameters.AddWithValue("@RecipientPostalCode", if isNull parcel.RecipientPostalCode then "" else parcel.RecipientPostalCode) |> ignore
                cmd.Parameters.AddWithValue("@RecipientCountry", if isNull parcel.RecipientCountry then "" else parcel.RecipientCountry) |> ignore
                cmd.Parameters.AddWithValue("@ParcelDescription", DbHelpers.dbValue parcel.ParcelDescription) |> ignore
                cmd.Parameters.AddWithValue("@ParcelImage", DbHelpers.dbValue parcel.ParcelImage) |> ignore
                cmd.Parameters.AddWithValue("@Status", int parcel.Status) |> ignore
                cmd.Parameters.AddWithValue("@CreatedAt", parcel.CreatedAt) |> ignore

                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.GetParcelByTrackingAsync(trackingNumber: string) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand("SELECT * FROM Parcel WHERE TrackingNumber = @TrackingNumber", connection.GetConnection())

                cmd.Parameters.AddWithValue("@TrackingNumber", if isNull trackingNumber then "" else trackingNumber) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()
                let parcel = if hasRow then Some(mapParcel reader) else None
                reader.Dispose()
                do! connection.CloseConnAsync()
                return parcel
            }

        member _.GetParcelByIdAsync(parcelId: string) =
            task {
                do! connection.OpenConnAsync()
                use cmd = new MySqlCommand("SELECT * FROM Parcel WHERE ParcelId = @ParcelId", connection.GetConnection())
                cmd.Parameters.AddWithValue("@ParcelId", parcelId) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()
                let parcel = if hasRow then Some(mapParcel reader) else None
                reader.Dispose()
                do! connection.CloseConnAsync()
                return parcel
            }

        member _.GetParcelsBySenderAsync(senderUserId: string) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand(
                        "SELECT * FROM Parcel WHERE SenderUserId = @SenderUserId ORDER BY CreatedAt DESC",
                        connection.GetConnection()
                    )

                cmd.Parameters.AddWithValue("@SenderUserId", senderUserId) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! parcels = DbHelpers.readAll reader mapParcel
                reader.Dispose()
                do! connection.CloseConnAsync()
                return parcels
            }

        member _.GetAllParcelsAsync() =
            task {
                do! connection.OpenConnAsync()
                // Ensure tables exist
                do! ensureTablesAsync ()

                use cmd = new MySqlCommand("SELECT * FROM Parcel ORDER BY CreatedAt DESC", connection.GetConnection())
                let! reader = cmd.ExecuteReaderAsync()
                let! parcels = DbHelpers.readAll reader mapParcel
                reader.Dispose()
                do! connection.CloseConnAsync()
                return parcels
            }

        member _.GetParcelsByStatusAsync(status: ParcelStatus) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand(
                        "SELECT * FROM Parcel WHERE Status = @Status ORDER BY CreatedAt DESC",
                        connection.GetConnection()
                    )

                cmd.Parameters.AddWithValue("@Status", int status) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! parcels = DbHelpers.readAll reader mapParcel
                reader.Dispose()
                do! connection.CloseConnAsync()
                return parcels
            }

        member _.UpdateParcelAsync(parcel: Parcel) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    @"UPDATE Parcel SET Status=@Status, LockerId=@LockerId, CompartmentId=@CompartmentId,
                      DepositedByEmployeeId=@DepositedByEmployeeId, DepositedAt=@DepositedAt,
                      AwaitingPickupAt=@AwaitingPickupAt, PickedUpAt=@PickedUpAt, AutoClosedAt=@AutoClosedAt
                      WHERE ParcelId=@ParcelId"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@Status", int parcel.Status) |> ignore
                cmd.Parameters.AddWithValue("@LockerId", DbHelpers.dbValue parcel.LockerId) |> ignore
                cmd.Parameters.AddWithValue("@CompartmentId", DbHelpers.dbValue parcel.CompartmentId) |> ignore
                cmd.Parameters.AddWithValue("@DepositedByEmployeeId", DbHelpers.dbValue parcel.DepositedByEmployeeId) |> ignore
                cmd.Parameters.AddWithValue("@DepositedAt", DbHelpers.dbDate parcel.DepositedAt) |> ignore
                cmd.Parameters.AddWithValue("@AwaitingPickupAt", DbHelpers.dbDate parcel.AwaitingPickupAt) |> ignore
                cmd.Parameters.AddWithValue("@PickedUpAt", DbHelpers.dbDate parcel.PickedUpAt) |> ignore
                cmd.Parameters.AddWithValue("@AutoClosedAt", DbHelpers.dbDate parcel.AutoClosedAt) |> ignore
                cmd.Parameters.AddWithValue("@ParcelId", parcel.ParcelId) |> ignore

                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.GetValidCodeAsync(code: string) =
            task {
                do! connection.OpenConnAsync()

                let ensureCode = ensurePickupCodeTableSql
                use c2 = new MySqlCommand(ensureCode, connection.GetConnection())
                do! DbHelpers.ignoreTask (c2.ExecuteNonQueryAsync())
                let query = "SELECT * FROM PickupCode WHERE Code = @Code AND IsUsed = 0 AND ExpiresAt > NOW()"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@Code", if isNull code then "" else code) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()
                let pickupCode = if hasRow then Some(mapPickupCode reader) else None
                reader.Dispose()
                do! connection.CloseConnAsync()
                return pickupCode
            }

        member _.GetLatestCodeForParcelAsync(parcelId: string) =
            task {
                do! connection.OpenConnAsync()

                let ensureCode = ensurePickupCodeTableSql
                use c2 = new MySqlCommand(ensureCode, connection.GetConnection())
                do! DbHelpers.ignoreTask (c2.ExecuteNonQueryAsync())
                let query = "SELECT * FROM PickupCode WHERE ParcelId = @ParcelId ORDER BY CreatedAt DESC LIMIT 1"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@ParcelId", parcelId) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()
                let pickupCode = if hasRow then Some(mapPickupCode reader) else None
                reader.Dispose()
                do! connection.CloseConnAsync()
                return pickupCode
            }

        member _.AddPickupCodeAsync(pickupCode: PickupCode) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    @"INSERT INTO PickupCode(CodeId, ParcelId, CompartmentId, Code, CreatedAt, ExpiresAt, IsUsed)
                      VALUES(@CodeId,@ParcelId,@CompartmentId,@Code,@CreatedAt,@ExpiresAt,@IsUsed)"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@CodeId", pickupCode.CodeId) |> ignore
                cmd.Parameters.AddWithValue("@ParcelId", pickupCode.ParcelId) |> ignore
                cmd.Parameters.AddWithValue("@CompartmentId", pickupCode.CompartmentId) |> ignore
                cmd.Parameters.AddWithValue("@Code", pickupCode.Code) |> ignore
                cmd.Parameters.AddWithValue("@CreatedAt", pickupCode.CreatedAt) |> ignore
                cmd.Parameters.AddWithValue("@ExpiresAt", pickupCode.ExpiresAt) |> ignore
                cmd.Parameters.AddWithValue("@IsUsed", pickupCode.IsUsed) |> ignore

                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.MarkCodeUsedAsync(codeId: string) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand("UPDATE PickupCode SET IsUsed=1, UsedAt=NOW() WHERE CodeId=@CodeId", connection.GetConnection())

                cmd.Parameters.AddWithValue("@CodeId", codeId) |> ignore
                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.AddPickupLogAsync(log: PickupLog) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    @"INSERT INTO PickupLog(LogId, ParcelId, CompartmentId, UserId, Action, ActionAt, IpAddress, Details)
                      VALUES(@LogId,@ParcelId,@CompartmentId,@UserId,@Action,@ActionAt,@IpAddress,@Details)"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@LogId", log.LogId) |> ignore
                cmd.Parameters.AddWithValue("@ParcelId", log.ParcelId) |> ignore
                cmd.Parameters.AddWithValue("@CompartmentId", log.CompartmentId) |> ignore
                cmd.Parameters.AddWithValue("@UserId", DbHelpers.dbValue log.UserId) |> ignore
                cmd.Parameters.AddWithValue("@Action", log.Action) |> ignore
                cmd.Parameters.AddWithValue("@ActionAt", log.ActionAt) |> ignore
                cmd.Parameters.AddWithValue("@IpAddress", DbHelpers.dbValue log.IpAddress) |> ignore
                cmd.Parameters.AddWithValue("@Details", DbHelpers.dbValue log.Details) |> ignore

                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.GetLogsByParcelAsync(parcelId: string) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand(
                        "SELECT * FROM PickupLog WHERE ParcelId = @ParcelId ORDER BY ActionAt DESC",
                        connection.GetConnection()
                    )

                cmd.Parameters.AddWithValue("@ParcelId", parcelId) |> ignore
                let! reader = cmd.ExecuteReaderAsync()

                let! logs =
                    DbHelpers.readAll
                        reader
                        (fun r ->
                            PickupLog(
                                LogId = DbHelpers.stringOr "" r.["LogId"],
                                ParcelId = DbHelpers.stringOr "" r.["ParcelId"],
                                CompartmentId = DbHelpers.stringOr "" r.["CompartmentId"],
                                UserId = DbHelpers.stringOrNull r.["UserId"],
                                Action = DbHelpers.stringOr "" r.["Action"],
                                ActionAt = DbHelpers.dateTimeOr DateTime.Now r.["ActionAt"],
                                IpAddress = DbHelpers.stringOrNull r.["IpAddress"],
                                Details = DbHelpers.stringOrNull r.["Details"]
                            ))

                reader.Dispose()
                do! connection.CloseConnAsync()
                return logs
            }
