module TimonWebApp.Client.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open Microsoft.JSInterop
open TimonWebApp.Client.Pages
open TimonWebApp.Client.Services
open TimonWebApp.Client

/// Routing endpoints definition.
type Page =
    | Start
    | [<EndPoint "/">] Home of PageModel<Home.Model>
    | [<EndPoint "/login">] Login of PageModel<Login.Model>

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | HomeMsg of Home.Message
    | LoginMsg of Login.Message
    | CommonMessage of Common.Message
    
/// The Elmish application's model.
type Model =
    {
        Page: Page
        State : Common.State
        Error: string option
        SignedInAs: option<string>
        SignInFailed: bool
        BufferedCommand : Cmd<Message>
    }

let init =
    {
        Page = Start
        Error = None
        SignedInAs = None
        SignInFailed = false
        BufferedCommand = Cmd.none
        State  = { Authentication = Common.AuthState.NotTried};
    }, Cmd.none


let initPage  init (model : Model) msg page =
    let pageModel, cmd = init
    let page = { Model = pageModel } |> page
    { model with Page  = page; }, Cmd.map msg cmd

let initLogin (jsRunTime: IJSRuntime) model =
    initPage (Login.init jsRunTime) model LoginMsg Login
    
let initHome (jsRunTime: IJSRuntime) model =
    initPage (Home.init jsRunTime) model HomeMsg Home

let update jsRuntime remote message model =
    let genericUpdate update subModel msg  msgFn pageFn =
        let subModel, cmd = update  msg subModel
        {model with Page = pageFn({ Model = subModel})}, Cmd.map msgFn cmd

    let genericUpdateWithCommon update subModel msg  msgFn pageFn =
        let subModel, cmd, (commonCommand  : Cmd<Common.Message>) = update  msg (subModel, model.State)
        if commonCommand |> List.isEmpty then
            {model with Page = pageFn({ Model = subModel})},  Cmd.map msgFn cmd
        else
            let m = {model with Page = pageFn({ Model = subModel}); BufferedCommand = Cmd.map msgFn cmd}
            m,Cmd.map CommonMessage commonCommand
            
    match message, model.Page with
    | LoginMsg msg, Login loginModel ->
        genericUpdateWithCommon (Login.update remote) (loginModel.Model) msg LoginMsg Login
    | SetPage(Page.Login _),  _ -> initLogin jsRuntime model
    | SetPage(Page.Home _),  _ -> initHome jsRuntime model
    | SetPage(Start), _
    | _ -> model, Cmd.none

let defaultPageModel (jsRuntime: IJSRuntime) = function
| Login model -> Router.definePageModel model (Login.init jsRuntime |> fst)
| Home model -> Router.definePageModel model (Home.init jsRuntime |> fst)
| Start -> ()

let buildRouter (jsRuntime: IJSRuntime) = Router.inferWithModel SetPage (fun m -> m.Page) (defaultPageModel jsRuntime)

let view (js: IJSRuntime) (model : Model) dispatch =
    let content =
        cond model.Page <| function
        | Login (model) -> Login.view model.Model (LoginMsg >> dispatch)
        | Home (model) -> Home.view model.Model (LoginMsg >> dispatch)
        | Start -> h2 [] [text "Timon WebApp Loading ..."]
    
    content

type Main = Template<"wwwroot/main.html">

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
//        Program.mkSimple (fun _ -> initModel) update view
        let remote = this.Remote<AuthService>()
        let router = buildRouter (this.JSRuntime)
        let update = update (this.JSRuntime) remote
        Program.mkProgram (fun _ -> init) (update) (view this.JSRuntime)
            |> Program.withRouter router
//    override this.Program =
//        let bookService = this.Remote<BookService>()
//        let update = update bookService
//        Program.mkProgram (fun _ -> initModel, Cmd.ofMsg GetSignedInAs) update view
//        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
