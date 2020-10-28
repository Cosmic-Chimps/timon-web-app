module TimonWebApp.Client.Services

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

[<JsonFSharpConverter>]
type CreateLinkPayload =
    { url: string
      channelId: string
      via: string
      tagName: string
      clubId: ClubId }

[<JsonFSharpConverter>]
type AddTagPayload = { clubId: ClubId; linkId: string; tags: string }

[<JsonFSharpConverter>]
type GetLinkParams = { page: int }

[<JsonFSharpConverter>]
type GetClubLinkParams =
    { clubId: Guid
      channelId: Guid
      page: int }

[<JsonFSharpConverter>]
type GetLinkByTagsParams = { tagName: string; page: int }

[<JsonFSharpConverter>]
type GetClubLinkByTagsParams = { clubId: ClubId; tagName: string; page: int }

[<JsonFSharpConverter>]
type DeleteTagFromLink = { clubId: ClubId; linkId: Guid; tagName: string }

[<JsonFSharpConverter>]
type GetClubLinkSearchParams = { clubId: ClubId; term: string; page: int }


#if DEBUG
type GetLinksResultProvider = JsonProvider<"http://localhost:5011/.meta/v10/get/links">
type ChannelViewProvider = JsonProvider<"http://localhost:5011/.meta/v10/get/channels">
type ClubViewProvider = JsonProvider<"http://localhost:5011/.meta/v10/get/clubs">
type GetClubLinksResultProvider = JsonProvider<"http://localhost:5011/.meta/v10/get/clubs/links">
#else
type GetLinksResultProvider = JsonProvider<"http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/.meta/v3/get/links">
type ChannelViewProvider = JsonProvider<"http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/.meta/get/v3/channels">
#endif
type GetLinksResult = GetLinksResultProvider.Root
type ChannelView = ChannelViewProvider.Root
type ClubView = ClubViewProvider.Root
type ClubListView = GetClubLinksResultProvider.Root

type LinkService =
    { ``get-links``: GetLinkParams -> Async<string>
      ``get-club-links``: GetClubLinkParams -> Async<string>
      ``create-link``: CreateLinkPayload -> Async<HttpStatusCode>
      ``add-tags``: AddTagPayload -> Async<HttpStatusCode>
      ``get-links-by-tag``: GetLinkByTagsParams -> Async<string>
      ``get-club-links-by-tag``: GetClubLinkByTagsParams -> Async<string>
      ``delete-tag-from-link``: DeleteTagFromLink -> Async<HttpStatusCode>
      ``search-club-links``: GetClubLinkSearchParams -> Async<string> }

    interface IRemoteService with
        member this.BasePath = "/links"

[<JsonFSharpConverter>]
type CreateChannelPayload = { clubId: ClubId; name: string }

type ChannelService =
    { ``get-channels``: ClubId -> Async<string>
      ``create-channel``: CreateChannelPayload -> Async<HttpStatusCode> }
    interface IRemoteService with
        member this.BasePath = "/channels"

[<JsonFSharpConverter>]
type CreateClubPayload = { name: string }

type ClubService =
    { ``get-clubs``: unit -> Async<string>
      ``create-club``: CreateClubPayload -> Async<HttpStatusCode> }
    interface IRemoteService with
        member this.BasePath = "/clubs"


type TimonService =
    { linkService: LinkService
      authService: AuthService
      channelService: ChannelService
      clubService: ClubService }

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

let getLinks (timonService, queryParams) =
    async {
        let! resp = timonService.linkService.``get-links`` queryParams
        return GetLinksResultProvider.Parse resp
    }

let getLinksByTag (timonService, queryParams) =
    async {
        let! resp = timonService.linkService.``get-links-by-tag`` queryParams
        return GetLinksResultProvider.Parse resp
    }

let searchClubLinks (timonService, queryParams: GetClubLinkSearchParams) =
    async {
        let! resp = timonService.linkService.``search-club-links`` queryParams
        return GetClubLinksResultProvider.Parse resp
    }

let createLink (timonService, payload) =
    async { return! timonService.linkService.``create-link`` payload }

let deleteTagFromLink (timonService, payload) =
    async {
        let! statusCode = timonService.linkService.``delete-tag-from-link`` payload
        return payload.linkId, statusCode
    }

let getChannels (timonService, clubId) =
    async {
        let! resp = timonService.channelService.``get-channels`` clubId
        return ChannelViewProvider.Parse resp
    }

let createChannel (timonService, payload) =
    async { return! timonService.channelService.``create-channel`` payload }

let addTags (timonService, payload) =
    async {
        let! statusCode = timonService.linkService.``add-tags`` payload
        return Guid.Parse payload.linkId, statusCode
    }

let getClubs timonService =
    async {
        let! resp = timonService.clubService.``get-clubs`` ()
        return ClubViewProvider.Parse resp
    }

let createClub (timonService, payload) =
    async { return! timonService.clubService.``create-club`` payload }

let getClubLinks (timonService, queryParams) =
    async {
        let! resp = timonService.linkService.``get-club-links`` queryParams
        return GetClubLinksResultProvider.Parse resp
    }

let getClubLinksByTag (timonService, queryParams: GetClubLinkByTagsParams) =
    async {
        let! resp = timonService.linkService.``get-club-links-by-tag`` queryParams
        return GetClubLinksResultProvider.Parse resp
    }
