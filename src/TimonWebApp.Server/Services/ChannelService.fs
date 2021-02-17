module TimonWebApp.Server.ChannelServices

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
open TimonWebApp.Client.ChannelServices

type ChannelService(ctx: IRemoteContext,
                    env: IWebHostEnvironment,
                    config: IConfiguration,
                    dataProvider: IDataProtectionProvider) =
    inherit RemoteHandler<Client.ChannelServices.ChannelService>()

    let endpoint, protector = getCommons config dataProvider

    override this.Handler =
        {
          ``get-channels`` =
              fun clubId ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let json =
                            httpAsync {
                                 GET
                                     (sprintf
                                         "%s/clubs/%O/channels"
                                          endpoint
                                          clubId)
                                 Authorization(sprintf "Bearer %s" authToken)
                             }
                             |> Async.RunSynchronously
                             |> Response.toText

                      return json
                  }

          ``create-channel`` =
              ctx.Authorize
              <| fun payload ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      return httpAsync {
                                 POST
                                     (sprintf
                                         "%s/clubs/%O/channels"
                                          endpoint
                                          payload.clubId)
                                 Authorization(sprintf "Bearer %s" authToken)
                                 body
                                 json (Json.serialize payload)
                             }
                             |> Async.RunSynchronously
                             |> fun x -> x.statusCode
                  }

          ``get-followings`` =
                  ctx.Authorize
                  <| fun queryArgs ->
                      async {
                          let! authToken = getToken ctx protector endpoint

                          let json =
                              httpAsync {
                                   GET
                                       (sprintf
                                           "%s/clubs/%O/channels/%O"
                                            endpoint
                                            queryArgs.clubId
                                            queryArgs.channelId)
                                   Authorization(sprintf "Bearer %s" authToken)
                               }
                               |> Async.RunSynchronously
                               |> Response.toText

                          return json
                       } }
