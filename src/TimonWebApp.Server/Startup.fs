namespace TimonWebApp.Server

open System
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Bolero.Remoting.Server
open Bolero.Server.RazorHost
open Bolero.Templating.Server
open TimonWebApp.Client.Services
open TimonWebApp.Server.AuthService

type Startup() =

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddMvc().AddRazorRuntimeCompilation() |> ignore
        services.AddServerSideBlazor() |> ignore
        services.AddDataProtection() |> ignore;
        services
            .AddAuthorization()
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .Services
            .AddRemoting<AuthService>()
            .AddRemoting<LinkService>()
            .AddBoleroHost()
#if DEBUG
            .AddHotReload(templateDir = __SOURCE_DIRECTORY__ + "/../TimonWebApp.Client")
#endif
        |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        app
            .UseAuthentication()
            .UseRemoting()
            .UseStaticFiles()
            .UseRouting()
            .UseBlazorFrameworkFiles()
            .UseEndpoints(fun endpoints ->
#if DEBUG
                endpoints.UseHotReload()
#endif
                endpoints.MapBlazorHub() |> ignore
                endpoints.MapFallbackToPage("/_Host") |> ignore)
        |> ignore

module Program =

    [<EntryPoint>]
    let main args =
        let environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")

        let environment' =
            if String.IsNullOrEmpty(environment) then "Production" else environment

        let configuration = ConfigurationBuilder().AddJsonFile("appsettings.json", false, true)
                                       .AddJsonFile(sprintf "appsettings.%s.json" environment', true)
                                       .AddEnvironmentVariables()
                                       .Build()
                                         
        WebHost
            .CreateDefaultBuilder(args)
            .UseStaticWebAssets()
            .UseConfiguration(configuration)
            .UseStartup<Startup>()
            .Build()
            .Run()
        0
