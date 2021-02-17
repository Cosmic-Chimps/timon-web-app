module TimonWebApp.Client.JsonProviders

open FSharp.Data

#if DEBUG

// LinkServices
type GetLinksResultProvider =
    JsonProvider<"http://localhost:5004/.meta/v14/get/links">

type GetClubLinksResultProvider =
    JsonProvider<"http://localhost:5004/.meta/v14/get/clubs/links">

// ClubServices
type ClubViewProvider =
    JsonProvider<"http://localhost:5004/.meta/v14/get/clubs">

type GetClubMembersResultProvider =
    JsonProvider<"http://localhost:5004/.meta/v14/get/clubs/members">

// ChannelServices

type ChannelViewProvider =
    JsonProvider<"http://localhost:5004/.meta/v14/get/channels">

type GetChannelFollowResultProvider =
    JsonProvider<"http://localhost:5004/.meta/v14/get/channels/follow">


#else


#endif
