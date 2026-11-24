namespace PackstationFsharp

open System
open System.Data.Common
open System.Threading.Tasks
open MySqlConnector

/// Small helpers used by every repository to translate database values
/// (including NULL / DBNull) to the F# models and vice versa.
module DbHelpers =

    let isDbNull (value: obj) =
        isNull value || obj.ReferenceEquals(value, DBNull.Value)

    let stringOrNull (value: obj) =
        if isDbNull value then null else value.ToString()

    let stringOr (fallback: string) (value: obj) =
        if isDbNull value then fallback else value.ToString()

    let intOr (fallback: int) (value: obj) =
        if isDbNull value then fallback else Convert.ToInt32(value)

    let boolOr (fallback: bool) (value: obj) =
        if isDbNull value then fallback else Convert.ToBoolean(value)

    let dateTimeOr (fallback: DateTime) (value: obj) =
        if isDbNull value then fallback else Convert.ToDateTime(value)

    let nullableDate (value: obj) =
        if isDbNull value then Nullable<DateTime>() else Nullable<DateTime>(Convert.ToDateTime(value))

    let dbValue (value: string) : obj =
        if isNull value then box DBNull.Value else box value

    let dbDate (value: Nullable<DateTime>) : obj =
        if value.HasValue then box value.Value else box DBNull.Value

    /// Awaits a task whose result is not needed (for example ExecuteNonQueryAsync,
    /// which returns the affected row count) inside a task computation expression.
    let ignoreTask (work: Task<'T>) : Task<unit> =
        task {
            let! _ = work
            return ()
        }

    let readAll (reader: DbDataReader) (map: DbDataReader -> 'T) : Task<ResizeArray<'T>> =
        task {
            let results = ResizeArray<'T>()
            let mutable reading = true
            while reading do
                let! hasRow = reader.ReadAsync()
                if hasRow then
                    results.Add(map reader)
                else
                    reading <- false
            return results
        }
