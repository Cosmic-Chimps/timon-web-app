module TimonWebApp.Client.Pages.Home

open System
open System.Collections.Generic
open Elmish
open Bolero
open Bolero.Html
open Microsoft.JSInterop
open TimonWebApp.Client
open TimonWebApp.Client.Common
open TimonWebApp.Client.Services

type Model = {
    Endpoint: string
    Links: LinkView array
}
with
    static member Default = {
        Endpoint = ""
        Links = Array.empty
    }
    
type Message =
    | LinksLoaded of LinkView array
    | OnUpperResult of string
    | LoadLinks



let init (jsRuntime: IJSRuntime) =
    Model.Default, Cmd.none

let update (jsRuntime: IJSRuntime) (timonService: TimonService) (message: Message) (model: Model) =
    jsRuntime.InvokeAsync("console.log", "home.inner.update") |> ignore
    jsRuntime.InvokeAsync("console.log", message) |> ignore
    
    match message with
    | OnUpperResult links ->
        model, Cmd.none
    | LinksLoaded links ->
        { model with Links = links }, Cmd.none
    | LoadLinks ->
//        let cmd = Cmd.ofAsync remote.links () LinksLoaded raise
//        let cmd = Cmd.ofFunc getLinks (model.Endpoint) LinksLoaded raise
        let cmd = Cmd.ofAsync getLinks timonService LinksLoaded raise
        model, cmd
//        model, Cmd.ofAsync asyncUpper "hola" OnUpperResult raise
    | _ -> failwith "" 
    
type HomeTemplate = Template<"wwwroot/home.html">

let viewLinkItems (links : IReadOnlyList<LinkView> ) dispatcher =
    forEach links (fun l ->
        HomeTemplate.LinkItem()
            .Url(l.Link.Url)
            .Title(l.Link.Title)
            .DomainName(l.Link.DomainName)
            .Date(l.Data.DateCreated.ToString())
            .ShortDescription(l.Link.ShortDescription)
            .ChannelName(l.Channel.Name)
            .SharedBy(l.User.Email)
            .Elt())

let view model dispatch =
    let items = viewLinkItems model.Links dispatch
    HomeTemplate()
        .LinkList(items)
        .Elt()