module TimonWebApp.Client.Pages.Home

open System
open Elmish
open Bolero
open Microsoft.JSInterop
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

}
with
    static member Default = {
        linkViewListModel = LinkViewList.Model.Default
        addLinkBoxModel = AddLinkBox.Model.Default
        channelMenuModel = ChannelMenu.Model.Default
        channelMenuFormModel = ChannelMenuForm.Model.Default
        channel = "general"
        channelId = Guid.Empty
    }

type Message =
    | LinksLoaded of LinkView array
    | ChannelsLoaded of ChannelView array
    | LoadLinks of bool * string * Guid
    | LoadChannels
    | AddLinkBoxMsg of AddLinkBox.Message
    | ChannelMenuMsg of ChannelMenu.Message
    | ChannelMenuFormMsg of ChannelMenuForm.Message
    | LinkViewItemMsg of LinkViewList.Message

let init (_: IJSRuntime) (channel: string) =
    { Model.Default with channel = channel }, Cmd.ofMsg (LoadLinks (true, channel, Guid.Empty))


let update (_: IJSRuntime) (timonService: TimonService) (message: Message) (model: Model) =
    match message, model with
    | LinksLoaded links, _ ->
        let linkViewFormList = links
                                |> Seq.map( fun lv -> {
                                                view = lv
                                                isTagFormOpen = false
                                                errorValidateForm = None
                                                tagForm = { tags = "" }
                                            })
                                |> Seq.toArray
        let linkViewListModel = { model.linkViewListModel with links = linkViewFormList }
        let addLinkBoxModel = { model.addLinkBoxModel with channelName = model.channel; channelId = model.channelId }
        { model with linkViewListModel = linkViewListModel; addLinkBoxModel = addLinkBoxModel }, Cmd.none
    | ChannelsLoaded channels, _ ->
        let channelMenuModel = { model.channelMenuModel with channels = channels }
        { model with channelMenuModel = channelMenuModel }, Cmd.none
    | LoadLinks (loadChannels, channel, channelId), _ ->
        let queryParams = {
            channelId = channelId
        }
        let linksCmd = Cmd.ofAsync getLinks (timonService, queryParams) LinksLoaded raise

        let batchCmds = [ linksCmd ] @ match loadChannels with
                                        | true -> [Cmd.ofMsg LoadChannels]
                                        | false -> [Cmd.none]

        { model with channel = channel; channelId = channelId }, Cmd.batch batchCmds
    | LoadChannels, _ ->
        let cmd = Cmd.ofAsync getChannels timonService ChannelsLoaded raise
        model, cmd

    | AddLinkBoxMsg (AddLinkBox.Message.NotifyLinkAdded), _ ->
        model, Cmd.ofMsg (LoadLinks (false, model.channel, model.channelId))
    | AddLinkBoxMsg msg, _ ->
        let m, cmd = AddLinkBox.update timonService msg model.addLinkBoxModel
        { model with addLinkBoxModel = m }, Cmd.map AddLinkBoxMsg cmd

    | ChannelMenuFormMsg (ChannelMenuForm.Message.NotifyChannelAdded), _ ->
        model, Cmd.ofMsg LoadChannels
    | ChannelMenuFormMsg msg, _ ->
        let m, cmd = ChannelMenuForm.update timonService msg model.channelMenuFormModel
        { model with channelMenuFormModel = m }, Cmd.map ChannelMenuFormMsg cmd

    | LinkViewItemMsg (LinkViewList.Message.NotifyTagsAdded), _ ->
        model, Cmd.ofMsg (LoadLinks (false, model.channel, model.channelId))
    | LinkViewItemMsg msg, _ ->
        let m, cmd = LinkViewList.update timonService msg model.linkViewListModel
        { model with linkViewListModel = m }, Cmd.map LinkViewItemMsg cmd

    | ChannelMenuMsg (ChannelMenu.Message.LoadLinks (channelId, channel)), _ ->
        model, Cmd.ofMsg (LoadLinks (false, channel, channelId))
    | ChannelMenuMsg _, _ ->
        model, Cmd.none


type HomeTemplate = Template<"wwwroot/home.html">

let view authState model dispatch =
    let items = LinkViewList.view authState model.linkViewListModel (LinkViewItemMsg >> dispatch)
    let channels = ChannelMenu.view authState (model.channelMenuModel) (ChannelMenuMsg >> dispatch)
    let channelForm = ChannelMenuForm.view authState (model.channelMenuFormModel) (ChannelMenuFormMsg >> dispatch)
    let addLinkBoxHole = AddLinkBox.view authState (model.addLinkBoxModel) (AddLinkBoxMsg >> dispatch)

    HomeTemplate()
        .LinkListHole(items)
        .ChannelForm(channelForm)
        .ChannelListHole(channels)
        .AddLinkBoxHole(addLinkBoxHole)
        .ChannelName(model.channel)
        .Elt()