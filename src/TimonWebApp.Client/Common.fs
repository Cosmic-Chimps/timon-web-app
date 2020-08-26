module TimonWebApp.Client.Common

open System.Text.Json.Serialization
open Zanaptak.TypedCssClasses

type Bulma = CssClasses<"https://cdnjs.cloudflare.com/ajax/libs/bulma/0.7.4/css/bulma.min.css", Naming.Verbatim>
type Mdi = CssClasses<"https://cdn.materialdesignicons.com/5.4.55/css/materialdesignicons.min.css", Naming.Verbatim>

open Elmish
open System

type Message =
    | AuthenticationRequested
    | AuthenticationError of exn

let authenticationRequested  = Cmd.ofMsg (AuthenticationRequested)

[<JsonFSharpConverter>]
type Authentication = {
    User : string;
    Token : string;
    TimeStamp : DateTime;
}

[<JsonFSharpConverter>]
type TimonConfiguration = {
    Endpoint : string
}

type ConfigurationState = NotInitialized | Success of TimonConfiguration
type AuthState = NotTried | Failed | Success
type State = {
    Authentication : AuthState
    Configuration: ConfigurationState
}