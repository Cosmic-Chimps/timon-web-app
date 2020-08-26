namespace TimonWebApp.Server.AuthService

open System
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Hosting
open Bolero.Remoting
open Bolero.Remoting.Server
open Microsoft.Extensions.Configuration
open TimonWebApp
open FsHttp
open FsHttp.DslCE
open FSharp.Json
open TimonWebApp.Client.Common
open TimonWebApp.Client.Services

type AuthService(ctx: IRemoteContext, env: IWebHostEnvironment, config: IConfiguration) =
    inherit RemoteHandler<Client.Services.AuthService>()

    let serializerOptions = JsonSerializerOptions()
    do serializerOptions.Converters.Add(JsonFSharpConverter())

    let endpoint = config.["TimonEndPoint"]
    
    override this.Handler =
        {
            ``sign-in`` = fun (loginRequest) -> async {
                let res = http
                            {
                                POST (sprintf "%s/login" endpoint)
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
            
            links = fun () -> async {
                let! response = 
                    httpAsync {
                        GET (sprintf "%s/link" endpoint)
                    }
                let links =
                    response
                    |> toText
                    |> GetLinkResponse.Parse
                    
                return links
            }
        }
