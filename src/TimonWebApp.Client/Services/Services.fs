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
      tagName: string }

[<JsonFSharpConverter>]
type AddTagPayload = { linkId: string; tags: string }

[<JsonFSharpConverter>]
type GetLinkParams = { channelId: Guid; page: int }

[<JsonFSharpConverter>]
type GetLinkByTagsParams = { tagName: string; page: int }

[<JsonFSharpConverter>]
type DeleteTagFromLink = { linkId: Guid; tagName: string }

[<JsonFSharpConverter>]
type GetLinkSearchParams = { term: string; page: int }

type LinkService =
    { ``get-links``: GetLinkParams -> Async<string>
      ``create-link``: CreateLinkPayload -> Async<HttpStatusCode>
      ``add-tags``: AddTagPayload -> Async<HttpStatusCode>
      ``get-links-by-tag``: GetLinkByTagsParams -> Async<string>
      ``delete-tag-from-link``: DeleteTagFromLink -> Async<HttpStatusCode>
      ``search-links``: GetLinkSearchParams -> Async<string> }

    interface IRemoteService with
        member this.BasePath = "/links"

[<JsonFSharpConverter>]
type CreateChannelPayload = { name: string }

type ChannelService =
    { ``get-channels``: unit -> Async<string>
      ``create-channel``: CreateChannelPayload -> Async<HttpStatusCode> }
    interface IRemoteService with
        member this.BasePath = "/channels"

type TimonService =
    { LinkService: LinkService
      AuthService: AuthService
      ChannelService: ChannelService }

let logIn (timonService, loginRequest) =
    async {
        let! resp = timonService.AuthService.``sign-in`` loginRequest
        return Some(resp)
    }

let signUp (timonService, signUpRequest) =
    async {
        let! resp = timonService.AuthService.``sign-up`` signUpRequest
        return Some(resp)
    }

#if DEBUG
type GetLinksResultProvider = JsonProvider<"http://localhost:5011/.meta/v3/get/links">
type ChannelViewProvider = JsonProvider<"http://localhost:5011/.meta/v3/get/channels">
#else
type GetLinksResultProvider = JsonProvider<"http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/.meta/v3/get/links">
type ChannelViewProvider = JsonProvider<"http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/.meta/get/v3/channels">
#endif
type GetLinksResult = GetLinksResultProvider.Root
type ChannelView = ChannelViewProvider.Root

let getLinks (timonService, queryParams) =
    async {
        let! resp = timonService.LinkService.``get-links`` queryParams
        return GetLinksResultProvider.Parse resp
    }

let getLinksByTag (timonService, queryParams) =
    async {
        let! resp = timonService.LinkService.``get-links-by-tag`` queryParams
        return GetLinksResultProvider.Parse resp
    }

let searchLinks (timonService, queryParams) =
    async {
        let! resp = timonService.LinkService.``search-links`` queryParams
        return GetLinksResultProvider.Parse resp
    }

let createLink (timonService, payload) =
    async { return! timonService.LinkService.``create-link`` payload }

let deleteTagFromLink (timonService, payload) =
    async {
        let! statusCode = timonService.LinkService.``delete-tag-from-link`` payload
        return payload.linkId, statusCode
    }

let getChannels timonService =
    async {
        let! resp = timonService.ChannelService.``get-channels`` ()
        return ChannelViewProvider.Parse resp
    }

let createChannel (timonService, payload) =
    async { return! timonService.ChannelService.``create-channel`` payload }

let addTags (timonService, payload) =
    async {
        let! statusCode = timonService.LinkService.``add-tags`` payload
        return Guid.Parse payload.linkId, statusCode
    }
