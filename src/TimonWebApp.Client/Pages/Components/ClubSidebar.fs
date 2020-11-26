module TimonWebApp.Client.Pages.Components.ClubSidebar

open Bolero
open Elmish
open TimonWebApp.Client.Services
open TimonWebApp.Client.Common
open Bolero.Html
open Microsoft.JSInterop
open TimonWebApp.Client.Pages
open TimonWebApp.Client.Validation
open System.Net
open System

type ClubForm = { name: string }

type Model =
    { sidebarClass: string
      isAddingClub: bool
      clubForm: ClubForm
      clubs: ClubView array
      otherClubs: ClubView array
      activeClubId: Guid
      errorsValidateForm: Result<ClubForm, Map<string, string list>> option
      isShowingWarningUnsubscribePrivateClub: bool
      clubFromUnsubscribe: ClubView option }

    static member Default =
        { sidebarClass = ""
          isAddingClub = false
          clubForm = { name = "" }
          errorsValidateForm = None
          clubs = [||]
          otherClubs = [||]
          activeClubId = Guid.Empty
          isShowingWarningUnsubscribePrivateClub = false
          clubFromUnsubscribe = None }


type Message =
    | ToggleSidebarVisibility
    | ToggleClubForm
    | SetFormField of string * string
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
    | UnSubscribeClubVerifyIsPrivate of ClubView
    | DismissModal
    | ForceUnsubscribe
    | NoClubs

let validateClubForm (clubForm) =
    let validateName (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank
            (name
             + " cannot be blank")
        |> validator.End

    all
    <| fun t -> { name = validateName t "Name" clubForm.name }


let update (jsRuntime: IJSRuntime)
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

    | ValidateForm, _ ->
        model.clubForm
        |> validateFormForced,
        Cmd.ofMsg (AddClub)

    | AddClub, _ ->
        let payload: CreateClubPayload = { name = model.clubForm.name }

        let cmd =
            Cmd.OfAsync.either
                createClub
                (timonService, payload)
                ClubAdded
                raise

        model, cmd

    | ClubAdded _, _ ->
        let clubForm = { model.clubForm with name = "" }
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
        let payload: SubscribeClubPayload =
            { id = clubView.Id
              name = clubView.Name }

        let cmd =
            Cmd.OfAsync.either
                subscribeClub
                (timonService, payload)
                ClubSubscribed
                raise

        model, cmd

    | ClubSubscribed _, _ -> model, Cmd.ofMsg LoadClubs

    | UnSubscribeClubVerifyIsPrivate clubView, _ ->
        match clubView.IsPublic with
        | true ->
            let unsubscribeCmd = Cmd.ofMsg (UnSubscribeClub clubView)
            model, unsubscribeCmd
        | false ->
            { model with
                  isShowingWarningUnsubscribePrivateClub = true
                  clubFromUnsubscribe = Some clubView },
            Cmd.none

    | UnSubscribeClub clubView, _ ->
        let payload: UnSubscribeClubPayload =
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

    | ClubUnSubscribed _, _ -> model, Cmd.ofMsg LoadClubs

    | SetActiveClubId clubId, _ ->
        { model with activeClubId = clubId }, Cmd.none

    | DismissModal, _ ->
        { model with
              isShowingWarningUnsubscribePrivateClub = false
              clubFromUnsubscribe = None },
        Cmd.none

    | ForceUnsubscribe, _ ->
        let unsubscribeCmd =
            match model.clubFromUnsubscribe with
            | None -> Cmd.none
            | Some c -> Cmd.ofMsg (UnSubscribeClub c)

        { model with
              isShowingWarningUnsubscribePrivateClub = false
              clubFromUnsubscribe = None },
        unsubscribeCmd
    | NoClubs, _ -> model, Cmd.none

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =

        let formFieldItem =
            Controls.inputAdd
                "club_new_input"
                "Add new club"
                Mdi.``mdi-tree``
                model.errorsValidateForm
                (Some "name")

        let inputCallback =
            fun v -> dispatch (SetFormField("name", v))

        let buttonAction = (fun _ -> dispatch ValidateForm)

        let inputBox, mdiIcon =
            match model.isAddingClub with
            | true ->
                (formFieldItem
                    "Name"
                     model.clubForm.name
                     inputCallback
                     buttonAction,
                 Mdi.``mdi-minus-circle-outline``)
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

            ComponentsTemplate.ClubLink().Name(club.Name)
                              .ChangeClub(fun c -> dispatch (ChangeClub(club)))
                              .ActiveClass(mainDivClass)
                              .UnSubscribeFromClub(fun c ->
                              dispatch (UnSubscribeClubVerifyIsPrivate(club)))
                              .Elt()

        let subscribeOtherClub (club: ClubView) =
            ComponentsTemplate.OtherClubLink().Name(club.Name)
                              .SubscribeToClub(fun c ->
                              dispatch (SubscribeClub(club))).Elt()

        let modalActiveClass =
            match model.isShowingWarningUnsubscribePrivateClub with
            | true -> "is-active"
            | _ -> ""

        let clubFromUnsubscribeName =
            match model.clubFromUnsubscribe with
            | Some c -> c.Name
            | None -> ""

        ComponentsTemplate.ClubSidebar().SidebarClass(model.sidebarClass)
                          .ToggleVisibility(fun _ ->
                          (dispatch ToggleSidebarVisibility)).Icon(icon)
                          .ClubInput(inputBox)
                          .PublicClubs(forEach publicClubs showClub)
                          .PrivateClubs(forEach privateClubs showClub)
                          .ModalActiveClass(modalActiveClass)
                          .DismissModal(fun _ -> dispatch DismissModal)
                          .ForceUnsubscribe(fun c -> dispatch ForceUnsubscribe)
                          .ModalClubNameUnsubscribe(clubFromUnsubscribeName)
                          .OtherClubs(forEach
                                          model.otherClubs
                                          subscribeOtherClub).Elt()

let view (model: Model) dispatch = ecomp<Component, _, _> [] model dispatch
