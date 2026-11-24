namespace PackstationFsharp.Repositories

open System
open System.Data.Common
open System.Threading.Tasks
open MySqlConnector
open PackstationFsharp
open PackstationFsharp.Models

type LockerRepository(connection: DbManageConnection) =

    let mapLocker (reader: DbDataReader) =
        Locker(
            LockerId = DbHelpers.stringOr "" reader.["LockerId"],
            LockerName = DbHelpers.stringOrNull reader.["LockerName"],
            Address = DbHelpers.stringOrNull reader.["Address"],
            IpAddress = DbHelpers.stringOrNull reader.["IpAddress"],
            Port = DbHelpers.intOr 5001 reader.["Port"],
            ApiKey = DbHelpers.stringOr "" reader.["ApiKey"],
            Status = enum<LockerStatus> (DbHelpers.intOr 0 reader.["Status"]),
            UseSimulation = DbHelpers.boolOr true reader.["UseSimulation"]
        )

    let mapCompartment (reader: DbDataReader) =
        Compartment(
            CompartmentId = DbHelpers.stringOr "" reader.["CompartmentId"],
            LockerId = DbHelpers.stringOr "" reader.["LockerId"],
            CompartmentNumber = DbHelpers.intOr 0 reader.["CompartmentNumber"],
            Status = enum<CompartmentStatus> (DbHelpers.intOr 0 reader.["Status"]),
            CurrentParcelId = DbHelpers.stringOrNull reader.["CurrentParcelId"]
        )

    interface ILockerRepository with

        member _.GetAllLockersAsync() =
            task {
                do! connection.OpenConnAsync()

                let ensureLocker =
                    @"CREATE TABLE IF NOT EXISTS Locker(LockerId NVARCHAR(100) NOT NULL, LockerName NVARCHAR(100), Address NVARCHAR(300), IpAddress NVARCHAR(50), Port INT DEFAULT 5001, ApiKey NVARCHAR(200), Status INT DEFAULT 0, UseSimulation BIT DEFAULT 1, CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY(LockerId)) ENGINE=InnoDB;"

                use c1 = new MySqlCommand(ensureLocker, connection.GetConnection())
                do! DbHelpers.ignoreTask (c1.ExecuteNonQueryAsync())
                let ensureComp =
                    @"CREATE TABLE IF NOT EXISTS Compartment(CompartmentId NVARCHAR(100) NOT NULL, LockerId NVARCHAR(100), CompartmentNumber INT, Status INT DEFAULT 0, CurrentParcelId NVARCHAR(100), OccupiedSince DATETIME NULL, PRIMARY KEY(CompartmentId)) ENGINE=InnoDB;"

                use c2 = new MySqlCommand(ensureComp, connection.GetConnection())
                do! DbHelpers.ignoreTask (c2.ExecuteNonQueryAsync())
                use cmd = new MySqlCommand("SELECT * FROM Locker", connection.GetConnection())
                let! reader = cmd.ExecuteReaderAsync()
                let! lockers = DbHelpers.readAll reader mapLocker
                reader.Dispose()
                do! connection.CloseConnAsync()
                return lockers
            }

        member _.GetLockerByIdAsync(lockerId: string) =
            task {
                do! connection.OpenConnAsync()
                use cmd = new MySqlCommand("SELECT * FROM Locker WHERE LockerId = @LockerId", connection.GetConnection())
                cmd.Parameters.AddWithValue("@LockerId", lockerId) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()
                let locker = if hasRow then Some(mapLocker reader) else None
                reader.Dispose()
                do! connection.CloseConnAsync()
                return locker
            }

        member _.AddLockerAsync(locker: Locker) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    @"INSERT INTO Locker(LockerId, LockerName, Address, IpAddress, Port, ApiKey, Status, UseSimulation)
                      VALUES(@LockerId,@LockerName,@Address,@IpAddress,@Port,@ApiKey,@Status,@UseSimulation)"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@LockerId", locker.LockerId) |> ignore
                cmd.Parameters.AddWithValue("@LockerName", if isNull locker.LockerName then "" else locker.LockerName) |> ignore
                cmd.Parameters.AddWithValue("@Address", if isNull locker.Address then "" else locker.Address) |> ignore
                cmd.Parameters.AddWithValue("@IpAddress", if isNull locker.IpAddress then "" else locker.IpAddress) |> ignore
                cmd.Parameters.AddWithValue("@Port", locker.Port) |> ignore
                cmd.Parameters.AddWithValue("@ApiKey", locker.ApiKey) |> ignore
                cmd.Parameters.AddWithValue("@Status", int locker.Status) |> ignore
                cmd.Parameters.AddWithValue("@UseSimulation", locker.UseSimulation) |> ignore

                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.GetCompartmentsByLockerAsync(lockerId: string) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand(
                        "SELECT * FROM Compartment WHERE LockerId = @LockerId ORDER BY CompartmentNumber",
                        connection.GetConnection()
                    )

                cmd.Parameters.AddWithValue("@LockerId", lockerId) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! compartments = DbHelpers.readAll reader mapCompartment
                reader.Dispose()
                do! connection.CloseConnAsync()
                return compartments
            }

        member _.GetCompartmentByIdAsync(compartmentId: string) =
            task {
                do! connection.OpenConnAsync()
                use cmd =
                    new MySqlCommand(
                        "SELECT * FROM Compartment WHERE CompartmentId = @CompartmentId",
                        connection.GetConnection()
                    )

                cmd.Parameters.AddWithValue("@CompartmentId", compartmentId) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()

                let compartment = if hasRow then Some(mapCompartment reader) else None
                reader.Dispose()
                do! connection.CloseConnAsync()
                return compartment
            }

        member _.GetFreeCompartmentAsync(lockerId: string) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand(
                        "SELECT * FROM Compartment WHERE LockerId = @LockerId AND Status = 0 ORDER BY CompartmentNumber LIMIT 1",
                        connection.GetConnection()
                    )

                cmd.Parameters.AddWithValue("@LockerId", lockerId) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()

                let compartment =
                    if hasRow then
                        Some(
                            Compartment(
                                CompartmentId = DbHelpers.stringOr "" reader.["CompartmentId"],
                                LockerId = DbHelpers.stringOr "" reader.["LockerId"],
                                CompartmentNumber = DbHelpers.intOr 0 reader.["CompartmentNumber"],
                                Status = CompartmentStatus.Free
                            )
                        )
                    else
                        None

                reader.Dispose()
                do! connection.CloseConnAsync()
                return compartment
            }

        member _.UpdateCompartmentStatusAsync(compartmentId: string, status: CompartmentStatus, parcelId: string) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    "UPDATE Compartment SET Status=@Status, CurrentParcelId=@CurrentParcelId, OccupiedSince=@OccupiedSince WHERE CompartmentId=@CompartmentId"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@Status", int status) |> ignore
                cmd.Parameters.AddWithValue("@CurrentParcelId", DbHelpers.dbValue parcelId) |> ignore

                let occupiedSince =
                    if status = CompartmentStatus.Occupied then
                        box DateTime.Now
                    else
                        box DBNull.Value

                cmd.Parameters.AddWithValue("@OccupiedSince", occupiedSince) |> ignore
                cmd.Parameters.AddWithValue("@CompartmentId", compartmentId) |> ignore

                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.AddCompartmentAsync(compartment: Compartment) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    @"INSERT INTO Compartment(CompartmentId, LockerId, CompartmentNumber, Status)
                      VALUES(@CompartmentId,@LockerId,@CompartmentNumber,@Status)"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@CompartmentId", compartment.CompartmentId) |> ignore
                cmd.Parameters.AddWithValue("@LockerId", compartment.LockerId) |> ignore
                cmd.Parameters.AddWithValue("@CompartmentNumber", compartment.CompartmentNumber) |> ignore
                cmd.Parameters.AddWithValue("@Status", int compartment.Status) |> ignore

                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }
