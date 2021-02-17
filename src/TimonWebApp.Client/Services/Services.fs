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
open TimonWebApp.Client.JsonProviders
type TimonService =
    { linkService: LinkService
      authService: AuthService
      channelService: ChannelService
      clubService: ClubService }

//#region LinkService
let getLinks (timonService, queryParams) =
    async {
        let! resp = timonService.linkService.``get-links`` queryParams
        return GetLinksResultProvider.Parse resp
    }

let getClubLinks (timonService, queryParams) =
    async {
        let! resp = timonService.linkService.``get-club-links`` queryParams
        return GetClubLinksResultProvider.Parse resp
    }

let createLink (timonService, payload) =
    async { return! timonService.linkService.``create-link`` payload }

let addTags (timonService, payload) =
    async {
        let! statusCode = timonService.linkService.``add-tags`` payload
        return Guid.Parse payload.linkId, statusCode
    }

let getLinksByTag (timonService, queryParams) =
    async {
        let! resp = timonService.linkService.``get-links-by-tag`` queryParams
        return GetLinksResultProvider.Parse resp
    }

let getClubLinksByTag (timonService, queryParams: GetClubLinkByTagsParams) =
    async {
        let! resp = timonService.linkService.``get-club-links-by-tag`` queryParams
        return GetClubLinksResultProvider.Parse resp
    }

let deleteTagFromLink (timonService, payload) =
    async {
        let! statusCode =
            timonService.linkService.``delete-tag-from-link`` payload

        return payload.linkId, statusCode
    }

let searchClubLinks (timonService, queryParams: GetClubLinkSearchParams) =
    async {
        let! resp = timonService.linkService.``search-club-links`` queryParams
        return GetClubLinksResultProvider.Parse resp
    }

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
    async {
        let! resp = timonService.channelService.``get-channels`` clubId
        return ChannelViewProvider.Parse resp
    }

let createChannel (timonService, payload) =
    async { return! timonService.channelService.``create-channel`` payload }


let getChannelFollowings (timonService: TimonService, queryParams) =
    async {
        let! resp = timonService.channelService.``get-followings`` (queryParams)
        return GetChannelFollowResultProvider.Parse resp
    }

//#endregion

//#region ClubService

let getClubs timonService =
    async {
        let! resp = timonService.clubService.``get-clubs`` ()
        return ClubViewProvider.Parse resp
    }

let createClub (timonService, payload) =
    async { return! timonService.clubService.``create-club`` payload }

let subscribeClub (timonService, payload) =
    async { return! timonService.clubService.``subscribe-club`` payload }

let unsubscribeClub (timonService, payload) =
    async { return! timonService.clubService.``unsubscribe-club`` payload }

let getOtherClubs timonService =
    async {
        let! resp = timonService.clubService.``get-other-clubs`` ()
        return ClubViewProvider.Parse resp
    }

let getMembers (timonService, queryParams) =
    async {
        let! resp = timonService.clubService.``get-members`` (queryParams)
        return GetClubMembersResultProvider.Parse resp
    }
