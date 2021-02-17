module TimonWebApp.Client.AuthServices

open System
open System.Net
open System.Text.Json.Serialization
open Common
open FSharp.Data
open Bolero.Remoting


[<JsonFSharpConverter>]
type LoginRequest = { email: string; password: string }

[<JsonFSharpConverter>]
type SignUpRequest =
    { userName: string
      firstName: string
      lastName: string
      email: string
      password: string
      confirmPassword: string }

type AuthService =
    {
      /// Sign into the application.
      ``sign-in``: LoginRequest -> Async<string>
      ``sign-up``: SignUpRequest -> Async<string>
      /// Get the user's name, or None if they are not authenticated.
      ``get-user-name``: unit -> Async<string>
      /// Sign out from the application.
      ``sign-out``: unit -> Async<unit>
      /// Sign out from the application.
      ``get-config``: unit -> Async<TimonConfiguration> }

    interface IRemoteService with
        member this.BasePath = "/auth"
