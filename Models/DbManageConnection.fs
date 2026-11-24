namespace PackstationFsharp.Models

open System
open System.Data
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open MySqlConnector

type DbManageConnection(connectionMode: ConnectionMode, configuration: IConfiguration) =
    let databaseName =
        let configuredName = configuration.["Db:Database"]

        if
            String.IsNullOrWhiteSpace(configuredName)
            || (configuredName
                |> Seq.exists (fun character -> not (Char.IsLetterOrDigit(character) || character = '_')))
        then
            invalidArg "configuration" "Db:Database must contain only letters, digits, or underscores."

        configuredName

    let connectionString =
        let configuredConnectionString = configuration.GetConnectionString("Admin")

        if String.IsNullOrWhiteSpace(configuredConnectionString) then
            invalidOp "Configure ConnectionStrings:Admin (for example with the ConnectionStrings__Admin environment variable)."

        let builder = MySqlConnectionStringBuilder(configuredConnectionString)
        builder.CharacterSet <- "utf8mb4"

        if connectionMode = ConnectionMode.GetRepository then
            builder.Database <- databaseName

        builder.ConnectionString

    let connection = new MySqlConnection(connectionString)

    member _.OpenConnAsync() : Task<unit> =
        task {
            if connection.State <> ConnectionState.Open then
                do! connection.OpenAsync()
        }

    member _.CloseConnAsync() : Task<unit> =
        task {
            if connection.State = ConnectionState.Open then
                do! connection.CloseAsync()
        }

    member _.GetConnection() = connection
    member _.DatabaseName = databaseName

    interface IDisposable with
        member _.Dispose() = connection.Dispose()
