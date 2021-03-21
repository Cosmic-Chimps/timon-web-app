module TimonWebApp.Client.Pages.Components.ClubSidebar

open Bolero
open Elmish
open TimonWebApp.Client.Services
open TimonWebApp.Client.Common
open Bolero.Html
open Microsoft.JSInterop
open TimonWebApp.Client.Pages.Controls
open TimonWebApp.Client.Pages.Controls.InputsHtml
open TimonWebApp.Client.Validation
open System.Net
open System
open TimonWebApp.Client.ClubServices
open TimonWebApp.Client.AuthServices
open TimonWebApp.Client.LinkServices
open TimonWebApp.Client.ChannelServices
open TimonWebApp.Client.Dtos


type ClubForm = { name: string; isProtected: bool }

type Model =
    { sidebarClass: string
      isAddingClub: bool
      clubForm: ClubForm
      clubs: ClubView array
      otherClubs: ClubView array
      activeClubId: Guid
      errorsValidateForm: Result<ClubForm, Map<string, string list>> option
      clubToSubscribe: ClubView option
      clubSettings: ClubView option
      clubSettingsTabControlModel: ClubSettingsTabControl.Model }

    static member Default =
        { sidebarClass = ""
          isAddingClub = false
          clubForm = { name = ""; isProtected = false }
          errorsValidateForm = None
          clubs = [||]
          otherClubs = [||]
          activeClubId = Guid.Empty
          clubToSubscribe = None
          clubSettings = None
          clubSettingsTabControlModel = ClubSettingsTabControl.Model.Default }

type Message =
    | ToggleSidebarVisibility
    | ToggleClubForm
    | SetFormField of string * string
    | SetFormIsProtected of bool
    | ValidateForm
    | AddClub
    | ClubAdded of HttpStatusCode
    | LoadClubs
    | ClubsLoaded of ClubView array
    | OtherClubsLoaded of ClubView array
    | ChangeClub of ClubView
    | SubscribeClub of ClubView
    | ClubSubscribed of HttpStatusCode
    | UnSubscribeClub of ClubView
    | ClubUnSubscribed of HttpStatusCode
    | SetActiveClubId of Guid
    | DismissSettingsModal
    | NoClubs
    | OpenClubSettings of ClubView
    | ClubSettingsTabControlMsg of ClubSettingsTabControl.Message

