﻿using System.Collections.ObjectModel;
using NETworkManager.Controls;
using Dragablz;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Input;
using NETworkManager.Views.Dialogs;
using System.Windows;
using NETworkManager.ViewModels.Network;
using NETworkManager.Models.Settings;
using System.ComponentModel;
using System.Windows.Data;
using System;

namespace NETworkManager.ViewModels.Applications
{
    public class RemoteDesktopViewModel : ViewModelBase
    {
        #region Variables
        private IDialogCoordinator dialogCoordinator;

        public IInterTabClient InterTabClient { get; private set; }
        public ObservableCollection<DragablzTabContent> TabContents { get; private set; }

        private bool _isLoading = true;

        private bool _hideWindowsFormsHost;
        public bool HideWindowsFormsHost
        {
            get { return _hideWindowsFormsHost; }
            set
            {
                if (value == _hideWindowsFormsHost)
                    return;

                _hideWindowsFormsHost = value;
                OnPropertyChanged();
            }
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                if (value == _selectedTabIndex)
                    return;

                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        #region Sessions
        ICollectionView _remoteDesktopSessions;
        public ICollectionView RemoteDesktopSessions
        {
            get { return _remoteDesktopSessions; }
        }

        private RemoteDesktopSessionInfo _selectedSession = new RemoteDesktopSessionInfo();
        public RemoteDesktopSessionInfo SelectedSession
        {
            get { return _selectedSession; }
            set
            {
                if (value == _selectedSession)
                    return;

                _selectedSession = value;
                OnPropertyChanged();
            }
        }

        private bool _expandSessionView;
        public bool ExpandSessionView
        {
            get { return _expandSessionView; }
            set
            {
                if (value == _expandSessionView)
                    return;

                if (!_isLoading)
                    SettingsManager.Current.RemoteDesktop_ExpandSessionView = value;

                _expandSessionView = value;
                OnPropertyChanged();
            }
        }
        #endregion
        #endregion

        #region Constructor
        public RemoteDesktopViewModel(IDialogCoordinator instance)
        {
            dialogCoordinator = instance;

            InterTabClient = new DragablzMainInterTabClient();
            TabContents = new ObservableCollection<DragablzTabContent>();

            // Load sessions
            if (RemoteDesktopSessionManager.Sessions == null)
                RemoteDesktopSessionManager.Load();

            _remoteDesktopSessions = CollectionViewSource.GetDefaultView(RemoteDesktopSessionManager.Sessions);
            _remoteDesktopSessions.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            LoadSettings();

            _isLoading = false;
        }

        private void LoadSettings()
        {
            ExpandSessionView = SettingsManager.Current.RemoteDesktop_ExpandSessionView;
        }

        public void OnShutdown()
        {
            if (RemoteDesktopSessionManager.SessionsChanged)
                RemoteDesktopSessionManager.Save();
        }
        #endregion

        #region Methods
        private void ConnectSession(Models.RemoteDesktop.RemoteDesktopSessionInfo remoteDesktopSessionInfo)
        {
            TabContents.Add(new DragablzTabContent(remoteDesktopSessionInfo.Hostname, new RemoteDesktopControl(remoteDesktopSessionInfo)));
            SelectedTabIndex = TabContents.Count - 1;
        }
        #endregion

        #region ICommand & Actions
        public ICommand ConnectRemoteDesktopSessionCommand
        {
            get { return new RelayCommand(p => ConnectRemoteDesktopSessionAction()); }
        }

        private async void ConnectRemoteDesktopSessionAction()
        {
            CustomDialog customDialog = new CustomDialog()
            {
                Title = Application.Current.Resources["String_Header_ConnectRemoteDesktopConnection"] as string
            };

            ConnectRemoteDesktopSessionViewModel connectRemoteDesktopSessionViewModel = new ConnectRemoteDesktopSessionViewModel(instance =>
            {
                dialogCoordinator.HideMetroDialogAsync(this, customDialog);
                HideWindowsFormsHost = false;

                Models.RemoteDesktop.RemoteDesktopSessionInfo remoteDesktopSessionInfo = new Models.RemoteDesktop.RemoteDesktopSessionInfo
                {
                    Hostname = instance.Hostname
                };

                ConnectSession(remoteDesktopSessionInfo);
            }, instance =>
            {
                dialogCoordinator.HideMetroDialogAsync(this, customDialog);
                HideWindowsFormsHost = false;
            });

            customDialog.Content = new ConnectRemoteDesktopSessionDialog
            {
                DataContext = connectRemoteDesktopSessionViewModel
            };

            // This will fix airpace problem in winform-wpf
            HideWindowsFormsHost = true;

            await dialogCoordinator.ShowMetroDialogAsync(this, customDialog);
        }

        public ICommand AddSessionCommand
        {
            get { return new RelayCommand(p => AddSessionAction()); }
        }

        private async void AddSessionAction()
        {
            CustomDialog customDialog = new CustomDialog()
            {
                Title = Application.Current.Resources["String_Header_AddRemoteDesktopConnection"] as string
            };

            RemoteDesktopSessionViewModel remoteDesktopSessionViewModel = new RemoteDesktopSessionViewModel(instance =>
            {
                dialogCoordinator.HideMetroDialogAsync(this, customDialog);
                HideWindowsFormsHost = false;

                RemoteDesktopSessionInfo remoteDesktopSessionInfo = new RemoteDesktopSessionInfo
                {
                    Name = instance.Name,
                    Hostname = instance.Hostname
                };

                RemoteDesktopSessionManager.AddSession(remoteDesktopSessionInfo);
            }, instance =>
            {
                dialogCoordinator.HideMetroDialogAsync(this, customDialog);
                HideWindowsFormsHost = false;
            });

            customDialog.Content = new RemoteDesktopSessionDialog
            {
                DataContext = remoteDesktopSessionViewModel
            };

            // This will fix airpace problem in winform-wpf
            HideWindowsFormsHost = true;

            await dialogCoordinator.ShowMetroDialogAsync(this, customDialog);
        }

        public ICommand ConnectSessionCommand
        {
            get { return new RelayCommand(p => ConnectSessionAction()); }
        }

        private void ConnectSessionAction()
        {
            Models.RemoteDesktop.RemoteDesktopSessionInfo remoteDesktopSessionInfo = new Models.RemoteDesktop.RemoteDesktopSessionInfo
            {
                Hostname = SelectedSession.Hostname
            };

            ConnectSession(remoteDesktopSessionInfo);
        }

        public ICommand EditSessionCommand
        {
            get { return new RelayCommand(p => EditSessionAction()); }
        }

        private async void EditSessionAction()
        {
            CustomDialog customDialog = new CustomDialog()
            {
                Title = Application.Current.Resources["String_Header_EditRemoteDesktopConnection"] as string
            };

            RemoteDesktopSessionViewModel remoteDesktopSessionViewModel = new RemoteDesktopSessionViewModel(instance =>
            {
                dialogCoordinator.HideMetroDialogAsync(this, customDialog);
                HideWindowsFormsHost = false;

                // Don't delete and add if nothing changed...
                if ((instance.Name == SelectedSession.Name) && (instance.Hostname == SelectedSession.Hostname))
                    return;

                RemoteDesktopSessionManager.RemoveSession(SelectedSession);

                RemoteDesktopSessionInfo remoteDesktopSessionInfo = new RemoteDesktopSessionInfo
                {
                    Name = instance.Name,
                    Hostname = instance.Hostname
                };

                RemoteDesktopSessionManager.AddSession(remoteDesktopSessionInfo);
            }, instance =>
            {
                dialogCoordinator.HideMetroDialogAsync(this, customDialog);
                HideWindowsFormsHost = false;
            }, SelectedSession);

            customDialog.Content = new RemoteDesktopSessionDialog
            {
                DataContext = remoteDesktopSessionViewModel
            };

            // This will fix airpace problem in winform-wpf
            HideWindowsFormsHost = true;

            await dialogCoordinator.ShowMetroDialogAsync(this, customDialog);
        }

        public ICommand DeleteSessionCommand
        {
            get { return new RelayCommand(p => DeleteSessionAction()); }
        }

        private async void DeleteSessionAction()
        {
            MetroDialogSettings settings = AppearanceManager.MetroDialog;

            settings.AffirmativeButtonText = Application.Current.Resources["String_Button_Delete"] as string;
            settings.NegativeButtonText = Application.Current.Resources["String_Button_Cancel"] as string;

            settings.DefaultButtonFocus = MessageDialogResult.Affirmative;

            if (MessageDialogResult.Negative == await dialogCoordinator.ShowMessageAsync(this, Application.Current.Resources["String_Header_AreYouSure"] as string, Application.Current.Resources["String_DeleteSessionMessage"] as string, MessageDialogStyle.AffirmativeAndNegative, settings))
                return;

            RemoteDesktopSessionManager.RemoveSession(SelectedSession);
        }
        #endregion
    }
}