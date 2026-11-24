namespace PackstationFsharp.Repositories

open System.Threading.Tasks
open PackstationFsharp.Models

type IUserRepository =
    abstract AddNewAccountAsync : UserAccount -> Task<unit>
    abstract FindAccountByEmailAsync : string -> Task<UserAccount option>
    abstract FindAccountByUserId : string -> Task<UserAccount option>
    abstract UpdateAccountAsync : UserAccount * string -> Task<unit>
    abstract DeleteAccountAsync : string -> Task<unit>
    abstract GetAllUsersAsync : unit -> Task<ResizeArray<UserAccount>>
    abstract UpdateProfileImageAsync : string * string -> Task<unit>
