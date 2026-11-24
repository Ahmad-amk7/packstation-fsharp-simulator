namespace PackstationFsharp.Repositories

open System
open System.Data.Common
open System.Threading.Tasks
open MySqlConnector
open PackstationFsharp
open PackstationFsharp.Models

type UserRepository(connection: DbManageConnection) =

    let mapAccount (reader: DbDataReader) =
        UserAccount(
            UserId = DbHelpers.stringOr "" reader.["UserId"],
            UserName = DbHelpers.stringOrNull reader.["UserName"],
            UserEmail = DbHelpers.stringOrNull reader.["UserEmail"],
            UserPassword = DbHelpers.stringOrNull reader.["UserPassword"],
            Certificate = DbHelpers.stringOr "" reader.["Certificate"],
            IsActivated = DbHelpers.boolOr false reader.["IsActivated"],
            IsRestricted = DbHelpers.boolOr false reader.["IsRestricted"],
            Role = enum<UserRoleType> (DbHelpers.intOr 0 reader.["Role"]),
            PhoneNumber = DbHelpers.stringOrNull reader.["PhoneNumber"],
            ProfileImage = DbHelpers.stringOrNull reader.["ProfileImage"],
            AddressStreet = DbHelpers.stringOrNull reader.["AddressStreet"],
            AddressHouseNo = DbHelpers.stringOrNull reader.["AddressHouseNo"],
            AddressCity = DbHelpers.stringOrNull reader.["AddressCity"],
            AddressPostalCode = DbHelpers.stringOrNull reader.["AddressPostalCode"],
            AddressCountry = DbHelpers.stringOrNull reader.["AddressCountry"]
        )

    interface IUserRepository with

        member _.AddNewAccountAsync(account: UserAccount) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    @"INSERT INTO UserAccount(UserId, UserName, UserEmail, UserPassword, Certificate, IsActivated, IsRestricted, Role, ProfileImage)
                      VALUES(@UserId, @UserName, @UserEmail, @UserPassword, @Certificate, @IsActivated, @IsRestricted, @Role, @ProfileImage)"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@UserId", account.UserId) |> ignore
                cmd.Parameters.AddWithValue("@UserName", if isNull account.UserName then "" else account.UserName) |> ignore
                cmd.Parameters.AddWithValue("@UserEmail", if isNull account.UserEmail then "" else account.UserEmail) |> ignore
                cmd.Parameters.AddWithValue("@UserPassword", if isNull account.UserPassword then "" else account.UserPassword) |> ignore
                cmd.Parameters.AddWithValue("@Certificate", account.Certificate) |> ignore
                cmd.Parameters.AddWithValue("@IsActivated", account.IsActivated) |> ignore
                cmd.Parameters.AddWithValue("@IsRestricted", account.IsRestricted) |> ignore
                cmd.Parameters.AddWithValue("@Role", int account.Role) |> ignore
                cmd.Parameters.AddWithValue("@ProfileImage", DbHelpers.dbValue account.ProfileImage) |> ignore

                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.FindAccountByEmailAsync(email: string) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand("SELECT * FROM UserAccount WHERE UserEmail = @UserEmail", connection.GetConnection())

                cmd.Parameters.AddWithValue("@UserEmail", if isNull email then "" else email) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()
                let account = if hasRow then Some(mapAccount reader) else None
                reader.Dispose()
                do! connection.CloseConnAsync()
                return account
            }

        member _.FindAccountByUserId(userId: string) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand("SELECT * FROM UserAccount WHERE UserId = @UserId", connection.GetConnection())

                cmd.Parameters.AddWithValue("@UserId", if isNull userId then "" else userId) |> ignore
                let! reader = cmd.ExecuteReaderAsync()
                let! hasRow = reader.ReadAsync()
                let account = if hasRow then Some(mapAccount reader) else None
                reader.Dispose()
                do! connection.CloseConnAsync()
                return account
            }

        member _.UpdateAccountAsync(user: UserAccount, userId: string) =
            task {
                do! connection.OpenConnAsync()

                let query =
                    @"UPDATE UserAccount SET UserName=@UserName, UserEmail=@UserEmail, PhoneNumber=@PhoneNumber,
                      AddressStreet=@AddressStreet, AddressHouseNo=@AddressHouseNo, AddressCity=@AddressCity,
                      AddressPostalCode=@AddressPostalCode, AddressCountry=@AddressCountry WHERE UserId=@UserId"

                use cmd = new MySqlCommand(query, connection.GetConnection())
                cmd.Parameters.AddWithValue("@UserName", if isNull user.UserName then "" else user.UserName) |> ignore
                cmd.Parameters.AddWithValue("@UserEmail", if isNull user.UserEmail then "" else user.UserEmail) |> ignore
                cmd.Parameters.AddWithValue("@PhoneNumber", DbHelpers.dbValue user.PhoneNumber) |> ignore
                cmd.Parameters.AddWithValue("@AddressStreet", DbHelpers.dbValue user.AddressStreet) |> ignore
                cmd.Parameters.AddWithValue("@AddressHouseNo", DbHelpers.dbValue user.AddressHouseNo) |> ignore
                cmd.Parameters.AddWithValue("@AddressCity", DbHelpers.dbValue user.AddressCity) |> ignore
                cmd.Parameters.AddWithValue("@AddressPostalCode", DbHelpers.dbValue user.AddressPostalCode) |> ignore
                cmd.Parameters.AddWithValue("@AddressCountry", DbHelpers.dbValue user.AddressCountry) |> ignore
                cmd.Parameters.AddWithValue("@UserId", userId) |> ignore

                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.DeleteAccountAsync(userId: string) =
            task {
                do! connection.OpenConnAsync()
                use cmd = new MySqlCommand("DELETE FROM UserAccount WHERE UserId=@UserId", connection.GetConnection())
                cmd.Parameters.AddWithValue("@UserId", userId) |> ignore
                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }

        member _.GetAllUsersAsync() =
            task {
                do! connection.OpenConnAsync()
                use cmd = new MySqlCommand("SELECT * FROM UserAccount", connection.GetConnection())
                let! reader = cmd.ExecuteReaderAsync()
                let! users = DbHelpers.readAll reader mapAccount
                reader.Dispose()
                do! connection.CloseConnAsync()
                return users
            }

        member _.UpdateProfileImageAsync(userId: string, imagePath: string) =
            task {
                do! connection.OpenConnAsync()

                use cmd =
                    new MySqlCommand(
                        "UPDATE UserAccount SET ProfileImage=@ProfileImage WHERE UserId=@UserId",
                        connection.GetConnection()
                    )

                cmd.Parameters.AddWithValue("@ProfileImage", imagePath) |> ignore
                cmd.Parameters.AddWithValue("@UserId", userId) |> ignore
                do! DbHelpers.ignoreTask (cmd.ExecuteNonQueryAsync())
                do! connection.CloseConnAsync()
            }
