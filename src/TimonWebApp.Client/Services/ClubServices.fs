module TimonWebApp.Client.ClubServices

open System
open System.Net
open System.Text.Json.Serialization
open Common
open FSharp.Data
open Bolero.Remoting
open JsonProviders


// Request
[<JsonFSharpConverter>]
type CreateClubPayload = { name: string; isProtected: bool }

[<JsonFSharpConverter>]
type SubscribeClubPayload = { id: Guid; name: string }

[<JsonFSharpConverter>]
type UnSubscribeClubPayload = { id: Guid; name: string }

[<JsonFSharpConverter>]
type GetClubMembers = { clubId: ClubId }

type ClubView = ClubViewProvider.Root
type ClubMember = GetClubMembersResultProvider.Root

type ClubService =
    { ``get-clubs``: unit -> Async<string> //ClubView array
      ``create-club``: CreateClubPayload -> Async<HttpStatusCode>
      ``subscribe-club``: SubscribeClubPayload -> Async<HttpStatusCode>
      ``unsubscribe-club``: UnSubscribeClubPayload -> Async<HttpStatusCode>
      ``get-other-clubs``: unit -> Async<string> // ClubView array
      ``get-members``: GetClubMembers -> Async<string> // ClubMember array
      }
    interface IRemoteService with
        member this.BasePath = "/clubs"
