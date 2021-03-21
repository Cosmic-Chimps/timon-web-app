module TimonWebApp.Server.ClubServices

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
open TimonWebApp.Client.ClubServices
open FSharp.Json
open TimonWebApp.Client.Dtos

type ClubService
    (
        ctx: IRemoteContext,
        env: IWebHostEnvironment,
        config: IConfiguration,
        dataProvider: IDataProtectionProvider
    ) =
    inherit RemoteHandler<Client.ClubServices.ClubService>()

    let endpoint, protector = getCommons config dataProvider

    override this.Handler =
        { ``get-other-clubs`` =
              ctx.Authorize
              <| fun () ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let json =
                          httpAsync {
                              GET(sprintf "%s/clubs/others" endpoint)
                              Authorization(sprintf "Bearer %s" authToken)
                          }
                          |> Async.RunSynchronously
                          |> Response.toText
                          |> fun r ->
                              match r with
                              | "" -> "[]"
                              | _ -> r

                      return Json.deserializeEx<ClubView array> jsonConfig json
                  }

          ``get-clubs`` =
              ctx.Authorize
              <| fun () ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let json =
                          httpAsync {
                              GET(sprintf "%s/clubs" endpoint)
                              Authorization(sprintf "Bearer %s" authToken)
                          }
                          |> Async.RunSynchronously
                          |> Response.toText
                          |> fun r ->
                              match r with
                              | "" -> "[]"
                              | _ -> r


                      return Json.deserializeEx<ClubView array> jsonConfig json
                  }

          ``create-club`` =
              ctx.Authorize
              <| fun payload ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      return
                          httpAsync {
                              POST(sprintf "%s/clubs" endpoint)
                              Authorization(sprintf "Bearer %s" authToken)
                              body
                              json (Json.serialize payload)
                          }
                          |> Async.RunSynchronously
                          |> fun x -> x.statusCode
                  }

          ``subscribe-club`` =
              ctx.Authorize
              <| fun payload ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      return
                          httpAsync {
                              POST(sprintf "%s/clubs/subscribe" endpoint)
                              Authorization(sprintf "Bearer %s" authToken)
                              body
                              json (Json.serialize payload)
                          }
                          |> Async.RunSynchronously
                          |> fun x -> x.statusCode
                  }

          ``unsubscribe-club`` =
              ctx.Authorize
              <| fun payload ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      return
                          httpAsync {
                              POST(sprintf "%s/clubs/unsubscribe" endpoint)
                              Authorization(sprintf "Bearer %s" authToken)
                              body
                              json (Json.serialize payload)
                          }
                          |> Async.RunSynchronously
                          |> fun x -> x.statusCode
                  }

          ``get-members`` =
              ctx.Authorize
              <| fun queryParams ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let json =
                          httpAsync {
                              GET(
                                  sprintf
                                      "%s/clubs/%O/members"
                                      endpoint
                                      queryParams.clubId
                              )

                              Authorization(sprintf "Bearer %s" authToken)
                          }
                          |> Async.RunSynchronously
                          |> Response.toText
                          |> Json.deserializeEx<ClubMembersView array>
                              jsonConfig

                      return json
                  } }
