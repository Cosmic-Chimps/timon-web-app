module TimonWebApp.Client.Pages.Login

open System
open Elmish
open Bolero.Html
open TimonWebApp.Client.Common
open TimonWebApp.Client.Pages
open TimonWebApp.Client.Services
open TimonWebApp.Client.Validation

type LoginInput = { Email : string; Password : string}

type Model = {
    FailureReason : string option
    IsSigningIn : bool
    CurrentLogin : LoginInput;
    ValidatedLogin : Result<LoginInput,Map<string,string list>> option
    Focus : string option
}
with
    static member Default = {
         CurrentLogin =  {Email = "" ; Password = "" };
         ValidatedLogin =  None;
         IsSigningIn = false;
         FailureReason = None
         Focus = None
    }
    
type Message =
    | DoLogin
    | LoginSuccess of option<LoginResponse.Root>
    | LoginError of exn
    | Focused of string
    | SetFormField of string *string
    
let init jsRuntime =
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

let update (remote: AuthService) message (model , commonState: State) =
    let validateForced form =
        let validated = validateForm form
        {model with CurrentLogin = form; ValidatedLogin = Some validated; FailureReason = None}
        
    let validate form =
        match model.ValidatedLogin with
        | None  ->
            {model with CurrentLogin = form; FailureReason = None}
        | Some _ -> validateForced form

    match message, model with
    | Focused field, _ -> { model with Focus = Some field}, Cmd.none, Cmd.none
    | SetFormField("Email",value),_ ->
        {model.CurrentLogin with Email = value} |> validate, Cmd.none, Cmd.none
    | SetFormField("Password",value),_ ->
        {model.CurrentLogin with Password = value} |> validate, Cmd.none, Cmd.none
    | _ , ({ ValidatedLogin = Some(Error _) }) -> model , Cmd.none, Cmd.none
    | DoLogin, { ValidatedLogin = None } ->
        model.CurrentLogin |> validateForced, Cmd.ofMsg (DoLogin), Cmd.none
    | DoLogin, _ ->
        model,
        Cmd.ofAsync
            remote.``sign-in`` ({
                Email = model.CurrentLogin.Email
                Password = model.CurrentLogin.Password
            })
            LoginSuccess
            LoginError,
            Cmd.none
    | LoginSuccess value, _ -> { model with IsSigningIn = true}, Cmd.none, Cmd.none
    | LoginError exn, _ -> { model with FailureReason = Some exn.Message }, Cmd.none, Cmd.none
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
                            formFieldItem "email" "Email" model.CurrentLogin.Email (pd (nameof model.CurrentLogin.Email))
                            formFieldItem "password" "Password" model.CurrentLogin.Password (pd (nameof model.CurrentLogin.Password))
                            button [ attr.``class``
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
                    div [ attr.``class`` Bulma.columns ] [
                        div [ attr.``class`` Bulma.column ] [
                            a [ attr.href ""
                                attr.``class`` Bulma.``has-text-grey`` ] [
                                text "Sign up"
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