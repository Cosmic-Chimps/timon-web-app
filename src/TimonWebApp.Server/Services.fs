module TimonWebApp.Server.AuthService

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

type TokenProvider = JsonProvider<Constants.tokenResponseJson>

type RefreshTokenRequest = {
    refreshToken: string
}

let getCommons (config: IConfiguration) (dataProvider: IDataProtectionProvider) =
    let protector = dataProvider.CreateProtector(Constants.providerKey)
    let endpoint = config.["TimonEndPoint"]
    (endpoint, protector)

let singInUser (ctx: IRemoteContext) (protector: IDataProtector) email (res: TokenProvider.Root) = async {
        printf "%s" res.RefreshToken
        let refreshTokenProtected = protector.Protect(res.RefreshToken)
        let expiresAt = DateTime.UtcNow.Add(TimeSpan.FromSeconds(float(res.ExpiresIn)))
        let claims = [
            Claim("TimonToken", res.AccessToken)
            Claim("TimonRefreshToken", refreshTokenProtected)
            Claim("TimonExpiredDate", expiresAt.ToString())
        ]

        do! ctx.HttpContext.AsyncSignIn(email, claims = claims, persistFor = TimeSpan.FromDays(365.))

        return res.AccessToken
    }

let renewToken endpoint refreshTokenRequest =
    httpAsync
        {
            POST (sprintf "%s/refresh-token" endpoint)
            body
            json (Json.serialize refreshTokenRequest)
        }
        |> Async.RunSynchronously
        |> toText
        |> TokenProvider.Parse

let getToken (ctx: IRemoteContext) (protector: IDataProtector) endpoint = async {
        let expireAt = ctx.HttpContext.User.Claims
                        |> Seq.find (fun c ->  c.Type = "TimonExpiredDate" )
                        |> fun c -> DateTime.Parse(c.Value)

        let timonRefreshToken = ctx.HttpContext.User.Claims
                                |> Seq.find (fun c ->  c.Type = "TimonRefreshToken" )
                                |> fun c -> c.Value
                                |> protector.Unprotect

        let timonToken = ctx.HttpContext.User.Claims
                        |> Seq.find (fun c ->  c.Type = "TimonToken" )
                        |> fun c -> c.Value

        return! match expireAt < DateTime.UtcNow with
                 | true ->
                    renewToken endpoint { refreshToken = timonRefreshToken }
                    |>  singInUser ctx protector ctx.HttpContext.User.Identity.Name
                 | false -> async { return timonToken }
    }

let hasResponseValidStatus response =
    match response.statusCode with
    | HttpStatusCode.Accepted
    | HttpStatusCode.OK
    | HttpStatusCode.Created -> Some response
    | _ -> None

let getResponseBodyAsText response =
    match response with
    | Some r -> Some (toText r)
    | None -> None

let parseBodyAsObject<'T> (parse : string -> 'T) body  =
    match body with
    | Some r -> Some (parse(r))
    | None -> None
type AuthService(ctx: IRemoteContext, env: IWebHostEnvironment, config: IConfiguration, dataProvider: IDataProtectionProvider) =
    inherit RemoteHandler<Client.Services.AuthService>()

    let endpoint, protector = getCommons config dataProvider

    override this.Handler = {
        ``sign-in`` = fun (loginRequest) -> async {
            let result = httpAsync
                            {
                                POST (sprintf "%s/login" endpoint)
                                body
                                json (Json.serialize loginRequest)
                            }
                            |> Async.RunSynchronously
                            |> hasResponseValidStatus
                            |> getResponseBodyAsText
                            |> parseBodyAsObject<TokenProvider.Root> (TokenProvider.Parse)
                            |> fun (x) ->
                                match x with
                                | Some r ->
                                    singInUser ctx protector loginRequest.email r
                                    |> Async.RunSynchronously
                                    |> Some
                                | None -> None

            return match result with
                   | Some _ -> loginRequest.email
                   | None -> failwith "Not Found"
        }

        ``sign-up`` = fun (signUpRequest) -> async {
            let result = httpAsync
                            {
                                POST (sprintf "%s/register" endpoint)
                                body
                                json (Json.serialize signUpRequest)
                            }
                            |> Async.RunSynchronously
                            |> hasResponseValidStatus
                            |> getResponseBodyAsText
                            |> parseBodyAsObject<TokenProvider.Root> (TokenProvider.Parse)
                            |> fun (x) ->
                                match x with
                                | Some r ->
                                    singInUser ctx protector signUpRequest.email r
                                    |> Async.RunSynchronously
                                    |> Some
                                | None -> None

            return match result with
                   | Some _ -> signUpRequest.email
                   | None -> failwith "Not Found"
        }

        ``sign-out`` = fun () -> async {
            return! ctx.HttpContext.AsyncSignOut()
        }

        ``get-user-name`` = ctx.Authorize <| fun () -> async {
            return
                match ctx.HttpContext.TryUsername() with
                | Some email -> email
                | None -> ""
//            return ctx.HttpContext.User.Identity.Name
        }

        ``get-config`` = fun () -> async {
            let endpoint = config.["TimonEndpoint"]
            return {
                Endpoint = endpoint
            }
        }
    }

type LinkService(ctx: IRemoteContext, env: IWebHostEnvironment, config: IConfiguration, dataProvider: IDataProtectionProvider) =
    inherit RemoteHandler<Client.Services.LinkService>()

    let endpoint, protector = getCommons config dataProvider

    override this.Handler = {
        ``get-links`` = fun (queryParams) -> async {
            let! response =
                    httpAsync {
                        GET (sprintf "%s/links?channelId=%O&page=%i" endpoint queryParams.channelId queryParams.page)
                    }

            let links =
                response
                |> toText

            return links
        }

        ``create-link`` = ctx.Authorize <| fun (payload) -> async {
            let! authToken = getToken ctx protector endpoint
            let! response = httpAsync {
                    POST (sprintf "%s/links" endpoint)
                    Authorization (sprintf "Bearer %s" authToken)
                    body
                    json (Json.serialize payload)
                }

            return response.statusCode
        }

        ``add-tags`` = ctx.Authorize <| fun (payload) -> async {
            let! authToken = getToken ctx protector endpoint
            let! response = httpAsync {
                                POST (sprintf "%s/links/%s/tags" endpoint payload.linkId)
                                Authorization (sprintf "Bearer %s" authToken)
                                body
                                json (Json.serialize payload)
                            }
            return response.statusCode
        }
    }

type ChannelService (ctx: IRemoteContext, env: IWebHostEnvironment, config: IConfiguration, dataProvider: IDataProtectionProvider) =
    inherit RemoteHandler<Client.Services.ChannelService>()

    let endpoint, protector = getCommons config dataProvider

    override this.Handler = {
        ``get-channels`` = fun () -> async {
            return httpAsync {
                        GET (sprintf "%s/channels" endpoint)
                    }
                    |> Async.RunSynchronously
                    |> toText
        }

        ``create-channel`` = ctx.Authorize <| fun (payload) -> async {
            let! authToken = getToken ctx protector endpoint
            return httpAsync {
                        POST (sprintf "%s/channels" endpoint)
                        Authorization (sprintf "Bearer %s" authToken)
                        body
                        json (Json.serialize payload)
                    }
                    |> Async.RunSynchronously
                    |> fun x -> x.statusCode
        }
    }