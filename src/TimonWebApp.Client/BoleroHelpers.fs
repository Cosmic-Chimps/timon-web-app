module TimonWebApp.Client.BoleroHelpers

open System
open Bolero
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open Bolero.Html


type KeySubscriber() =
    inherit Component()

    interface IDisposable with
        member this.Dispose() =
            this.JsRunTime.InvokeVoidAsync("generalFunctions.removeOnKeyUp")
            |> ignore

    [<Inject>]
    member val JsRunTime: IJSRuntime = Unchecked.defaultof<_> with get, set

    override this.Render() = empty

    override this.OnAfterRenderAsync firstTime =
        async {
            if firstTime then
                return! this.JsRunTime.InvokeVoidAsync("generalFunctions.registerForOnKeyUp").AsTask()
                        |> Async.AwaitTask
            else
                return ()
        }
        |> Async.StartImmediateAsTask :> _
