module TimonWebApp.Client.Pages.Login

open System
open Elmish
open Bolero.Html
open TimonWebApp.Client.BoleroHelpers
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages
open TimonWebApp.Client.Services
open TimonWebApp.Client.Validation

type LoginInput = { Email : string; Password : string}

type Model = {
    FailureReason : string option
    IsSignedIn : bool
    CurrentLogin : LoginInput;
    ValidatedLogin : Result<LoginInput,Map<string,string list>> option
    Focus : string option
}
with
    static member Default = {
         CurrentLogin =  {Email = "" ; Password = "" };
         ValidatedLogin =  None;
         IsSignedIn = false;
         FailureReason = None
         Focus = None
    }
    
type Message =
    | DoLogin
    | LoginSuccess of option<Authentication>
    | SignInSuccessful of Authentication
    | LoginError of exn
    | Focused of string
    | SetFormField of string *string
    
let init _ =
    Model.Default, Cmd.none

let validateForm (form : LoginInput) =
    let cannotBeBlank (validator:Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank (name + " cannot be blank")
        |> validator.End
        
    let validEmail (validator:Validator<string>) name value =
        validator.Test name value
        |> validator.NotBlank (name + " cannot be blank")
        |> validator.IsMail (name + " should be an email format")
        |> validator.End
        
    all <| fun t -> {
        Email = validEmail t (nameof form.Email) form.Email
        Password =  cannotBeBlank t (nameof form.Password) form.Password
    }

let update (remote: AuthService) message (model , _: State) =
    let validateForced form =
        let validated = validateForm form
        {model with CurrentLogin = form; ValidatedLogin = Some validated; FailureReason = None}
        
    let validate form =
        match model.ValidatedLogin with
        | None  ->
            {model with CurrentLogin = form; FailureReason = None}
        | Some _ -> validateForced form

    match message, model with
    | Focused field, _ -> { model with Focus = Some field}, Cmd.none
    | SetFormField("Email",value),_ ->
        {model.CurrentLogin with Email = value} |> validate, Cmd.none
    | SetFormField("Password",value),_ ->
        {model.CurrentLogin with Password = value} |> validate, Cmd.none
    | _ , ({ ValidatedLogin = Some(Error _) }) -> model , Cmd.none
    | DoLogin, { ValidatedLogin = None } ->
        model.CurrentLogin |> validateForced, Cmd.ofMsg (DoLogin)
    | DoLogin, _ ->
        model,
        Cmd.ofAsync
            remote.``sign-in`` ({
                Email = model.CurrentLogin.Email
                Password = model.CurrentLogin.Password
            })
            LoginSuccess
            LoginError
    | LoginSuccess value, _ ->
        match value with
        | Some loginResponse -> { model with IsSignedIn = true}, Cmd.ofMsg(SignInSuccessful loginResponse)
        | None -> { model with IsSignedIn = false }, Cmd.none
    | LoginError exn, _ -> { model with FailureReason = Some exn.Message }, Cmd.none
    | _ -> failwith ""

let view model dispatch =
    div [ attr.``class``
          <| String.concat
              " "
                 [ Bulma.hero
                   Bulma.``is-fullheight``
                   Bulma.``is-light`` ] ] [
        div [ attr.``class`` Bulma.``hero-body`` ] [
            div [ attr.``class``
                  <| String.concat
                      " "
                         [ Bulma.container
                           Bulma.``has-text-centered`` ] ] [
                div [ attr.``class``
                      <| String.concat
                          " "
                             [ Bulma.column
                               Bulma.``is-8``
                               Bulma.``is-offset-2`` ] ] [
                    h1 [ attr.``class``
                         <| String.concat " " [ Bulma.title; Bulma.``has-text-grey`` ] ] [
                        text "Login"
                    ]
                    hr [ attr.``class`` "login-hr" ]
                    div [ attr.``class`` Bulma.box ] [
                        div [ attr.``class`` Bulma.box ] [
                            img [ attr.src "images/timon_logo.png" ]
                        ]
                        div [ attr.``class``
                              <| String.concat
                                  " "
                                     [ Bulma.title
                                       Bulma.``has-text-grey``
                                       Bulma.``is-5`` ] ] [
                            text "Please enter your email and password"
                        ]
                        match model.FailureReason with
                            | Some(value) ->
                                div[attr.``class`` <| String.concat " " [Bulma.``is-danger``; Bulma.message]][
                                    div[attr.``class`` Bulma.``message-header``] [
                                     text value
                                    ]
                                ]
                            | None -> ()
        
                        let focused = (fun name -> Action<_>(fun _ -> dispatch (Focused name)))
                        let formFieldItem = Controls.formFieldItem model.ValidatedLogin model.Focus focused
                        let pd name = fun v -> dispatch (SetFormField(name,v ))
                        
                        div [] [
                            concat [
                                comp<KeySubscriber> [] []
                                formFieldItem "email" "Email" model.CurrentLogin.Email (pd (nameof model.CurrentLogin.Email))
                                formFieldItem "password" "Password" model.CurrentLogin.Password (pd (nameof model.CurrentLogin.Password))
                                button [ attr.id "confirmButton"
                                         attr.``class``
                                         <| String.concat
                                             " "
                                                [ Bulma.button
                                                  Bulma.``is-block``
                                                  Bulma.``is-success``
                                                  Bulma.``is-large``
                                                  Bulma.``is-fullwidth`` ]
                                         on.click (fun _ -> dispatch DoLogin) ] [
                                    text "Login"
                                ]
                            ]
                        ]
                    ]
                    div [ attr.``class`` Bulma.columns ] [
                        div [ attr.``class`` Bulma.column ] [
                            a [ attr.href ""
                                attr.``class`` Bulma.``has-text-grey`` ] [
                                text "Sign up1"
                            ]
                        ]
                        div [ attr.``class`` Bulma.column ] [
                            a [ attr.href ""
                                attr.``class`` Bulma.``has-text-grey`` ] [
                                text "Forgot Password"
                            ]
                        ]
                        div [ attr.``class`` Bulma.column ] [
                            a [ attr.href ""
                                attr.``class`` Bulma.``has-text-grey`` ] [
                                text "Need Help?"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]