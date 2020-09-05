module TimonWebApp.Client.Pages.Controls

open Bolero.Html
open Microsoft.JSInterop
open TimonWebApp.Client.BoleroHelpers
open TimonWebApp.Client.Common

let errorAndClass name onFocus (result:Result<_,Map<_,_>> option) =
      match result, onFocus with
      | _ , Some focus when focus = name -> None,""
      | Some (Error e), _ when (e.ContainsKey name && e.[name] <> []) -> Some(System.String.Join(",", e.[name])), Bulma.``is-danger``
      | Some _, _ -> None, "modified valid"
      | _ -> None,""

let formFieldItem  validatedForm onFocus focusMessage fieldType name value callback =
    let error, validClass = errorAndClass name onFocus validatedForm
    div [ attr.``class`` "field" ] [
        div [ attr.``class`` "control" ] [
            input [ attr.``class`` <| String.concat " " [ Bulma.input; Bulma.``is-large``; validClass ]
                    attr.``type`` fieldType
                    attr.placeholder name
                    attr.autofocus ""
                    bind.input.string value callback
                    ]
            match error with
            | Some value ->
                span [attr.``class`` <| String.concat " " [ Bulma.``has-text-danger`` ]]
                    [text value]
            | None -> ()
        ]
    ]

let inputAddLink item onFocus inputName inputValue inputCallback buttonAction =
    let error, validClass = errorAndClass inputName onFocus item
    concat [
//        comp<KeySubscriber> [] []
        div [ attr.``class`` <| String.concat " " [ Bulma.content; Bulma.``is-medium`` ] ] [
            div [ attr.``class`` <| String.concat " " [ Bulma.field; Bulma.``has-addons`` ] ] [
                div [ attr.``class``<| String.concat " " [Bulma.control; Bulma.``is-expanded`` ] ] [
                    p [ attr.``class`` <| String.concat " " [ Bulma.control; Bulma.``has-icons-left``; Bulma.``has-icons-right`` ] ] [
                        input [ attr.``class`` <| String.concat " " [ Bulma.input; validClass ]
                                attr.``type`` "text"
                                attr.placeholder "Add new link"
                                bind.input.string inputValue inputCallback
                                on.keyup (fun e ->
                                            match e.Key with
                                                | "Enter" -> buttonAction(null)
                                                | "Escape" -> ()
                                                | _ ->()
                                            )]
                        span [ attr.``class`` <| String.concat " " [ Bulma.icon; Bulma.``is-small``; Bulma.``is-left`` ] ] [
                            i [ attr.``class`` <| String.concat " " [ Mdi.mdi; Mdi.``mdi-link`` ] ] []
                        ]

//                        span [ attr.``class`` <| String.concat " " [ Bulma.icon; Bulma.``is-small``; Bulma.``is-right`` ] ] [
//                            i [ attr.``class`` <| String.concat " " [ Mdi.mdi; Mdi.``mdi-plus`` ] ] []
//                        ]
                        match error with
                        | Some value ->
                            span [attr.``class`` <| String.concat " " [ Bulma.``has-text-danger`` ]]
                                [text value]
                        | None -> ()
                    ]
                ]
                div [ attr.``class`` Bulma.control ] [
                    button [ attr.``class``
                             <| String.concat
                                 " "
                                    [ Bulma.button
                                      Bulma.``is-block``
                                      Bulma.``is-info``
                                      Bulma.``is-fullwidth`` ]
                             on.click buttonAction ] [
                        text "Add"
                    ]
                ]
            ]
        ]
    ]

let inputAdd placeholder leftIcon item onFocus inputName inputValue inputCallback buttonAction =
    let error, validClass = errorAndClass inputName onFocus item
    concat [
//        comp<KeySubscriber> [] []
        div [ attr.``class`` <| String.concat " " [ Bulma.field; Bulma.``has-addons`` ] ] [
                div [ attr.``class``<| String.concat " " [Bulma.control; Bulma.``is-expanded`` ] ] [
                    p [ attr.``class`` <| String.concat " " [ Bulma.control; Bulma.``has-icons-left``; Bulma.``has-icons-right`` ] ] [
                        input [ attr.``class`` <| String.concat " " [ Bulma.input; validClass ]
                                attr.``type`` "text"
                                attr.placeholder placeholder
                                bind.input.string inputValue inputCallback
                                on.keyup (fun e ->
                                            match e.Key with
                                                | "Enter" -> buttonAction(null)
                                                | "Escape" -> ()
                                                | _ ->()
                                            )]
                        span [ attr.``class`` <| String.concat " " [ Bulma.icon; Bulma.``is-small``; Bulma.``is-left`` ] ] [
                            i [ attr.``class`` <| String.concat " " [ Mdi.mdi; leftIcon ] ] []
                        ]

//                        span [ attr.``class`` <| String.concat " " [ Bulma.icon; Bulma.``is-small``; Bulma.``is-right`` ] ] [
//                            i [ attr.``class`` <| String.concat " " [ Mdi.mdi; Mdi.``mdi-plus`` ] ] []
//                        ]
                        match error with
                        | Some value ->
                            span [attr.``class`` <| String.concat " " [ Bulma.``has-text-danger`` ]]
                                [text value]
                        | None -> ()
                    ]
                ]
                div [ attr.``class`` Bulma.control ] [
                    button [ attr.``class``
                             <| String.concat
                                 " "
                                    [ Bulma.button
                                      Bulma.``is-block``
                                      Bulma.``is-info``
                                      Bulma.``is-fullwidth`` ]
                             on.click buttonAction ] [
                        text "Add"
                    ]
                ]
            ]
    ]

//<div class="content is-medium">
//        <p class="control has-icons-left has-icons-right">
//            <input class="input" type="text" placeholder="Add new link">
//            <span class="icon is-small is-left">
//              <i class="mdi mdi-link"></i>
//            </span>
//            <span class="icon is-small is-right">
//              <i class="mdi mdi-plus"></i>
//            </span>
//        </p>
//    </div>