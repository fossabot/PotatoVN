﻿using Windows.ApplicationModel.DataTransfer;
using GalgameManager.Activation;
using GalgameManager.Contracts.Services;
using GalgameManager.Core.Contracts.Services;
using GalgameManager.Core.Services;
using GalgameManager.Helpers;
using GalgameManager.Models;
using GalgameManager.Services;
using GalgameManager.ViewModels;
using GalgameManager.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GalgameManager;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();
    
    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<List<string>>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddTransient<INavigationViewService, NavigationViewService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddTransient<IJumpListService, JumpListService>();
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDataCollectionService<Galgame>, GalgameCollectionService>();
            services.AddSingleton<IDataCollectionService<GalgameFolder>, GalgameFolderCollectionService>();
            services.AddSingleton<IFaqService, FaqService>();
            services.AddSingleton<IFilterService, FilterService>();
            services.AddSingleton<IUpdateService, UpdateService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<HelpViewModel>();
            services.AddTransient<HelpPage>();
            services.AddTransient<GalgameSettingViewModel>();
            services.AddTransient<GalgameSettingPage>();
            services.AddTransient<PlayedTimePage>();
            services.AddTransient<PlayedTimeViewModel>();
            services.AddTransient<LibraryViewModel>();
            services.AddTransient<LibraryPage>();
            services.AddTransient<GalgameFolderViewModel>();
            services.AddTransient<GalgameFolderPage>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<UpdateContentViewModel>();
            services.AddTransient<UpdateContentPage>();
            services.AddTransient<GalgameViewModel>();
            services.AddTransient<HomeDetailPage>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<HomePage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        UnhandledException += App_UnhandledException;
    }

    private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
        ContentDialog dialog = new()
        {
            XamlRoot = MainWindow.Content.XamlRoot,
            Title = "App_UnhandledException_Oops".GetLocalized(),
            PrimaryButtonText = "App_UnhandledException_BackToHome".GetLocalized(),
            CloseButtonText = "App_UnhandledException_Exit".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary
        };
        StackPanel stackPanel = new();
        stackPanel.Children.Add(new TextBlock()
        {
            Text = e.Exception.ToString(),
            TextWrapping = TextWrapping.WrapWholeWords
        });
        HyperlinkButton button = new()
        {
            Content = "App_UnhandledException_Hyperlink".GetLocalized(),
            NavigateUri = new Uri("https://github.com/GoldenPotato137/GalgameManager/issues/new/choose"),
        };
        button.Click += (_, _) =>
        {
            DataPackage package = new();
            package.SetText(e.Exception.ToString());
            Clipboard.SetContent(package);
        };
        stackPanel.Children.Add(button);
        dialog.Content = stackPanel;
        dialog.PrimaryButtonClick += (_, _) =>
        {
            GetService<INavigationService>().NavigateTo(typeof(HomeViewModel).FullName!);
        };
        dialog.CloseButtonClick += (_, _) => { Exit();};
        e.Handled = true;
        await dialog.ShowAsync();
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        //已知bug：args的参数永远是空string
        //见：https://github.com/microsoft/microsoft-ui-xaml/issues/3368
        await App.GetService<IActivationService>().ActivateAsync(Environment.GetCommandLineArgs().ToList());
    }
}
