module TimonWebApp.Client.Common

open Zanaptak.TypedCssClasses

type Bulma = CssClasses<"https://cdnjs.cloudflare.com/ajax/libs/bulma/0.7.4/css/bulma.min.css", Naming.Verbatim>

open Elmish
open System

type Message =
    | AuthenticationRequested
    | AuthenticationError of exn

let authenticationRequested  = Cmd.ofMsg (AuthenticationRequested)

type Authentication = {
    User : string;
    Token : string;
    TimeStamp : DateTime;
}
type AuthState = NotTried | Failed | Success of Authentication
type State = {
    Authentication : AuthState;
}


