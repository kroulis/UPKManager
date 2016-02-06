﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using STR.Common.Extensions;

using STR.MvvmCommon.Contracts;

using UpkManager.Domain.Messages.FileHeader;
using UpkManager.Domain.Messages.HeaderTables;
using UpkManager.Domain.Models.Compression;
using UpkManager.Domain.ViewModels;


namespace UpkManager.Domain.Controllers {

  [Export(typeof(IController))]
  public class HeaderTablesController : IController {

    #region Private Fields

    private readonly HeaderTablesViewModel viewModel;
    private readonly MainMenuViewModel menuViewModel;

    private readonly IMessenger messenger;

    #endregion Private Fields

    #region Constructor

    [ImportingConstructor]
    public HeaderTablesController(HeaderTablesViewModel ViewModel, MainMenuViewModel MenuViewModel, IMessenger Messenger) {
          viewModel = ViewModel;
      menuViewModel = MenuViewModel;

      messenger = Messenger;

      viewModel.Blocks = new ObservableCollection<DomainCompressedChunkBlock>();

      menuViewModel.PropertyChanged += onMenuViewModelChanged;

      registerMessages();
    }

    #endregion Constructor

    #region Messages

    private void registerMessages() {
      messenger.Register<FileHeaderLoadedMessage>(this, onFileHeaderLoaded);
    }

    private void onFileHeaderLoaded(FileHeaderLoadedMessage message) {
      if (viewModel.Chunks != null && viewModel.Chunks.Any()) {
        viewModel.Chunks.ForEach(chunk => chunk.PropertyChanged -= onChunkPropertyChanged);
      }

      viewModel.Generations = message.FileHeader.Generations;
      viewModel.Chunks      = message.FileHeader.CompressedChunks;

      viewModel.Blocks.Clear();

      if (viewModel.Chunks != null && viewModel.Chunks.Any()) {
        viewModel.Chunks.ForEach(chunk => chunk.PropertyChanged += onChunkPropertyChanged);
      }
    }

    private void onChunkPropertyChanged(object sender, PropertyChangedEventArgs e) {
      DomainCompressedChunk chunk = sender as DomainCompressedChunk;

      if (chunk == null) return;

      switch(e.PropertyName) {
        case "IsSelected": {
          if (chunk.IsSelected) {
            if (viewModel.Blocks.Any()) {
              viewModel.Blocks.ForEach(block => block.PropertyChanged -= onBlockPropertyChanged);
            }

            viewModel.Blocks = chunk.Header.Blocks;

            if (viewModel.Blocks.Any()) {
              viewModel.Blocks.ForEach(block => block.PropertyChanged += onBlockPropertyChanged);
            }
          }

          break;
        }
        default: {
          break;
        }
      }
    }

    private async void onBlockPropertyChanged(object sender, PropertyChangedEventArgs e) {
      DomainCompressedChunkBlock block = sender as DomainCompressedChunkBlock;

      if (block == null) return;

      switch(e.PropertyName) {
        case "IsSelected": {
          if (block.IsSelected) await messenger.SendAsync(new CompressedBlockSelectedMessage { Block = block });

          break;
        }
        default: {
          break;
        }
      }
    }

    #endregion Messages

    #region Private Methods

    private void onMenuViewModelChanged(object sender, PropertyChangedEventArgs e) {
      switch(e.PropertyName) {
        case "IsViewRawData": {
          viewModel.IsViewCleanData = !viewModel.IsViewCleanData;
          viewModel.IsViewRawData   = !viewModel.IsViewRawData;

          break;
        }
        default: {
          break;
        }
      }
    }

    #endregion Private Methods

  }

}
