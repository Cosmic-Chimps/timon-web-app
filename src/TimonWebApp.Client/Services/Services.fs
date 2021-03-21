module TimonWebApp.Client.Services

open System
open System.Net
open System.Text.Json.Serialization
open Common
open FSharp.Data
open Bolero.Remoting
open ChannelServices
open AuthServices
open LinkServices
open ClubServices
open TimonWebApp.Client.Dtos

type TimonService =
    { linkService: LinkService
      authService: AuthService
      channelService: ChannelService
      clubService: ClubService }

//#region LinkService
let getLinks (timonService, queryParams) =
    async { return! timonService.linkService.``get-links`` queryParams }

let getClubLinks (timonService, queryParams) =
    async { return! timonService.linkService.``get-club-links`` queryParams }

let createLink (timonService, payload) =
    async { return! timonService.linkService.``create-link`` payload }

let addTags (timonService, payload) =
    async {
        let! statusCode = timonService.linkService.``add-tags`` payload
        return Guid.Parse payload.linkId, statusCode
    }

let getLinksByTag (timonService, queryParams) =
    async { return! timonService.linkService.``get-links-by-tag`` queryParams }

let getClubLinksByTag (timonService, queryParams: GetClubLinkByTagsParams) =
    async {
        return! timonService.linkService.``get-club-links-by-tag`` queryParams }

let deleteTagFromLink (timonService, payload) =
    async {
        let! statusCode =
            timonService.linkService.``delete-tag-from-link`` payload

        return payload.linkId, statusCode
    }

let searchClubLinks (timonService, queryParams: GetClubLinkSearchParams) =
    async { return! timonService.linkService.``search-club-links`` queryParams }

//#endregion

//#region AuthService

let logIn (timonService, loginRequest) =
    async {
        let! resp = timonService.authService.``sign-in`` loginRequest
        return Some(resp)
    }

let signUp (timonService, signUpRequest) =
    async {
        let! resp = timonService.authService.``sign-up`` signUpRequest
        return Some(resp)
    }

//#endregion

//#region ChannelService

let getChannels (timonService, clubId) =
    async { return! timonService.channelService.``get-channels`` clubId }

let getChanneActivityPublDetails (timonService, queryParams) =
    async {
        return!
            timonService.channelService.``get-channel-activity-pub-details``
                queryParams
    }

let createChannel (timonService, payload) =
    async { return! timonService.channelService.``create-channel`` payload }

let getChannelFollowings (timonService: TimonService, queryParams) =
    async {
        return! timonService.channelService.``get-followings`` (queryParams) }

let getChannelFollowers (timonService: TimonService, queryParams) =
    async {
        return! timonService.channelService.``get-followers`` (queryParams) }

let follow (timonService, payload) =
    async { return! timonService.channelService.follow payload }

let createActivityPubId (timonService, payload) =
    async {
        return! timonService.channelService.``create-activity-pub-id`` payload }
//#endregion

//#region ClubService

let getClubs timonService =
    async { return! timonService.clubService.``get-clubs`` () }

let createClub (timonService, payload) =
    async { return! timonService.clubService.``create-club`` payload }

let subscribeClub (timonService, payload) =
    async { return! timonService.clubService.``subscribe-club`` payload }

let unsubscribeClub (timonService, payload) =
    async { return! timonService.clubService.``unsubscribe-club`` payload }

let getOtherClubs timonService =
    async { return! timonService.clubService.``get-other-clubs`` () }

let getMembers (timonService, queryParams) =
    async { return! timonService.clubService.``get-members`` (queryParams) }
