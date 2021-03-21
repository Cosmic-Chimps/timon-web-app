module TimonWebApp.Client.LinkServices

open System
open System.Net
open System.Text.Json.Serialization
open Common
open FSharp.Data
open Bolero.Remoting
open Dtos

// Request
[<JsonFSharpConverter>]
type CreateLinkPayload =
    { url: string
      channelId: string
      via: string
      tagName: string
      clubId: ClubId }

[<JsonFSharpConverter>]
type AddTagPayload =
    { clubId: ClubId
      linkId: string
      tags: string }


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
type GetClubLinkByTagsParams =
    { clubId: ClubId
      tagName: string
      page: int }

[<JsonFSharpConverter>]
type DeleteTagFromLink =
    { clubId: ClubId
      linkId: Guid
      tagName: string }

[<JsonFSharpConverter>]
type GetClubLinkSearchParams =
    { clubId: ClubId
      term: string
      page: int }

// Respone
// type GetLinksResult = GetLinksResultProvider.Root
// type ClubListView = GetClubLinksResultProvider.Root

type LinkService =
    { ``get-links``: GetLinkParams -> Async<AnonymousLinkView>
      ``get-club-links``: GetClubLinkParams -> Async<AuthLinkView>
      ``create-link``: CreateLinkPayload -> Async<HttpStatusCode>
      ``add-tags``: AddTagPayload -> Async<HttpStatusCode>
      ``get-links-by-tag``: GetLinkByTagsParams -> Async<AnonymousLinkView>
      ``get-club-links-by-tag``: GetClubLinkByTagsParams -> Async<AuthLinkView>
      ``delete-tag-from-link``: DeleteTagFromLink -> Async<HttpStatusCode>
      ``search-club-links``: GetClubLinkSearchParams -> Async<AuthLinkView> }

    interface IRemoteService with
        member this.BasePath = "/links"
