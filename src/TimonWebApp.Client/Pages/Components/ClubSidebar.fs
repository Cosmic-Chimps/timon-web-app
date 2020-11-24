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
    errorsValidateForm: Result<ClubForm, Map<string, string list>> option }

  static member Default =
    { sidebarClass = ""
      isAddingClub = false
      clubForm = { name = ""}
      errorsValidateForm = None
      clubs = [||] }


type Message =
  | ToggleSidebarVisibility
  | ToggleClubForm
  | SetFormField of string * string
  | ValidateForm
  | AddClub
  | ClubAdded of HttpStatusCode
  | LoadClubs
  | ClubsLoaded of ClubView array
  | ChangeClub of Guid * string

let validateClubForm (clubForm) =
  let validateName (validator: Validator<string>) name value =
      validator.Test name value
      |> validator.NotBlank(name + " cannot be blank")
      |> validator.End

  all
  <| fun t -> { name = validateName t "Name" clubForm.name }


let update (jsRuntime: IJSRuntime) (timonService: TimonService) (message: Message) (model: Model) =
  let validateFormForced form =
    let mapResults = validateClubForm form
    { model with
          clubForm = form
          errorsValidateForm = Some mapResults }

  let validateForm form =
    match model.errorsValidateForm with
    | None -> { model with clubForm = form }
    | Some _ -> validateFormForced form

  match message, model  with
  | ToggleSidebarVisibility, _ ->
    let sidebarClass =
      match model.sidebarClass with
      | "" -> "show"
      | _ -> ""

    let cmd =
        match model.sidebarClass with
        | "" -> Cmd.ofMsg LoadClubs
        | _ -> Cmd.none

    { model with sidebarClass = sidebarClass }, cmd

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

  | ValidateForm, _ ->
    model.clubForm |> validateFormForced, Cmd.ofMsg (AddClub)

  | AddClub, _ ->
    let payload : CreateClubPayload = { name = model.clubForm.name }

    let cmd =
      Cmd.OfAsync.either createClub (timonService, payload) ClubAdded raise

    model, cmd

  | ClubAdded _, _ ->
      let clubForm = { model.clubForm with name = ""}
      { model with isAddingClub = false; clubForm = clubForm } , Cmd.ofMsg LoadClubs

  | LoadClubs, _ ->
      let cmd =
        Cmd.OfAsync.either getClubs (timonService) ClubsLoaded raise

      model, cmd

  | ClubsLoaded clubs, _ ->
      { model with clubs = clubs }, Cmd.none

  | ChangeClub _, _ ->
    { model with sidebarClass = "" }, Cmd.none

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
            (formFieldItem "Name" model.clubForm.name inputCallback buttonAction,
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
          |> Seq.filter(fun c -> c.IsPublic )

      let privateClubs =
          model.clubs
          |> Seq.filter(fun c -> not c.IsPublic )

      let showClub (club: ClubView) =
          ComponentsTemplate
            .ClubLink()
            .Name(club.Name)
            .ChangeClub(fun c -> dispatch (ChangeClub (club.Id, club.Name)))
            .Elt()

      ComponentsTemplate
        .ClubSidebar()
        .SidebarClass(model.sidebarClass)
        .ToggleVisibility(fun _ -> (dispatch ToggleSidebarVisibility))
        .Icon(icon)
        .ClubInput(inputBox)
        .PublicClubs(forEach publicClubs showClub)
        .PrivateClubs(forEach privateClubs showClub)
        .Elt()

let view (model: Model) dispatch =
  ecomp<Component, _, _>
    []
    model
    dispatch
