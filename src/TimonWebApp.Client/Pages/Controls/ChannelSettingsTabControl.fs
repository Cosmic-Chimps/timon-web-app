module TimonWebApp.Client.Pages.Controls.ChannelSettingsTabControl

open Bolero
open Bolero.Html
open TimonWebApp.Client.Common
open Elmish
open TimonWebApp.Client.Services
open TimonWebApp.Client.ClubServices
open TimonWebApp.Client.AuthServices
open TimonWebApp.Client.LinkServices
open TimonWebApp.Client.ChannelServices
open TimonWebApp.Client.JsonProviders
open System

type TabItem =
    { code: int
      tabClass: string
      iClass: string
      text: string }

type Model =
    { tabItems: TabItem array
      selectedTab: int
      clubId: ClubId
      channelId: ChannelId
      channelName: string
      channelMembers: ChannelFollows array }
    static member Default =
        { tabItems =
              [| { code = 0
                   tabClass = "is-active"
                   iClass = "mdi mdi-details"
                   text = "General" }
                 { code = 1
                   tabClass = ""
                   iClass = "mdi mdi-apps"
                   text = "Apps" }
                 { code = 2
                   tabClass = ""
                   iClass = "mdi mdi-account-group"
                   text = "Members" } |]
          selectedTab = 0
          channelId = Guid.Empty
          channelName = String.Empty
          clubId = Guid.Empty
          channelMembers = [||] }

type Message =
    | ChangeTab of int
    | SetChannel of ClubId * ChannelId * string
    | ResetModel
    | DismissConfirmation
    | LoadFollowing
    | FollowingsLoaded of ChannelFollows array

let update (timonService: TimonService) model message =
    match message with
    | ChangeTab tabCode ->
        let tabItems' =
            model.tabItems
            |> Seq.map (fun t ->
                match tabCode = t.code with
                | true -> { t with tabClass = "is-active" }
                | false -> { t with tabClass = "" })
            |> Seq.toArray

        { model with
              tabItems = tabItems'
              selectedTab = tabCode },
        Cmd.none

    | SetChannel (clubId, channelId, channelName) ->

        { model with channelId = channelId; clubId = clubId; channelName = channelName }, Cmd.ofMsg LoadFollowing

    | ResetModel -> Model.Default, Cmd.none

    | LoadFollowing ->
        let queryParams = { channelId = model.channelId; clubId = model.clubId }

        let cmd =
            Cmd.OfAsync.either
                getChannelFollowings
                (timonService, queryParams)
                FollowingsLoaded
                raise

        model, cmd

    | FollowingsLoaded members -> { model with channelMembers = members }, Cmd.none

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =

        let tabContent =
            match model.selectedTab with
            | 0 -> ChannelSettingsTabControlsTemplate.ChannelSettingTabContentGeneral().Elt()
            | 1 -> ChannelSettingsTabControlsTemplate.ChannelSettingTabContentApps().Elt()
            | 2 ->
                let memberRow (channelMember: ChannelFollows) =
                    ChannelSettingsTabControlsTemplate.RowMember()
                                    .DisplayName(channelMember.Name).Elt()

                ChannelSettingsTabControlsTemplate.ChannelSettingTabContentMembers()
                                .Members(forEach model.channelMembers memberRow)
                                .Elt()


        let renderTabItems tabItem =
            ChannelSettingsTabControlsTemplate.ChannelSettingsTabHeader().TabClass(tabItem.tabClass)
                            .ChangeTab(fun _ -> dispatch (ChangeTab tabItem.code))
                            .IClass(tabItem.iClass).Text(tabItem.text).Elt()

        ChannelSettingsTabControlsTemplate.ChannelSettingsTabControl()
                        .TabHeaders(forEach model.tabItems renderTabItems)
                        .TabContent(tabContent).Elt()


let view (model: Model) dispatch = ecomp<Component, _, _> [] model dispatch
