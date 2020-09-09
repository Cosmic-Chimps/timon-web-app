module TimonWebApp.Client.Pages.Home

open System
open Elmish
open Bolero
open Bolero.Html
open Microsoft.JSInterop
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages.Components
open TimonWebApp.Client.Pages.Components.LinkViewList
open TimonWebApp.Client.Services


type Model = {
    addLinkBoxModel: AddLinkBox.Model
    channelMenuModel: ChannelMenu.Model
    channelMenuFormModel: ChannelMenuForm.Model
    linkViewListModel: LinkViewList.Model
    channel: string
    channelId: Guid
    page: int
    showNext: bool

}
with
    static member Default = {
        linkViewListModel = LinkViewList.Model.Default
        addLinkBoxModel = AddLinkBox.Model.Default
        channelMenuModel = ChannelMenu.Model.Default
        channelMenuFormModel = ChannelMenuForm.Model.Default
        channel = "general"
        channelId = Guid.Empty
        page = 0
        showNext = false
    }

type Message =
    | LinksLoaded of GetLinksResult
    | ChannelsLoaded of ChannelView array
    | LoadLinks of bool * string * Guid * int
    | LoadLinksByTag of string
    | LoadChannels
    | AddLinkBoxMsg of AddLinkBox.Message
    | ChannelMenuMsg of ChannelMenu.Message
    | ChannelMenuFormMsg of ChannelMenuForm.Message
    | LinkViewItemMsg of LinkViewList.Message

let init (_: IJSRuntime) (channel: string) =
    { Model.Default with channel = channel }, Cmd.ofMsg (LoadLinks (true, channel, Guid.Empty, 0))


let update (jsRuntime: IJSRuntime) (timonService: TimonService) (message: Message) (model: Model) =
    match message, model with
    | LinksLoaded data, _ ->
        let linkViewFormList = data.Links
                                |> Seq.map( fun lv -> {
                                                view = lv
                                                isTagFormOpen = false
                                                errorValidateForm = None
                                                tagForm = { tags = "" }
                                                openMoreInfo = false
                                            })
                                |> Seq.toArray
        let linkViewListModel = { model.linkViewListModel with links = linkViewFormList }
        let addLinkBoxModel = { model.addLinkBoxModel with channelName = model.channel; channelId = model.channelId }
        jsRuntime.InvokeVoidAsync("scroll", 0, 0).AsTask() |> Async.AwaitTask |> ignore
        { model with
            linkViewListModel = linkViewListModel
            addLinkBoxModel = addLinkBoxModel
            page = data.Page
            showNext = data.ShowNext }, Cmd.none
    | ChannelsLoaded channels, _ ->
        let channelMenuModel = { model.channelMenuModel with channels = channels }
        { model with channelMenuModel = channelMenuModel }, Cmd.none
    | LoadLinks (loadChannels, channel, channelId, page), _ ->
        let queryParams = {
            channelId = channelId
            page = page
        }
        let linksCmd = Cmd.ofAsync getLinks (timonService, queryParams) LinksLoaded raise

        let batchCmds = [ linksCmd ] @ match loadChannels with
                                        | true -> [Cmd.ofMsg LoadChannels]
                                        | false -> [Cmd.none]

        { model with channel = channel; channelId = channelId; page = page }, Cmd.batch batchCmds

    | LoadLinksByTag (tag), _ ->
        model, Cmd.none
