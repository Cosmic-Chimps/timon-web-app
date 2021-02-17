module TimonWebApp.Client.ChannelServices

open System
open System.Net
open System.Text.Json.Serialization
open Common
open FSharp.Data
open Bolero.Remoting
open JsonProviders


// Requests
[<JsonFSharpConverter>]
type CreateChannelPayload = { clubId: ClubId; name: string }

[<JsonFSharpConverter>]
type GetChannelFollowings = { clubId: ClubId; channelId: ChannelId }

// Response
type ChannelView = ChannelViewProvider.Root
type ChannelFollows = GetChannelFollowResultProvider.Root

type ChannelService =
    { ``get-channels``: ClubId -> Async<string> // ChannelView array
      ``create-channel``: CreateChannelPayload -> Async<HttpStatusCode>
      ``get-followings``: GetChannelFollowings -> Async<string> // ChannelFollows array
      }
    interface IRemoteService with
        member this.BasePath = "/channels"
