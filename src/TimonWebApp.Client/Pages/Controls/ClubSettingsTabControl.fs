module TimonWebApp.Client.Pages.Controls.ClubSettingsTabControl

open Bolero
open Bolero.Html
open TimonWebApp.Client.Common
open Elmish
open TimonWebApp.Client.Services

type TabItem =
    { code: int
      tabClass: string
      iClass: string
      text: string }

type Model =
    { tabItems: TabItem array
      selectedTab: int
      clubView: ClubView option
      showWarningLeavingProtectedClub: bool
      clubMembers: ClubMember array }
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
                   text = "Members" }
                 { code = 3
                   tabClass = ""
                   iClass = "mdi mdi-alert"
                   text = "Danger" } |]
          selectedTab = 0
          clubView = None
          showWarningLeavingProtectedClub = false
          clubMembers = [||] }

type Message =
    | ChangeTab of int
    | LeaveClub of ClubView
    | SetClub of ClubView
    | BeforeLeaveClub of ClubView
    | ResetModel
    | DismissConfirmation
    | ForceLeave
    | LoadMembers
    | MembersLoaded of ClubMember array

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

    | BeforeLeaveClub clubView ->
        match clubView.IsPublic with
        | true ->
            let leaveCmd = Cmd.ofMsg (LeaveClub clubView)
            model, leaveCmd
        | false ->
            { model with
                  showWarningLeavingProtectedClub = true },
            Cmd.none

    | LeaveClub _ -> model, Cmd.none

    | SetClub clubView ->

        { model with clubView = Some clubView }, Cmd.ofMsg LoadMembers

    | ResetModel -> Model.Default, Cmd.none

    | DismissConfirmation ->
        { model with
              showWarningLeavingProtectedClub = false },
        Cmd.none

    | ForceLeave -> model, Cmd.ofMsg (LeaveClub model.clubView.Value)

    | LoadMembers ->
        let queryParams = { clubId = model.clubView.Value.Id }

        let cmd =
            Cmd.OfAsync.either
                getMembers
                (timonService, queryParams)
                MembersLoaded
                raise

        model, cmd

    | MembersLoaded members -> { model with clubMembers = members }, Cmd.none

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =

        let tabContent =
            match model.selectedTab with
            | 0 -> ControlsTemplate.ClubSettingTabContentGeneral().Elt()
            | 1 -> ControlsTemplate.ClubSettingTabContentApps().Elt()
            | 2 ->
                let memberRow (clubMember: ClubMember) =
                    ControlsTemplate.RowMember()
                                    .DisplayName(clubMember.DisplayName).Elt()

                ControlsTemplate.ClubSettingTabContentMembers()
                                .Members(forEach model.clubMembers memberRow)
                                .Elt()
            | _ ->
                let confirmLeaveProtectedClub =
                    match model.showWarningLeavingProtectedClub with
                    | false -> empty
                    | true ->
                        ControlsTemplate.ConfirmLeaveProtectedClub()
                                        .ClubName(model.clubView.Value.Name)
                                        .DismissConfirmation(fun _ ->
                                        dispatch DismissConfirmation)
                                        .ForceLeave(fun _ -> dispatch ForceLeave)
                                        .Elt()

                ControlsTemplate.ClubSettingTabContentDangerZone()
                                .LeaveClub(fun _ ->
                                dispatch (BeforeLeaveClub model.clubView.Value))
                                .ConfirmLeaveProtectedClub(confirmLeaveProtectedClub)
                                .Elt()


        let renderTabItems tabItem =
            ControlsTemplate.ClubSettingsTabHeader().TabClass(tabItem.tabClass)
                            .ChangeTab(fun _ ->
                            dispatch (ChangeTab tabItem.code))
                            .IClass(tabItem.iClass).Text(tabItem.text).Elt()

        ControlsTemplate.ClubSettingsTabControl()
                        .TabHeaders(forEach model.tabItems renderTabItems)
                        .TabContent(tabContent).Elt()


let view (model: Model) dispatch = ecomp<Component, _, _> [] model dispatch
