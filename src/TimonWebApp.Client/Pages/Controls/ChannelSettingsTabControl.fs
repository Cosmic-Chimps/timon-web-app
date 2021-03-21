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
open TimonWebApp.Client.Dtos
open System
open TimonWebApp.Client.Pages.Controls.InputsHtml
open TimonWebApp.Client.Validation
open System.Net

type TabItem =
    { code: int
      tabClass: string
      iClass: string
      text: string }

type FollowForm = { Url: string }

type Model =
    { tabItems: TabItem array
      selectedTab: int
      clubId: ClubId
      channelId: ChannelId
      channelName: string
      channelActivityPubDetails: ChannelActivityPubDetailsView option
      channelFollowers: ChannelFollowsView array
      channelFollowings: ChannelFollowsView array
      errorsValidateFollowForm: Result<FollowForm, Map<string, string list>> option
      followForm: FollowForm }
    static member Default =
        { tabItems =
              [| { code = 0
                   tabClass = "is-active"
                   iClass = $"{Mdi.mdi} {Mdi.``mdi-details``}"
                   text = "General" }
                 { code = 1
                   tabClass = ""
                   iClass = "mdi mdi-apps"
                   text = "Apps" }
                 { code = 2
                   tabClass = ""
                   iClass = $"{Mdi.mdi} {Mdi.``mdi-account-arrow-right``}"
                   text = "Followers" }
                 { code = 3
                   tabClass = ""
                   iClass = $"{Mdi.mdi} {Mdi.``mdi-account-arrow-left``}"
                   text = "Following" } |]
          selectedTab = 0
          channelId = Guid.Empty
          channelName = String.Empty
          clubId = Guid.Empty
          channelActivityPubDetails = None
          channelFollowers = [||]
          channelFollowings = [||]
          errorsValidateFollowForm = None
          followForm = { Url = "" } }

type Message =
    | ChangeTab of int
    | SetChannel of ClubId * ChannelId * string
    | ResetModel
    | DismissConfirmation
    | LoadTabs
    | FollowersLoaded of ChannelFollowsView array
    | FollowingsLoaded of ChannelFollowsView array
    | ChannelDetailsLoaded of ChannelActivityPubDetailsView
    | SetFollowFormUrl of string
    | TryFollowUrl
    | FollowUrl
    | UrlFollowed of HttpStatusCode
    | CreateActivityPubId
    | ActivityPubIdCreated of HttpStatusCode