//        let queryParams = {
//            channelId = channelId
//            page = page
//        }
//        let linksCmd = Cmd.ofAsync getLinks (timonService, queryParams) LinksLoaded raise
//
//        let batchCmds = [ linksCmd ] @ match loadChannels with
//                                        | true -> [Cmd.ofMsg LoadChannels]
//                                        | false -> [Cmd.none]
//
//        { model with channel = channel; channelId = channelId; page = page }, Cmd.batch batchCmds

    | LoadChannels, _ ->
        let cmd = Cmd.ofAsync getChannels timonService ChannelsLoaded raise
        model, cmd

    | AddLinkBoxMsg (AddLinkBox.Message.NotifyLinkAdded), _ ->
        model, Cmd.ofMsg (LoadLinks (false, model.channel, model.channelId, model.page))
    | AddLinkBoxMsg msg, _ ->
        let m, cmd = AddLinkBox.update timonService msg model.addLinkBoxModel
        { model with addLinkBoxModel = m }, Cmd.map AddLinkBoxMsg cmd

    | ChannelMenuFormMsg (ChannelMenuForm.Message.NotifyChannelAdded), _ ->
        model, Cmd.ofMsg LoadChannels
    | ChannelMenuFormMsg msg, _ ->
        let m, cmd = ChannelMenuForm.update timonService msg model.channelMenuFormModel
        { model with channelMenuFormModel = m }, Cmd.map ChannelMenuFormMsg cmd

    | LinkViewItemMsg (LinkViewList.Message.LoadLinksbyTag (tag)), _ ->
        let channelModel = { model.channelMenuModel with activeChannelId = Guid.Empty }
        { model with channelMenuModel = channelModel }, Cmd.ofMsg (LoadLinksByTag tag)

    | LinkViewItemMsg (LinkViewList.Message.LoadLinks (channelId, channel)), _ ->
        let channelModel = { model.channelMenuModel with activeChannelId = channelId }
        { model with channelMenuModel = channelModel }, Cmd.ofMsg (LoadLinks (false, channel, channelId, 0))
    | LinkViewItemMsg (LinkViewList.Message.NotifyTagsAdded), _ ->
        model, Cmd.ofMsg (LoadLinks (false, model.channel, model.channelId, model.page))
    | LinkViewItemMsg msg, _ ->
        let m, cmd = LinkViewList.update timonService msg model.linkViewListModel
        { model with linkViewListModel = m }, Cmd.map LinkViewItemMsg cmd

    | ChannelMenuMsg (ChannelMenu.Message.LoadLinks (channelId, channel)), _ ->
        let channelModel = { model.channelMenuModel with activeChannelId = channelId }
        { model with channelMenuModel = channelModel }, Cmd.ofMsg (LoadLinks (false, channel, channelId, 0))
    | ChannelMenuMsg _, _ ->
        model, Cmd.none


type HomeTemplate = Template<"wwwroot/home.html">

let previousButton (model: Model) dispatch =
    let isDisabled, onClick =
        match model.page with
        | 0 -> (true, (fun _ -> ()))
        | _ -> (false, (fun _ -> dispatch (LoadLinks(false, model.channel, model.channelId, model.page - 1))))


    a [ attr.``class``  Bulma.``pagination-previous``
        attr.disabled isDisabled
        on.click  onClick] [
            text "Previous"
    ]

let nextButton (model: Model) dispatch =
    let isDisabled, onClick =
        match model.showNext with
        | false -> (true, (fun _ -> ()))
        | true -> (false, (fun _ -> dispatch (LoadLinks(false, model.channel, model.channelId, model.page + 1))))

    a [ attr.``class`` Bulma.``pagination-next``
        attr.disabled isDisabled
        on.click onClick ] [
        text "Next"
    ]
let view authState model dispatch =
    let items = LinkViewList.view authState model.linkViewListModel (LinkViewItemMsg >> dispatch)
    let channels = ChannelMenu.view authState (model.channelMenuModel) (ChannelMenuMsg >> dispatch)
    let channelForm = ChannelMenuForm.view authState (model.channelMenuFormModel) (ChannelMenuFormMsg >> dispatch)
    let addLinkBoxHole = AddLinkBox.view authState (model.addLinkBoxModel) (AddLinkBoxMsg >> dispatch)

    let previousButton = previousButton model dispatch
    let nextButton = nextButton model dispatch

    HomeTemplate()
        .LinkListHole(items)
        .ChannelForm(channelForm)
        .ChannelListHole(channels)
        .AddLinkBoxHole(addLinkBoxHole)
        .ChannelName(model.channel)
        .PreviousButton(previousButton)
        .NextButton(nextButton)
        .Elt()