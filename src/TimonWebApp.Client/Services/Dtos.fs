module TimonWebApp.Client.Dtos

open FSharp.Data
open System

#if DEBUG

// LinkServices
// type GetLinksResultProvider =
//     JsonProvider<"http://localhost:5004/.meta/v15/get/links">

// type GetClubLinksResultProvider =
//     JsonProvider<"http://localhost:5004/.meta/v15/get/clubs/links">

// // ClubServices
// type ClubViewProvider =
//     JsonProvider<"http://localhost:5004/.meta/v15/get/clubs">

// type GetClubMembersResultProvider =
//     JsonProvider<"http://localhost:5004/.meta/v15/get/clubs/members">

// // ChannelServices

// type ChannelViewProvider =
//     JsonProvider<"http://localhost:5004/.meta/v15/get/channels">

// type GetChannelFollowResultProvider =
//     JsonProvider<"http://localhost:5004/.meta/v15/get/channels/follow">


#else


#endif

type ChannelView =
    { Id: Guid
      Name: string
      ActivityPubId: string }

type ChannelLinkView = { Id: Guid; Name: string }

type ChannelFollowsView = { Name: string }

type ChannelActivityPubDetailsView = { ActivityPubId: string }

type LinkView =
    { Id: Guid
      Title: string
      Url: string
      ShortDescription: string
      DomainName: string
      Tags: string
      DateCreated: DateTime }

type ChannePropertieslLinkView =
    { DateCreated: DateTime
      UpVotes: int
      DownVotes: int
      Via: string
      Tags: string }

type UserView = { DisplayName: string }

type TagView = { Name: string }

type ClubLinkView =
    { Link: LinkView
      Channel: ChannelLinkView
      Data: ChannePropertieslLinkView
      User: UserView
      CustomTags: string }

type ClubMembersView = { DisplayName: string }

type ClubView =
    { Id: Guid
      Name: string
      IsPublic: bool }

type AnonymousLinkView =
    { Links: LinkView list
      Page: int
      ShowNext: bool }

type AuthLinkView =
    { Links: ClubLinkView list
      Page: int
      ShowNext: bool }
