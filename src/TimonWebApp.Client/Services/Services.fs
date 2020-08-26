module TimonWebApp.Client.Services

open System
open Common
open FSharp.Data
open Bolero.Remoting
open TimonWebApp.Client

//let httpClientWithNoRedirects () =
//    let handler = new HttpClientHandler(UseCookies = false)
//    handler.AllowAutoRedirect <- false
//    let client = new HttpClient(handler)
//    client.DefaultRequestHeaders.Clear()
//    client
//
//// we can trivially extend request to add convenience functions for common operations
//module Request =
//    let autoFollowRedirectsDisabled h = 
//        { h with httpClient = httpClientWithNoRedirects () }

type LoginRequest = {
    Email: string
    Password: string
}

type LinkViewProvider = JsonProvider<"http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/.meta/get/link">
type LinkView = LinkViewProvider.Root

type AuthService =
    {
        /// Sign into the application.
//        ``sign-in`` : LoginRequest -> Async<option<Authentication>>
        ``sign-in`` : LoginRequest -> Async<string>
        
        /// Get the user's name, or None if they are not authenticated.
        ``get-user-name`` : unit -> Async<string>

        /// Sign out from the application.
        ``sign-out`` : unit -> Async<unit>
        
        /// Sign out from the application.
        ``get-config`` : unit -> Async<Common.TimonConfiguration>
    }

    interface IRemoteService with
        member this.BasePath = "/auth"
        
type LinkService =
    {
        ``get-links`` : unit -> Async<string>
    }

    interface IRemoteService with
        member this.BasePath = "/links"

type TimonService = {
     LinkService: LinkService
     AuthService: AuthService
}


let logIn (timonService,loginRequest) =
    async {
        let! resp = timonService.AuthService.``sign-in`` loginRequest
        return Some(resp)
    }

let getLinks timonService =
    async {
        let! resp = timonService.LinkService.``get-links``()
        return LinkViewProvider.Parse resp

//        let httpClient = new System.Net.Http.HttpClient()
//        let url = (sprintf "%s/link" endpoint)
//        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
//        response.EnsureSuccessStatusCode () |> ignore
//        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
//        let links = LinkViewProvider.Parse(content)
//        return links
    }
