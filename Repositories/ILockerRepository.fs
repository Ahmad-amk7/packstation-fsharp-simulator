namespace PackstationFsharp.Repositories

open System.Threading.Tasks
open PackstationFsharp.Models

type ILockerRepository =
    abstract GetAllLockersAsync : unit -> Task<ResizeArray<Locker>>
    abstract GetLockerByIdAsync : string -> Task<Locker option>
    abstract AddLockerAsync : Locker -> Task<unit>
    abstract GetCompartmentsByLockerAsync : string -> Task<ResizeArray<Compartment>>
    abstract GetCompartmentByIdAsync : string -> Task<Compartment option>
    abstract GetFreeCompartmentAsync : string -> Task<Compartment option>
    abstract UpdateCompartmentStatusAsync : string * CompartmentStatus * string -> Task<unit>
    abstract AddCompartmentAsync : Compartment -> Task<unit>
