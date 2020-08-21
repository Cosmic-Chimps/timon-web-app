module TimonWebApp.Client.Pages.Home

open Elmish
open Bolero
open Bolero.Html

type Model = {
    X: int
}
with
    static member Default = {
        X = 10
    }
    
type Message =
    | Noupe
    
let init jsRuntime =
    Model.Default, Cmd.none

let update message model =
    match message with
    | Noupe -> { model with X = 200}


type HomeTemplate = Template<"wwwroot/home.html">

let view model dispatch =
    HomeTemplate().Elt()