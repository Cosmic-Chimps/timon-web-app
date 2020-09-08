module TimonWebApp.Client.Services

open System
open System.Net
open System.Text.Json.Serialization
open Common
open FSharp.Data
open Bolero.Remoting

[<JsonFSharpConverter>]
type LoginRequest = {
    email: string
    password: string
}

[<JsonFSharpConverter>]
type SignUpRequest = {
    userName: string
    firstName: string
    lastName: string
    email: string
    password: string
    confirmPassword: string
}

type AuthService =
    {
        /// Sign into the application.
        ``sign-in`` : LoginRequest -> Async<string>

        ``sign-up``: SignUpRequest -> Async<string>

        /// Get the user's name, or None if they are not authenticated.
        ``get-user-name`` : unit -> Async<string>

        /// Sign out from the application.
        ``sign-out`` : unit -> Async<unit>

        /// Sign out from the application.
        ``get-config`` : unit -> Async<TimonConfiguration>
    }

    interface IRemoteService with
        member this.BasePath = "/auth"

[<JsonFSharpConverter>]
type CreateLinkPayload = {
    url: string
    channelId: string
    via: string
}

[<JsonFSharpConverter>]
type AddTagPayload = {
    linkId: string
    tags: string
}

[<JsonFSharpConverter>]
type GetLinkParams = {
    channelId: Guid
}
type LinkService =
    {
        ``get-links`` : GetLinkParams -> Async<string>
        ``create-link``: CreateLinkPayload -> Async<HttpStatusCode>
        ``add-tags``: AddTagPayload -> Async<HttpStatusCode>
    }

    interface IRemoteService with
        member this.BasePath = "/links"

[<JsonFSharpConverter>]
type CreateChannelPayload = {
    name: string
}

type ChannelService =
    {
        ``get-channels``: unit -> Async<string>
        ``create-channel``: CreateChannelPayload -> Async<HttpStatusCode>
    }
    interface IRemoteService with
        member this.BasePath = "/channels"

type TimonService = {
     LinkService: LinkService
     AuthService: AuthService
     ChannelService: ChannelService
}

let logIn (timonService,loginRequest) =
    async {
        let! resp = timonService.AuthService.``sign-in`` loginRequest
        return Some(resp)
    }

let signUp (timonService,signUpRequest) =
    async {
        let! resp = timonService.AuthService.``sign-up`` signUpRequest
        return Some(resp)
    }

#if DEBUG
type LinkViewProvider = JsonProvider<"https://localhost:5010/.meta/get/links">
type ChannelViewProvider = JsonProvider<"http://localhost:5011/.meta/get/channels">
#else
type LinkViewProvider = JsonProvider<"http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/.meta/get/links">
type ChannelViewProvider = JsonProvider<"http://timon-api-gateway-openfaas-fn.127.0.0.1.nip.io/.meta/get/channels">
#endif
type LinkView = LinkViewProvider.Root
type ChannelView = ChannelViewProvider.Root

let getLinks (timonService, queryParams) =
    async {
        let! resp = timonService.LinkService.``get-links`` queryParams
        return LinkViewProvider.Parse resp
    }

let createLink (timonService, payload) =
    async {
        return! timonService.LinkService.``create-link`` payload
    }

let getChannels timonService =
    async {
        let! resp = timonService.ChannelService.``get-channels``()
        return ChannelViewProvider.Parse resp
    }

let createChannel (timonService, payload) =
    async {
        return! timonService.ChannelService.``create-channel`` payload
    }

let addTags (timonService, payload) =
    async {
        let! statusCode = timonService.LinkService.``add-tags`` payload
        return Guid.Parse payload.linkId, statusCode
    }