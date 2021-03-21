module TimonWebApp.Client.ChannelServices

open System
open System.Net
open System.Text.Json.Serialization
open Common
open FSharp.Data
open Bolero.Remoting
open Dtos


// Requests
[<JsonFSharpConverter>]
type CreateChannelPayload = { clubId: ClubId; name: string }

[<JsonFSharpConverter>]
type GetChannelDetails =
    { clubId: ClubId
      channelId: ChannelId }

type FollowPayload =
    { clubId: ClubId
      channelId: ChannelId
      activityPubId: String }

// Response
// type ChannelView = ChannelViewProvider.Root
// type ChannelFollows = GetChannelFollowResultProvider.Root

type ChannelService =
    { ``get-channels``: ClubId -> Async<ChannelView array>
      ``get-channel-activity-pub-details``: GetChannelDetails -> Async<ChannelActivityPubDetailsView>
      ``create-channel``: CreateChannelPayload -> Async<HttpStatusCode>
      ``get-followings``: GetChannelDetails -> Async<ChannelFollowsView array>
      ``get-followers``: GetChannelDetails -> Async<ChannelFollowsView array>
      ``create-activity-pub-id``: GetChannelDetails -> Async<HttpStatusCode>
      follow: FollowPayload -> Async<HttpStatusCode> }
    interface IRemoteService with
        member this.BasePath = "/channels"
