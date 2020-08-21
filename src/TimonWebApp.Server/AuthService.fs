namespace TimonWebApp.Server.AuthService

open System
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Hosting
open Bolero.Remoting
open Bolero.Remoting.Server
open TimonWebApp
open FsHttp
open FsHttp.DslCE
open FSharp.Json
open TimonWebApp.Client.Common
open TimonWebApp.Client.Services

type AuthService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Services.AuthService>()

    let serializerOptions = JsonSerializerOptions()
    do serializerOptions.Converters.Add(JsonFSharpConverter())

    override this.Handler =
        {
            ``sign-in`` = fun (loginRequest) -> async {
                let res = http
                            {
                                POST "http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/login"
                                body
                                json (Json.serialize loginRequest)
                            }
                            |> toText
                            |> Client.Services.LoginResponseProvider.Parse
                do! ctx.HttpContext.AsyncSignIn(loginRequest.Email, TimeSpan.FromDays(365.))
                
                let loginResponse = {
                    Token = res.AccessToken
                    TimeStamp = DateTime.UtcNow.AddSeconds(float(res.ExpiresIn))
                    User = loginRequest.Email
                }
                return Some(loginResponse)
//                if password = "password" then
//                    do! ctx.HttpContext.AsyncSignIn(username, TimeSpan.FromDays(365.))
//                    return Some username
//                else
//                    return None
            }

            ``sign-out`` = fun () -> async {
                return! ctx.HttpContext.AsyncSignOut()
            }

            ``get-user-name`` = ctx.Authorize <| fun () -> async {
                return ctx.HttpContext.User.Identity.Name
            }
        }
