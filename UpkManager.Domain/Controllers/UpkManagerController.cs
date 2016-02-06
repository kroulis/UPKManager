﻿using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using STR.Common.Messages;

using STR.MvvmCommon;
using STR.MvvmCommon.Contracts;

using UpkManager.Domain.ViewModels;


namespace UpkManager.Domain.Controllers {

  [Export(typeof(IController))]
  public class UpkManagerController : IController {

    #region Private Fields

    private bool isStartupComplete;

    private readonly UpkManagerViewModel   viewModel;
    private readonly MainMenuViewModel menuViewModel;

    private readonly IMessenger messenger;

    #endregion Private Fields

    #region Constructor

    [ImportingConstructor]
    public UpkManagerController(UpkManagerViewModel ViewModel, MainMenuViewModel MenuViewModel, IMessenger Messenger) {
      if (Application.Current != null) Application.Current.DispatcherUnhandledException += onCurrentDispatcherUnhandledException;

      AppDomain.CurrentDomain.UnhandledException += onDomainUnhandledException;

      Dispatcher.CurrentDispatcher.UnhandledException += onCurrentDispatcherUnhandledException;

      TaskScheduler.UnobservedTaskException += onUnobservedTaskException;

      System.Windows.Forms.Application.ThreadException += onThreadException;

          viewModel =     ViewModel;
      menuViewModel = MenuViewModel;

      messenger = Messenger;

      registerCommands();
    }

    #endregion Constructor

    #region Commands

    private void registerCommands() {
      viewModel.Loaded = new RelayCommandAsync<RoutedEventArgs>(onLoadedExecuteAsync);

      viewModel.Closing = new RelayCommand<CancelEventArgs>(onClosingExecute);

      menuViewModel.Exit = new RelayCommand(onExitExecute);
    }

    private async Task onLoadedExecuteAsync(RoutedEventArgs args) {
      isStartupComplete = true;

      await messenger.SendAsync(new ApplicationLoadedMessage());
    }

    private void onClosingExecute(CancelEventArgs args) {
      ApplicationClosingMessage message = new ApplicationClosingMessage();

      messenger.Send(message);

      args.Cancel = message.Cancel;
    }

    private void onExitExecute() {
      ApplicationClosingMessage message = new ApplicationClosingMessage();

      messenger.Send(message);

      if (!message.Cancel) Application.Current.Shutdown();
    }

    #endregion Commands

    #region Private Methods

    private void onDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) {
      Exception ex = e.ExceptionObject as Exception;

      if (ex != null) {
        if (e.IsTerminating) MessageBox.Show(ex.Message, "Fatal Domain Unhandled Exception");
        else messenger.SendUi(new ApplicationErrorMessage { ErrorMessage = "Domain Unhandled Exception", Exception = ex });
      }
    }

    private void onCurrentDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
      if (e.Exception != null) {
        if (isStartupComplete) {
          messenger.SendUi(new ApplicationErrorMessage { ErrorMessage = "Dispatcher Unhandled Exception", Exception = e.Exception });

          e.Handled = true;
        }
        else {
          e.Handled = true;

          MessageBox.Show(e.Exception.Message, "Fatal Startup Exception");

          Application.Current.Shutdown();
        }
      }
    }

    private void onUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
      if (e.Exception == null || e.Exception.InnerExceptions.Count == 0) return;

      foreach(Exception ex in e.Exception.InnerExceptions) {
        if (isStartupComplete) {
          messenger.SendUi(new ApplicationErrorMessage { ErrorMessage = "Unobserved Task Exception", Exception = ex });
        }
        else {
          MessageBox.Show(ex.Message, "Fatal Startup Exception");
        }
      }

      if (!isStartupComplete) Application.Current.Shutdown();

      e.SetObserved();
    }

    private void onThreadException(object sender, ThreadExceptionEventArgs e) {
      if (e.Exception == null) return;

      messenger.SendUi(new ApplicationErrorMessage { ErrorMessage = "Thread Exception", Exception = e.Exception });
    }

    #endregion Private Methods

  }

}
