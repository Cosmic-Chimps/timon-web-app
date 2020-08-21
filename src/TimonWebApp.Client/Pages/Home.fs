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
    | Noupe -> { model with X = 100}

let view model dispatch =
    div[][
        a [attr.href "/login"] [text "Go to Login"]
        p[][
            text "Hello World 88"
        ]
    ]