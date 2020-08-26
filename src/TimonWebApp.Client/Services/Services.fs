module TimonWebApp.Client.Services

open System.Text.Json
open FSharp.Data
open FsHttp
open FsHttp.DslCE
open Bolero.Remoting

type LoginRequest = {
    Email: string
    Password: string
}
let [<Literal>] loginResponseJson = """
{
  "access_token": "string",
  "expires_in": 3600,
  "username": "string"
}
"""
type LoginResponseProvider = JsonProvider<loginResponseJson>

type GetLinkResponse = JsonProvider<"http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/.meta/get/link">

type AuthService =
    {
        /// Sign into the application.
        ``sign-in`` : LoginRequest -> Async<option<Common.Authentication>>
        
        /// Get the user's name, or None if they are not authenticated.
        ``get-user-name`` : unit -> Async<string>

        /// Sign out from the application.
        ``sign-out`` : unit -> Async<unit>
        
        /// Sign out from the application.
        ``get-config`` : unit -> Async<Common.TimonConfiguration>
        
        links : unit -> Async<GetLinkResponse.Root array>
    }

    interface IRemoteService with
        member this.BasePath = "/auth"

let getLinks endpoint =
    async {
        let httpClient = new System.Net.Http.HttpClient()
        let url = (sprintf "%s/link" endpoint)
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
        response.EnsureSuccessStatusCode () |> ignore
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let links = GetLinkResponse.Parse(content)
        return links
    }

let asyncUpper (txt : string) =
    async {
        do! Async.Sleep 1000

        return txt.ToUpper()
    }