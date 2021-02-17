module TimonWebApp.Server.HelperService

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

type TokenProvider = JsonProvider<Constants.tokenResponseJson>

type RefreshTokenRequest = { refreshToken: string }

let getCommons (config: IConfiguration) (dataProvider: IDataProtectionProvider) =
    let protector =
        dataProvider.CreateProtector(Constants.providerKey)

    let endpoint = config.["TimonEndPoint"]
    (endpoint, protector)

let singInUser (ctx: IRemoteContext)
               (protector: IDataProtector)
               email
               (res: TokenProvider.Root)
               =
    async {
        let refreshTokenProtected = protector.Protect(res.RefreshToken)

        let expiresAt =
            DateTime.UtcNow.Add(TimeSpan.FromSeconds(float (res.ExpiresIn)))

        let claims =
            [ Claim("TimonToken", res.AccessToken)
              Claim("TimonRefreshToken", refreshTokenProtected)
              Claim("TimonExpiredDate", expiresAt.ToString()) ]

        do! ctx.HttpContext.AsyncSignIn
                (email, claims = claims, persistFor = TimeSpan.FromDays(365.))

        return res.AccessToken
    }

let renewToken endpoint refreshTokenRequest =
    httpAsync {
        POST(sprintf "%s/refresh-token" endpoint)
        body
        json (Json.serialize refreshTokenRequest)
    }
    |> Async.RunSynchronously
    |> Response.toText
    |> TokenProvider.Parse

let getDisplayNameFromToken token =
    let payload = Jose.JWT.Payload(token)

    let map =
        JsonSerializer.Deserialize<Dictionary<string, Object>>(payload)

    match map.ContainsKey("timonUserDisplayName") with
    | true -> map.["timonUserDisplayName"].ToString()
    | false -> map.["email"].ToString()


let getDisplayNameFromAccessToken (ctx: IRemoteContext) =
    ctx.HttpContext.User.Claims
    |> Seq.find (fun c -> c.Type = "TimonToken")
    |> fun token -> getDisplayNameFromToken token.Value

let getToken (ctx: IRemoteContext) (protector: IDataProtector) endpoint =
    async {
        let expireAt =
            ctx.HttpContext.User.Claims
            |> Seq.find (fun c -> c.Type = "TimonExpiredDate")
            |> fun c -> DateTime.Parse(c.Value)

        let timonRefreshToken =
            ctx.HttpContext.User.Claims
            |> Seq.find (fun c -> c.Type = "TimonRefreshToken")
            |> fun c -> c.Value
            |> protector.Unprotect

        let timonToken =
            ctx.HttpContext.User.Claims
            |> Seq.find (fun c -> c.Type = "TimonToken")
            |> fun c -> c.Value

        return! match expireAt < DateTime.UtcNow with
                | true ->
                    renewToken endpoint { refreshToken = timonRefreshToken }
                    |> singInUser
                        ctx
                           protector
                           ctx.HttpContext.User.Identity.Name
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
    | Some r -> Some(Response.toText r)
    | None -> None

let parseBodyAsObject<'T> (parse: string -> 'T) body =
    match body with
    | Some r -> Some(parse (r))
    | None -> None