let validateClubForm (clubForm) =
    let validateName (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank(
            name
            + " cannot be blank"
        )
        |> validator.End

    all
    <| fun t ->
        { name = validateName t "Name" clubForm.name
          isProtected = clubForm.isProtected }


let update
    (jsRuntime: IJSRuntime)
    (timonService: TimonService)
    (message: Message)
    (model: Model)
    =
    let validateFormForced form =
        let mapResults = validateClubForm form

        { model with
              clubForm = form
              errorsValidateForm = Some mapResults }

    let validateForm form =
        match model.errorsValidateForm with
        | None -> { model with clubForm = form }
        | Some _ -> validateFormForced form

    match message, model with
    | ToggleSidebarVisibility, _ ->
        let sidebarClass =
            match model.sidebarClass with
            | "" -> "show"
            | _ -> ""

        let cmd =
            match model.sidebarClass with
            | "" -> Cmd.ofMsg LoadClubs
            | _ -> Cmd.none

        { model with
              sidebarClass = sidebarClass },
        cmd

    | ToggleClubForm, _ ->
        jsRuntime.InvokeVoidAsync("jsTimon.focusElement", "club_new_input")
        |> ignore

        { model with
              isAddingClub = not model.isAddingClub },
        Cmd.none

    | SetFormField ("name", value), _ ->
        { model.clubForm with
              name = value.Trim() }
        |> validateForm,
        Cmd.none

    | SetFormField (_, value), _ -> model, Cmd.none

    | SetFormIsProtected value, _ ->
        let clubForm =
            { model.clubForm with
                  isProtected = value }

        { model with clubForm = clubForm }, Cmd.none

    | ValidateForm, _ ->
        model.clubForm
        |> validateFormForced,
        Cmd.ofMsg (AddClub)

    | AddClub, _ ->
        let payload : CreateClubPayload =
            { name = model.clubForm.name
              isProtected = model.clubForm.isProtected }

        let cmd =
            Cmd.OfAsync.either
                createClub
                (timonService, payload)
                ClubAdded
                raise

        model, cmd

    | ClubAdded _, _ ->
        let clubForm =
            { model.clubForm with
                  name = ""
                  isProtected = false }

        { model with
              isAddingClub = false
              clubForm = clubForm },
        Cmd.ofMsg LoadClubs

    | LoadClubs, _ ->
        let cmd =
            Cmd.OfAsync.either getClubs (timonService) ClubsLoaded raise

        let cmdOtherClubs =
            Cmd.OfAsync.either
                getOtherClubs
                (timonService)
                OtherClubsLoaded
                raise

        model, Cmd.batch [ cmd; cmdOtherClubs ]

    | ClubsLoaded clubs, _ -> { model with clubs = clubs }, Cmd.none

    | OtherClubsLoaded clubs, _ -> { model with otherClubs = clubs }, Cmd.none

    | ChangeClub clubView, _ ->
        { model with
              sidebarClass = ""
              activeClubId = clubView.Id },
        Cmd.none

    | SubscribeClub clubView, _ ->
        let payload : SubscribeClubPayload =
            { id = clubView.Id
              name = clubView.Name }

        let cmd =
            Cmd.OfAsync.either
                subscribeClub
                (timonService, payload)
                ClubSubscribed
                raise

        { model with
              clubToSubscribe = Some clubView },
        cmd

    | ClubSubscribed _, _ ->
        let changeClubCmd =
            match model.clubToSubscribe with
            | None -> Cmd.none
            | Some clubView -> Cmd.ofMsg (ChangeClub clubView)

        let cmds = [| Cmd.ofMsg LoadClubs; changeClubCmd |]

        { model with clubToSubscribe = None }, Cmd.batch cmds

    | UnSubscribeClub clubView, _ ->
        let payload : UnSubscribeClubPayload =
            { id = clubView.Id
              name = clubView.Name }

        let cmd =
            Cmd.OfAsync.either
                unsubscribeClub
                (timonService, payload)
                ClubUnSubscribed
                raise

        let changeClubCommand =
            match clubView.Id = model.activeClubId with
            | false -> Cmd.none
            | true ->
                let newClubView =
                    model.clubs
                    |> Seq.tryFind (fun c -> c.Id <> clubView.Id)

                let changeClubMessage =
                    match newClubView with
                    | Some c -> ChangeClub c
                    | None -> NoClubs

                Cmd.ofMsg changeClubMessage

        let cmds = [| cmd; changeClubCommand |]

        model, Cmd.batch cmds

    | ClubUnSubscribed _, _ ->
        let dismissCmd = Cmd.ofMsg DismissSettingsModal
        let loadClubsCmd = Cmd.ofMsg LoadClubs
        model, Cmd.batch [| dismissCmd; loadClubsCmd |]

    | SetActiveClubId clubId, _ ->
        { model with activeClubId = clubId }, Cmd.none

    | DismissSettingsModal, _ ->
        let clubSettingsTabControlMsg =
            ClubSettingsTabControl.Message.ResetModel

        let clubSettingsTabControlModel, _ =
            ClubSettingsTabControl.update
                timonService
                model.clubSettingsTabControlModel
                clubSettingsTabControlMsg

        { model with
              clubSettings = None
              clubSettingsTabControlModel = clubSettingsTabControlModel },
        Cmd.none

    | NoClubs, _ -> model, Cmd.none

    | OpenClubSettings clubView, _ ->
        let clubSettingsTabControlMsg =
            ClubSettingsTabControl.Message.SetClub clubView

        let clubSettingsTabControlModel, cmd =
            ClubSettingsTabControl.update
                timonService
                model.clubSettingsTabControlModel
                clubSettingsTabControlMsg

        { model with
              clubSettings = Some clubView
              clubSettingsTabControlModel = clubSettingsTabControlModel },
        Cmd.map ClubSettingsTabControlMsg cmd

    | ClubSettingsTabControlMsg (ClubSettingsTabControl.Message.LeaveClub clubView),
      _ ->
        let unsubscribeCmd = Cmd.ofMsg (UnSubscribeClub clubView)

        { model with clubSettings = None }, unsubscribeCmd

    | ClubSettingsTabControlMsg msg, _ ->
        let clubSettingsTabModal, cmd =
            ClubSettingsTabControl.update
                timonService
                model.clubSettingsTabControlModel
                msg

        { model with
              clubSettingsTabControlModel = clubSettingsTabModal },
        Cmd.map ClubSettingsTabControlMsg cmd

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =

        let formFieldItem =
            inputWithButton
                "club_new_input"
                "Add new club"
                Mdi.``mdi-tree``
                model.errorsValidateForm
                (Some "name")

        let inputCallback =
            fun v -> dispatch (SetFormField("name", v))

        let buttonAction = (fun _ -> dispatch ValidateForm)

        let clubForm, mdiIcon =
            match model.isAddingClub with
            | true ->
                let inputBox =
                    formFieldItem
                        "Name"
                        model.clubForm.name
                        inputCallback
                        buttonAction
                        "Add"

                let form =
                    ComponentsTemplate
                        .ClubForm()
                        .ClubInput(inputBox)
                        .ClubFormIsProtected(
                            model.clubForm.isProtected,
                            (fun n -> dispatch (SetFormIsProtected n))
                        )
                        .Elt()

                (form, Mdi.``mdi-minus-circle-outline``)

            | false -> (empty, Mdi.``mdi-plus-circle-outline``)

        let icon =
            a [ attr.``class`` Bulma.``has-text-grey``
                on.click (fun _ -> dispatch ToggleClubForm) ] [
                i [ attr.``class``
                    <| String.concat " " [ "mdi"; mdiIcon ] ] []
            ]

        let publicClubs =
            model.clubs
            |> Seq.filter (fun c -> c.IsPublic)

        let privateClubs =
            model.clubs
            |> Seq.filter (fun c -> not c.IsPublic)

        let showClub (club: ClubView) =
            let mainDivClass =
                match club.Id = model.activeClubId with
                | true -> "is-active"
                | _ -> ""

            ComponentsTemplate
                .ClubLink()
                .Name(club.Name)
                .ChangeClub(fun c -> dispatch (ChangeClub(club)))
                .ActiveClass(mainDivClass)
                .OpenClubSettings(fun c -> dispatch (OpenClubSettings(club)))
                .Elt()

        let subscribeOtherClub (club: ClubView) =
            ComponentsTemplate
                .OtherClubLink()
                .Name(club.Name)
                .SubscribeToClub(fun c -> dispatch (SubscribeClub(club)))
                .Elt()

        let clubSettingsModal =
            match model.clubSettings with
            | None -> empty
            | Some c ->
                let tabControl =
                    ClubSettingsTabControl.view
                        model.clubSettingsTabControlModel
                        (ClubSettingsTabControlMsg
                         >> dispatch)

                ComponentsTemplate
                    .ClubSettingsModal()
                    .ClubName(c.Name)
                    .DismissModal(fun _ -> dispatch DismissSettingsModal)
                    .ClubSettingsTabControl(tabControl)
                    .Elt()


        ComponentsTemplate
            .ClubSidebar()
            .SidebarClass(model.sidebarClass)
            .ToggleVisibility(fun _ -> (dispatch ToggleSidebarVisibility))
            .Icon(icon)
            .ClubForm(clubForm)
            .PublicClubs(forEach publicClubs showClub)
            .PrivateClubs(forEach privateClubs showClub)
            .ClubSettingsModal(clubSettingsModal)
            .OtherClubs(forEach model.otherClubs subscribeOtherClub)
            .Elt()

let view (model: Model) dispatch =
    ecomp<Component, _, _> [] model dispatch
