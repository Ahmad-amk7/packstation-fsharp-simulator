namespace PackstationFsharp.Repositories

open System
open System.Threading.Tasks
open MySqlConnector
open PackstationFsharp
open PackstationFsharp.Models

type EmailActivationRepository(connection: DbManageConnection, emailSender: IEmailSender) =

    interface IEmailActivationRepository with

        member _.RequestActivationAsync(email: string) =
            task {
                do! connection.OpenConnAsync()

                // Ensure table exists
                let ensureTable =
                    @"CREATE TABLE IF NOT EXISTS EmailActivation(
                        UserId NVARCHAR(100) NOT NULL,
                        UserEmail NVARCHAR(150) NOT NULL,
                        RequestDateTime DATETIME NOT NULL,
                        ActivationCode NVARCHAR(6) NOT NULL,
                        Certificate NVARCHAR(100) NOT NULL,
                        PRIMARY KEY(UserId)
                    ) ENGINE=InnoDB;"

                use ensureCmd = new MySqlCommand(ensureTable, connection.GetConnection())
                do! DbHelpers.ignoreTask (ensureCmd.ExecuteNonQueryAsync())
                use getUserCmd =
                    new MySqlCommand(
                        "SELECT UserId, Certificate FROM UserAccount WHERE UserEmail = @Email",
                        connection.GetConnection()
                    )

                getUserCmd.Parameters.AddWithValue("@Email", if isNull email then "" else email) |> ignore

                let! reader = getUserCmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()
                let userId = if hasRow then Some(reader.["UserId"].ToString()) else None
                let cert = if hasRow then DbHelpers.stringOr "" reader.["Certificate"] else ""
                reader.Dispose()

                match userId with
                | None ->
                    do! connection.CloseConnAsync()
                    return String.Empty
                | Some uid ->
                    let code = Random.Shared.Next(100000, 999999).ToString()

                    use delCmd =
                        new MySqlCommand("DELETE FROM EmailActivation WHERE UserId = @UserId", connection.GetConnection())

                    delCmd.Parameters.AddWithValue("@UserId", uid) |> ignore
                    do! DbHelpers.ignoreTask (delCmd.ExecuteNonQueryAsync())
                    use insCmd =
                        new MySqlCommand(
                            @"INSERT INTO EmailActivation(UserId, UserEmail, RequestDateTime, ActivationCode, Certificate)
                              VALUES(@UserId,@UserEmail,@RequestDateTime,@ActivationCode,@Certificate)",
                            connection.GetConnection()
                        )

                    insCmd.Parameters.AddWithValue("@UserId", uid) |> ignore
                    insCmd.Parameters.AddWithValue("@UserEmail", email) |> ignore
                    insCmd.Parameters.AddWithValue("@RequestDateTime", DateTime.Now) |> ignore
                    insCmd.Parameters.AddWithValue("@ActivationCode", code) |> ignore
                    insCmd.Parameters.AddWithValue("@Certificate", cert) |> ignore
                    do! DbHelpers.ignoreTask (insCmd.ExecuteNonQueryAsync())
                    do! connection.CloseConnAsync()

                    let body =
                        $"<h2>Packstation Aktivierung</h2><p>Ihr Aktivierungscode lautet: <strong>{code}</strong></p>"

                    do! emailSender.SendEmailAsync(email, "Packstation - Aktivierungscode", body)
                    return code
            }

        member _.ActivateAccountAsync(email: string, code: string) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    @"SELECT ea.UserId FROM EmailActivation ea JOIN UserAccount ua ON ea.UserId = ua.UserId
                      WHERE ua.UserEmail = @Email AND ea.ActivationCode = @Code"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@Email", if isNull email then "" else email) |> ignore
                cmd.Parameters.AddWithValue("@Code", if isNull code then "" else code) |> ignore

                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()
                let userId = if hasRow then Some(reader.["UserId"].ToString()) else None
                reader.Dispose()

                match userId with
                | None ->
                    do! connection.CloseConnAsync()
                    return false
                | Some uid ->
                    use updCmd =
                        new MySqlCommand("UPDATE UserAccount SET IsActivated = 1 WHERE UserId = @UserId", connection.GetConnection())

                    updCmd.Parameters.AddWithValue("@UserId", uid) |> ignore
                    do! DbHelpers.ignoreTask (updCmd.ExecuteNonQueryAsync())
                    use delCmd =
                        new MySqlCommand("DELETE FROM EmailActivation WHERE UserId = @UserId", connection.GetConnection())

                    delCmd.Parameters.AddWithValue("@UserId", uid) |> ignore
                    do! DbHelpers.ignoreTask (delCmd.ExecuteNonQueryAsync())
                    do! connection.CloseConnAsync()
                    return true
            }
