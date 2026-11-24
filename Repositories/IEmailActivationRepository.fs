namespace PackstationFsharp.Repositories

open System.Threading.Tasks

type IEmailActivationRepository =
    abstract RequestActivationAsync : string -> Task<string>
    abstract ActivateAccountAsync : string * string -> Task<bool>
