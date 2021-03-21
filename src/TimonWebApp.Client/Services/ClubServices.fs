module TimonWebApp.Client.ClubServices

open System
open System.Net
open System.Text.Json.Serialization
open Common
open FSharp.Data
open Bolero.Remoting
open Dtos


// Request
[<JsonFSharpConverter>]
type CreateClubPayload = { name: string; isProtected: bool }

[<JsonFSharpConverter>]
type SubscribeClubPayload = { id: Guid; name: string }

[<JsonFSharpConverter>]
type UnSubscribeClubPayload = { id: Guid; name: string }

[<JsonFSharpConverter>]
type GetClubMembers = { clubId: ClubId }

// type ClubView = ClubViewProvider.Root
// type ClubMember = GetClubMembersResultProvider.Root

type ClubService =
    { ``get-clubs``: unit -> Async<ClubView array>
      ``create-club``: CreateClubPayload -> Async<HttpStatusCode>
      ``subscribe-club``: SubscribeClubPayload -> Async<HttpStatusCode>
      ``unsubscribe-club``: UnSubscribeClubPayload -> Async<HttpStatusCode>
      ``get-other-clubs``: unit -> Async<ClubView array>
      ``get-members``: GetClubMembers -> Async<ClubMembersView array> }
    interface IRemoteService with
        member this.BasePath = "/clubs"
