namespace PackstationFsharp.Models

type ErrorViewModel() =
    member val RequestId : string = null with get, set

    member this.ShowRequestId = not (System.String.IsNullOrEmpty(this.RequestId))
