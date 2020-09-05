module TimonWebApp.Client.Pages.Components.AddLinkBox

open System.Net
open Bolero
open Elmish
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages.Controls
open TimonWebApp.Client.Services
open TimonWebApp.Client.Validation
open Bolero.Html

type UrlForm = {
    url: string
}

type Model = {
    errorsValidateUrlForm : Result<UrlForm,Map<string,string list>> option
    urlForm: UrlForm
    authentication: AuthState
}
with
    static member Default = {
        urlForm = { url = ""}
        errorsValidateUrlForm = None
        authentication = AuthState.NotTried
    }
type Message =
    | SetFormField of string * string
    | ValidateLink
    | AddLink
    | LinkAdded of HttpStatusCode
    | LoadLinks
    | NotifyLinkAdded

let validateForm (urlForm) =
    let ValidUrl (validator:Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank (name + " cannot be blank")
        |> validator.IsUrl (name + " should be in url format 'https://'")
        |> validator.End

    all <| fun t -> {
        url = ValidUrl t "Link" urlForm.url
    }


let update (timonService: TimonService) (message: Message) (model: Model) =
    let validateForced form =
        let mapResults = validateForm form
        {model with urlForm = form; errorsValidateUrlForm = Some mapResults }

    let validate form =
        match model.errorsValidateUrlForm with
        | None  ->
            {model with urlForm = form; }
        | Some _ -> validateForced form

    match message, model with
    | SetFormField ("url", value), _ ->
        {model.urlForm with url = value.Trim()} |> validate, Cmd.none
    | ValidateLink, _ ->
        model.urlForm |> validateForced, Cmd.ofMsg (AddLink)
    | LinkAdded _, _ ->
        let urlForm = { model.urlForm with url = "" }
        { model with urlForm = urlForm }, Cmd.ofMsg NotifyLinkAdded
    | _ , ({ errorsValidateUrlForm = Some(Error _) }) -> model , Cmd.none
    | AddLink, _ ->
        let createLinkPayload = {
            url = model.urlForm.url
            channelId = ""
            via = "web"
        }
        let cmdAddLink = Cmd.ofAsync createLink (timonService, createLinkPayload) LinkAdded raise
        model, cmdAddLink
     | _, _ -> model, Cmd.none


type Component() =
    inherit ElmishComponent<Model, Message>()
    override _.View model dispatch =
        match model.authentication with
        | AuthState.Success ->
            let formFieldItem = inputAddLink model.errorsValidateUrlForm (Some "url")
            let inputCallback = fun v -> dispatch (SetFormField("url",v ))
            let buttonAction = fun _ -> dispatch ValidateLink

            formFieldItem "Link" model.urlForm.url inputCallback buttonAction
        | _ -> empty


let view authState (model: Model) dispatch =
    ecomp<Component,_,_> [] { model with authentication = authState} dispatch