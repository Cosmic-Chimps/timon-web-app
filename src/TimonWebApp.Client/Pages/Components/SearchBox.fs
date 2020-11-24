module TimonWebApp.Client.Pages.Components.SearchBox

open System
open System.ComponentModel.Design
open System.Net
open Bolero
open Elmish
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages.Controls
open TimonWebApp.Client.Services
open TimonWebApp.Client.Validation
open Bolero.Html
open Microsoft.JSInterop

type Model =
    {
      inputSearchBoxModel: InputSearchBox.Model
      term: string
      authState: AuthState
      clubName: string
    }
    static member Default = {
      inputSearchBoxModel = InputSearchBox.Model.Default
      authState = AuthState.NotTried
      term = String.Empty
      clubName = String.Empty
    }

type Message =
  | InputSearchBoxMsg of InputSearchBox.Message
  | LoadSearch of string * int
  | UpdateInputSearchBox of string
  | ToggleSidebarVisibility

let update (timonService: TimonService) (message: Message) (model: Model) =
  match message, model with
  | InputSearchBoxMsg (InputSearchBox.Message.Search (term)), _ ->
    { model with term = term }, Cmd.ofMsg (LoadSearch(term, 0))

  | InputSearchBoxMsg msg, _ ->
    let m, cmd =
        InputSearchBox.update timonService msg model.inputSearchBoxModel

    { model with inputSearchBoxModel = m }, Cmd.none

  | LoadSearch _, _ -> model, Cmd.none

  | ToggleSidebarVisibility, _ -> model, Cmd.none

  | UpdateInputSearchBox term , _->
      let inputMessage = InputSearchBox.Message.SetField term
      let inputSearchBoxModel, _ = InputSearchBox.update timonService inputMessage model.inputSearchBoxModel
      { model with term = term; inputSearchBoxModel = inputSearchBoxModel}, Cmd.none


type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
      match model.authState with
      | AuthState.Success ->
        let inputSearchBox =
          InputSearchBox.view model.inputSearchBoxModel (InputSearchBoxMsg >> dispatch)

        ComponentsTemplate
          .SearchBox()
          .InputSearchBox(inputSearchBox)
          .ClubName(model.clubName)
          .ShowClubSidebar(fun _ -> (dispatch (ToggleSidebarVisibility)))
          .Elt()
      | _ -> empty



let view authState (model: Model) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              authState = authState }
        dispatch
