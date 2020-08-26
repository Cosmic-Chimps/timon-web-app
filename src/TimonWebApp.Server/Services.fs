namespace TimonWebApp.Server.AuthService

open System
open System.Security.Claims
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.Hosting
open Bolero.Remoting
open Bolero.Remoting.Server
open Microsoft.Extensions.Configuration
open TimonWebApp
open FsHttp.DslCE
open FsHttp
open FSharp.Json
open TimonWebApp.Client.Common
open TimonWebApp.Client.Services
open TimonWebApp.Server
open FSharp.Data

type LoginProvider = JsonProvider<Constants.loginResponseJson>

type AuthService(ctx: IRemoteContext, env: IWebHostEnvironment, config: IConfiguration, dataProvider: IDataProtectionProvider) =
    inherit RemoteHandler<Client.Services.AuthService>()

    let protector = dataProvider.CreateProtector(Constants.Key);
    let serializerOptions = JsonSerializerOptions()
    do serializerOptions.Converters.Add(JsonFSharpConverter())

    let endpoint = config.["TimonEndPoint"]
    
    override this.Handler = {
        ``sign-in`` = fun (loginRequest) -> async {
            let res = http
                        {
                            POST (sprintf "%s/login" endpoint)
                            body
                            json (Json.serialize loginRequest)
                        }
                        |> toText
                        |> LoginProvider.Parse
                        
            let refreshTokenProtected = protector.Protect(res.RefreshToken);
            let claims = [Claim("TimonToken", res.AccessToken); Claim("TimonRefreshToken", refreshTokenProtected) ]

            do! ctx.HttpContext.AsyncSignIn(res.Username, claims = claims, persistFor = TimeSpan.FromDays(365.))
            
//            let loginResponse = {
//                Token = res.AccessToken
//                TimeStamp = DateTime.UtcNow.AddSeconds(float(res.ExpiresIn))
//                User = loginRequest.Email
//            }
            return loginRequest.Email
//            let! res =
//                Request.createUrl Post (sprintf "%s/login" endpoint)
//                |> Request.setHeader (ContentType (ContentType.create("application", "json")))
//                |> Request.bodyStringEncoded (Json.serialize loginRequest) (Encoding.UTF8)
//                |> getResponse
//                |> Job.bind Response.readBodyAsString
//                |> Job.map LoginProvider.Parse
//                |> Job.toAsync
//
//            
//            let refreshTokenProtected = protector.Protect(res.RefreshToken);
//            let claims = [Claim("TimonToken", res.AccessToken); Claim("TimonRefreshToken", refreshTokenProtected) ]
//
//            do! ctx.HttpContext.AsyncSignIn(res.Username, claims = claims, persistFor = TimeSpan.FromDays(365.))
//
//            return res.Username
        }

        ``sign-out`` = fun () -> async {
            return! ctx.HttpContext.AsyncSignOut()
        }

        ``get-user-name`` = ctx.Authorize <| fun () -> async {
            return ctx.HttpContext.User.Identity.Name
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

    let protector = dataProvider.CreateProtector(Constants.Key)
    
    let serializerOptions = JsonSerializerOptions()
    do serializerOptions.Converters.Add(JsonFSharpConverter())

    let endpoint = config.["TimonEndPoint"]
    
    override this.Handler = {
        ``get-links`` = fun () -> async {
            let endpoint = sprintf "%s/link" endpoint
            return! Request.createUrl Get endpoint
                    |> getResponse
                    |> Job.bind Response.readBodyAsString
//                    |> Job.map LinkViewProvider.Parse
                    |> Job.toAsync
                
        }
    }