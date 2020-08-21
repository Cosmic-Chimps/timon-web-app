module TimonWebApp.Client.Services

open FSharp.Data
open FsHttp
open FsHttp.DslCE
open FSharp.Json
open Bolero.Remoting

type LoginRequest = {
    Email: string
    Password: string
}
let [<Literal>] loginResponseJson = """
{
  "scope": "",
  "access_token": "",
  "expires_in": 3600,
  "refresh_token": "",
  "token_type": ""
}
"""
type LoginResponse = JsonProvider<loginResponseJson>

type AuthService =
    {
        /// Sign into the application.
        ``sign-in`` : LoginRequest -> Async<option<LoginResponse.Root>>
        
        /// Get the user's name, or None if they are not authenticated.
        ``get-user-name`` : unit -> Async<string>

        /// Sign out from the application.
        ``sign-out`` : unit -> Async<unit>
    }

    interface IRemoteService with
        member this.BasePath = "/auth"

//let login (payload: LoginRequest) =
//    http {
//        POST "http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/login"
//        body
//        json (Json.serialize payload)
//    }
//    |> toText
//    |> LoginResponse.Parse

//let getLinks =
//    http {
//        GET @"https://reqres.in/api/users?page=2&delay=3"
//    }
//    |> toJson
//    |> fun json -> json?page.AsInteger()
//    