let validateFollowForm (followForm) =
    let validateUrl (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank(
            name
            + " cannot be blank"
        )
        |> validator.End

    all
    <| fun t -> { Url = validateUrl t "Url" followForm.Url }

let update (timonService: TimonService) model message =

    let validateFollowFormForced form =
        let mapResults = validateFollowForm form

        { model with
              followForm = form
              errorsValidateFollowForm = Some mapResults }

    let validateInputFollowUrl form =
        match model.errorsValidateFollowForm with
        | None -> { model with followForm = form }
        | Some _ -> validateFollowFormForced form

    match message with
    | ChangeTab tabCode ->
        let tabItems' =
            model.tabItems
            |> Seq.map
                (fun t ->
                    match tabCode = t.code with
                    | true -> { t with tabClass = "is-active" }
                    | false -> { t with tabClass = "" })
            |> Seq.toArray

        { model with
              tabItems = tabItems'
              selectedTab = tabCode },
        Cmd.none

    | SetChannel (clubId, channelId, channelName) ->

        { model with
              channelId = channelId
              clubId = clubId
              channelName = channelName },
        Cmd.ofMsg LoadTabs

    | ResetModel -> Model.Default, Cmd.none

    | LoadTabs ->
        let queryParams =
            { channelId = model.channelId
              clubId = model.clubId }

        let cmdFollowers =
            Cmd.OfAsync.either
                getChannelFollowers
                (timonService, queryParams)
                FollowersLoaded
                raise

        let cmdFollowings =
            Cmd.OfAsync.either
                getChannelFollowings
                (timonService, queryParams)
                FollowingsLoaded
                raise

        let cmdChannelActivityPubDetails =
            Cmd.OfAsync.either
                getChanneActivityPublDetails
                (timonService, queryParams)
                ChannelDetailsLoaded
                raise

        model,
        Cmd.batch [|
            cmdFollowers
            cmdFollowings
            cmdChannelActivityPubDetails
        |]

    | FollowersLoaded members ->
        { model with
              channelFollowers = members },
        Cmd.none

    | FollowingsLoaded members ->
        { model with
              channelFollowings = members },
        Cmd.none

    | ChannelDetailsLoaded channelDetailsParam ->
        { model with
              channelActivityPubDetails = Some channelDetailsParam },
        Cmd.none

    | SetFollowFormUrl url ->
        { model.followForm with
              Url = url.Trim() }
        |> validateInputFollowUrl,
        Cmd.none

    | TryFollowUrl _ ->
        model.followForm
        |> validateFollowFormForced,
        Cmd.ofMsg (FollowUrl)

    | FollowUrl _ ->
        let followPayload =
            { clubId = model.clubId
              channelId = model.channelId
              activityPubId = model.followForm.Url }

        let cmdFollowed =
            Cmd.OfAsync.either
                follow
                (timonService, followPayload)
                UrlFollowed
                raise

        model, cmdFollowed

    | UrlFollowed _ -> model, Cmd.ofMsg LoadTabs

    | CreateActivityPubId _ ->
        let getChannelDetails =
            { clubId = model.clubId
              channelId = model.channelId }

        let cmdCreateActivityPub =
            Cmd.OfAsync.either
                createActivityPubId
                (timonService, getChannelDetails)
                ActivityPubIdCreated
                raise

        model, cmdCreateActivityPub

    | ActivityPubIdCreated _ -> model, Cmd.ofMsg LoadTabs

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =

        let tabContent =
            let activityPubId =
                match model.channelActivityPubDetails with
                | Some x -> x.ActivityPubId
                | None -> ""

            let createActivityPubIdLink =
                a [ on.click (fun _ -> dispatch CreateActivityPubId) ] [
                    text "Create Activity Pub Id"
                ]

            match model.selectedTab with
            | 0 ->
                match activityPubId with
                | "" ->
                    ChannelSettingsTabControlsTemplate
                        .ChannelSettingTabContentGeneral()
                        .ActivityPubId(createActivityPubIdLink)
                        .Elt()
                | _ ->
                    ChannelSettingsTabControlsTemplate
                        .ChannelSettingTabContentGeneral()
                        .ActivityPubId(activityPubId)
                        .Elt()
            | 1 ->
                ChannelSettingsTabControlsTemplate
                    .ChannelSettingTabContentApps()
                    .Elt()
            | 2 ->
                match activityPubId with
                | "" ->
                    ChannelSettingsTabControlsTemplate
                        .ChannelSettingTabContentNoActivityPubId()
                        .CreateActivityPubIdAction(createActivityPubIdLink)
                        .Elt()
                | _ ->
                    let memberRow (channelMember: ChannelFollowsView) =
                        ChannelSettingsTabControlsTemplate
                            .RowMember()
                            .DisplayName(channelMember.Name)
                            .Elt()

                    ChannelSettingsTabControlsTemplate
                        .ChannelSettingTabContentFollowers()
                        .Members(forEach model.channelFollowers memberRow)
                        .Elt()
            | 3 ->
                match activityPubId with
                | "" ->
                    ChannelSettingsTabControlsTemplate
                        .ChannelSettingTabContentNoActivityPubId()
                        .CreateActivityPubIdAction(createActivityPubIdLink)
                        .Elt()
                | _ ->
                    let memberRow (channelMember: ChannelFollowsView) =
                        ChannelSettingsTabControlsTemplate
                            .RowMember()
                            .DisplayName(channelMember.Name)
                            .Elt()

                    let inputCallback = fun v -> dispatch (SetFollowFormUrl v)

                    let buttonAction = (fun _ -> dispatch (TryFollowUrl))

                    let followForm =
                        inputWithButton
                            "add_follow_id"
                            "Follow Url"
                            Mdi.``mdi-link-plus``
                            model.errorsValidateFollowForm
                            (Some "url")
                            "Url"
                            model.followForm.Url
                            inputCallback
                            buttonAction
                            "Follow"

                    ChannelSettingsTabControlsTemplate
                        .ChannelSettingTabContentFollowing()
                        .FollowForm(followForm)
                        .Members(forEach model.channelFollowings memberRow)
                        .Elt()


        let renderTabItems tabItem =
            ChannelSettingsTabControlsTemplate
                .ChannelSettingsTabHeader()
                .TabClass(tabItem.tabClass)
                .ChangeTab(fun _ -> dispatch (ChangeTab tabItem.code))
                .IClass(tabItem.iClass)
                .Text(tabItem.text)
                .Elt()

        ChannelSettingsTabControlsTemplate
            .ChannelSettingsTabControl()
            .TabHeaders(forEach model.tabItems renderTabItems)
            .TabContent(tabContent)
            .Elt()


let view (model: Model) dispatch =
    ecomp<Component, _, _> [] model dispatch
