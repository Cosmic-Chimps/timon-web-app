module TimonWebApp.Client.Pages.Components.ChannelMenuForm

open System.Net
open Bolero
open Elmish
open Microsoft.JSInterop
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages
open TimonWebApp.Client.Services
open Bolero.Html
open TimonWebApp.Client.Validation

type ChannelForm = { name: string }

type Model =
    { errorsValidateChannelForm: Result<ChannelForm, Map<string, string list>> option
      channelForm: ChannelForm
      isAddingChannel: bool
      authentication: AuthState }
    static member Default =
        { channelForm = { name = "" }
          errorsValidateChannelForm = None
          isAddingChannel = false
          authentication = AuthState.NotTried }

type Message =
    | ValidateChannel
    | SetChannelFormField of string * string
    | ToggleChannelForm
    | AddChannel
    | ChannelAdded of HttpStatusCode
    | NotifyChannelAdded

let validateChannelForm (channelForm) =
    let validateName (validator: Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank(name + " cannot be blank")
        |> validator.End

    all
    <| fun t -> { name = validateName t "Name" channelForm.name }


let update (jsRuntime: IJSRuntime) (timonService: TimonService) (message: Message) (model: Model) =
    let validateChannelForced form =
        let mapResults = validateChannelForm form
        { model with
              channelForm = form
              errorsValidateChannelForm = Some mapResults }

    let validateChannel form =
        match model.errorsValidateChannelForm with
        | None -> { model with channelForm = form }
        | Some _ -> validateChannelForced form

    match message, model with
    | SetChannelFormField ("name", value), _ ->
        { model.channelForm with
              name = value.Trim() }
        |> validateChannel,
        Cmd.none
    | ValidateChannel, _ -> model.channelForm |> validateChannelForced, Cmd.ofMsg (AddChannel)
    | ChannelAdded _, _ ->
        let channelForm = { model.channelForm with name = "" }
        { model with
              isAddingChannel = false
              channelForm = channelForm },
        Cmd.ofMsg NotifyChannelAdded
    | ToggleChannelForm, _ ->
        jsRuntime.InvokeVoidAsync("jsTimon.focusElement", "channel_new_input")
        |> ignore

        { model with
              isAddingChannel = not model.isAddingChannel },
        Cmd.none

    | _, ({ errorsValidateChannelForm = Some (Error _) }) -> model, Cmd.none

    | AddChannel, _ ->
        let payload: CreateChannelPayload = { name = model.channelForm.name }

        let cmd =
            Cmd.ofAsync createChannel (timonService, payload) ChannelAdded raise

        model, cmd
    | _, _ -> model, Cmd.none

type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        let formFieldItem =
            Controls.inputAdd
                "channel_new_input"
                "Add new channel"
                Mdi.``mdi-pound``
                model.errorsValidateChannelForm
                (Some "name")

        let inputCallback =
            fun v -> dispatch (SetChannelFormField("name", v))

        let buttonAction = (fun _ -> dispatch ValidateChannel)

        let inputBox, icon =
            match model.isAddingChannel with
            | true ->
                (formFieldItem "Name" model.channelForm.name inputCallback buttonAction,
                 Mdi.``mdi-minus-circle-outline``)
            | false -> (empty, Mdi.``mdi-plus-circle-outline``)

        let icon =
            a [ attr.``class`` Bulma.``has-text-grey``
                on.click (fun _ -> dispatch ToggleChannelForm) ] [
                i [ attr.``class``
                    <| String.concat " " [ "mdi"; icon ] ] []
            ]

        match model.authentication with
        | AuthState.Success -> ComponentsTemplate.AddChannelForm().Icon(icon).ChannelInput(inputBox).Elt()
        | _ -> ComponentsTemplate.AddChannelForm().Elt()

let view authState (model: Model) dispatch =
    ecomp<Component, _, _>
        []
        { model with
              authentication = authState }
        dispatch
