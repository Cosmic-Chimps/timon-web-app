module TimonWebApp.Server.LinkServices

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
open TimonWebApp.Client.LinkServices
open TimonWebApp.Client.Dtos

type LinkService
    (
        ctx: IRemoteContext,
        env: IWebHostEnvironment,
        config: IConfiguration,
        dataProvider: IDataProtectionProvider
    ) =
    inherit RemoteHandler<Client.LinkServices.LinkService>()

    let endpoint, protector = getCommons config dataProvider


    override this.Handler =
        { ``get-links`` =
              fun queryParams ->
                  async {
                      let json =
                          httpAsync {
                              GET(
                                  sprintf
                                      "%s/links?page=%i"
                                      endpoint
                                      queryParams.page
                              )
                          }
                          |> Async.RunSynchronously
                          |> Response.toText

                      return
                          Json.deserializeEx<AnonymousLinkView> jsonConfig json
                  }

          ``get-club-links`` =
              ctx.Authorize
              <| fun queryParams ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let! response =
                          httpAsync {
                              GET(
                                  sprintf
                                      "%s/clubs/%O/channels/%O/links?page=%i"
                                      endpoint
                                      queryParams.clubId
                                      queryParams.channelId
                                      queryParams.page
                              )

                              Authorization(sprintf "Bearer %s" authToken)
                          }

                      return
                          response
                          |> Response.toText
                          |> Json.deserializeEx<AuthLinkView> jsonConfig
                  }

          ``create-link`` =
              ctx.Authorize
              <| fun payload ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let! response =
                          httpAsync {
                              POST(
                                  sprintf
                                      "%s/clubs/%O/channels/%O/links"
                                      endpoint
                                      payload.clubId
                                      payload.channelId
                              )

                              Authorization(sprintf "Bearer %s" authToken)
                              body
                              json (Json.serialize payload)
                          }

                      return response.statusCode
                  }

          ``add-tags`` =
              ctx.Authorize
              <| fun payload ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let! response =
                          httpAsync {
                              POST(
                                  sprintf
                                      "%s/clubs/%O/links/%s/tags"
                                      endpoint
                                      payload.clubId
                                      payload.linkId
                              )

                              Authorization(sprintf "Bearer %s" authToken)
                              body
                              json (Json.serialize payload)
                          }

                      return response.statusCode
                  }

          ``get-links-by-tag`` =
              fun queryParams ->
                  async {
                      let! response =
                          httpAsync {
                              GET(
                                  sprintf
                                      "%s/links/by-tag/%s?page=%i"
                                      endpoint
                                      queryParams.tagName
                                      queryParams.page
                              )
                          }

                      return
                          response
                          |> Response.toText
                          |> Json.deserializeEx<AnonymousLinkView> jsonConfig
                  }

          ``get-club-links-by-tag`` =
              ctx.Authorize
              <| fun queryParams ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let json =
                          httpAsync {
                              GET(
                                  sprintf
                                      "%s/clubs/%O/links/by-tag/%s?page=%i"
                                      endpoint
                                      queryParams.clubId
                                      queryParams.tagName
                                      queryParams.page
                              )

                              Authorization(sprintf "Bearer %s" authToken)
                          }
                          |> Async.RunSynchronously
                          |> Response.toText

                      return Json.deserializeEx<AuthLinkView> jsonConfig json
                  }

          ``delete-tag-from-link`` =
              ctx.Authorize
              <| fun payload ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let! response =
                          httpAsync {
                              DELETE(
                                  sprintf
                                      "%s/clubs/%O/links/%O/tags/%s"
                                      endpoint
                                      payload.clubId
                                      payload.linkId
                                      payload.tagName
                              )

                              Authorization(sprintf "Bearer %s" authToken)
                          }

                      return response.statusCode
                  }

          ``search-club-links`` =
              ctx.Authorize
              <| fun queryParams ->
                  async {
                      let! authToken = getToken ctx protector endpoint

                      let! response =
                          httpAsync {
                              GET(
                                  sprintf
                                      "%s/clubs/%O/links/search/%s?page=%i"
                                      endpoint
                                      queryParams.clubId
                                      queryParams.term
                                      queryParams.page
                              )

                              Authorization(sprintf "Bearer %s" authToken)
                          }

                      return
                          response
                          |> Response.toText
                          |> Json.deserializeEx<AuthLinkView> jsonConfig
                  }


        }
