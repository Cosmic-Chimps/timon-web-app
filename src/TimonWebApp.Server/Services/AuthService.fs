module TimonWebApp.Server.AuthServices

open System
open System.Net
open System.Security.Claims
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.Hosting
open Bolero.Remoting
open Bolero.Remoting.Server
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open TimonWebApp
open FsHttp.DslCE
open FsHttp
open FSharp.Json
open TimonWebApp.Client.Services
open TimonWebApp.Server
open FSharp.Data
open System.Collections.Generic
open TimonWebApp.Server.HelperService

type AuthService(ctx: IRemoteContext,
                 env: IWebHostEnvironment,
                 config: IConfiguration,
                 dataProvider: IDataProtectionProvider) =
    inherit RemoteHandler<Client.AuthServices.AuthService>()

    let endpoint, protector = getCommons config dataProvider

    override this.Handler =
        { ``sign-in`` =
              fun loginRequest ->
                  async {
                      let result =
                          httpAsync {
                              POST(sprintf "%s/login" endpoint)
                              body
                              json (Json.serialize loginRequest)
                          }
                          |> Async.RunSynchronously
                          |> hasResponseValidStatus
                          |> getResponseBodyAsText
                          |> parseBodyAsObject<TokenProvider.Root>
                              (TokenProvider.Parse)
                          |> fun x ->
                              match x with
                              | Some r ->
                                  singInUser ctx protector loginRequest.email r
                                  |> Async.RunSynchronously
                                  |> Some
                              | None -> None

                      return match result with
                             | Some token -> getDisplayNameFromToken token

                             | None -> failwith "Not Found"
                  }

          ``sign-up`` =
              fun signUpRequest ->
                  async {
                      let result =
                          httpAsync {
                              POST(sprintf "%s/register" endpoint)
                              body
                              json (Json.serialize signUpRequest)
                          }
                          |> Async.RunSynchronously
                          |> hasResponseValidStatus
                          |> getResponseBodyAsText
                          |> parseBodyAsObject<TokenProvider.Root>
                              (TokenProvider.Parse)
                          |> fun x ->
                              match x with
                              | Some r ->
                                  singInUser ctx protector signUpRequest.email r
                                  |> Async.RunSynchronously
                                  |> Some
                              | None -> None

                      return match result with
                             | Some token -> getDisplayNameFromToken token
                             | None -> failwith "Not Found"
                  }

          ``sign-out`` =
              fun () -> async { return! ctx.HttpContext.AsyncSignOut() }

          ``get-user-name`` =
              ctx.Authorize
              <| fun () -> async { return getDisplayNameFromAccessToken ctx }

          ``get-config`` =
              fun () ->
                  async {
                      let endpoint = config.["TimonEndpoint"]
                      return { Endpoint = endpoint }
                  } }
