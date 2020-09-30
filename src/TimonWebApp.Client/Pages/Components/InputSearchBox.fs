module TimonWebApp.Client.Pages.Components.InputSearchBox

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


type Model =
    { term: string }
    static member Default = {
        term = String.Empty
    }

type Message =
    | SetField of string
    | Search of string

let update (timonService: TimonService) (message: Message) (model: Model) =
    match message, model with
    | SetField (value), _ ->
        { model with term = value.Trim() }, Cmd.none
    | _, _ -> model, Cmd.none


type Component() =
    inherit ElmishComponent<Model, Message>()

    override _.View model dispatch =
        let formFieldItem = inputSearch

        let inputCallback =
            fun v -> dispatch (SetField(v))

        let buttonAction = fun _ -> dispatch (Search model.term)

        formFieldItem model.term inputCallback buttonAction


let view (model: Model) dispatch =
    ecomp<Component, _, _>
        []
        model
        dispatch
