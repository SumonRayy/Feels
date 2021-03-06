﻿using Feels.Services;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Windows.Services.Store;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Feels.Views {
    public sealed partial class AchievementsPage : Page
    {

        #region navigation

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;
            base.OnNavigatedTo(e);
        }

        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        #endregion navigation

        #region titlebar
        private void InitializeTitleBar() {
            App.DeviceType = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;

            if (App.DeviceType == "Windows.Mobile") {
                return;
            }

            Window.Current.Activated += Current_Activated;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            TitleBar.Height = coreTitleBar.Height;
            Window.Current.SetTitleBar(MainTitleBar);

            coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
        }

        void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar titleBar, object args) {
            TitleBar.Visibility = titleBar.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args) {
            TitleBar.Height = sender.Height;
            RightMask.Width = sender.SystemOverlayRightInset;
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e) {
            if (e.WindowActivationState != CoreWindowActivationState.Deactivated) {
                MainTitleBar.Opacity = 1;
            } else {
                MainTitleBar.Opacity = 0.5;
            }
        }

        #endregion titlebar

        public AchievementsPage()
        {
            InitializeComponent();
            InitializeTitleBar();
            InitializeData();
        }

        #region animations

        private void InitializePageAnimation() {
            TransitionCollection collection = new TransitionCollection();
            NavigationThemeTransition theme = new NavigationThemeTransition();

            var info = new SlideNavigationTransitionInfo();

            theme.DefaultNavigationTransitionInfo = info;
            collection.Add(theme);
            Transitions = collection;
        }
        #endregion animations

        #region data
        private void ShowInAppPurchasesLoadingView() {
            DonationsLoadingView.Visibility = Visibility.Visible;
            ProgressLoadingInAppPurchases.Visibility = Visibility.Visible;
        }

        private void HideInAppPurchasesLoadingView() {
            DonationsLoadingView.Visibility = Visibility.Collapsed;
            ProgressLoadingInAppPurchases.Visibility = Visibility.Collapsed;
        }

        private async void InitializeData() {
            ShowInAppPurchasesLoadingView();

            var queryResult = await InAppPurchases.GetAllAddons();

            if (queryResult.ExtendedError != null) {
                return;
            }

            var addonsList = new List<StoreProduct>();

            foreach (KeyValuePair<string, StoreProduct> item in queryResult.Products) {
                StoreProduct product = item.Value;
                addonsList.Add(product);
            }

            HideInAppPurchasesLoadingView();

            UnlocksListView.ItemsSource = addonsList;

            // TODO: move to check entitlement
            var res = await InAppPurchases.GetUserAddons();
            foreach (KeyValuePair<string, StoreProduct> item in res.Products) {
                StoreProduct product = item.Value;
            }

        }

        #endregion data

        #region events

        private void Addon_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            var item = (Grid)sender;
            var product = (StoreProduct)item.DataContext;

            Purchase(product.StoreId);
        }

        #endregion events

        #region others

        private async void Purchase(string id) {
            var result = await InAppPurchases.PurchaseAddon(id);

            string extendedError = string.Empty;
            string descriptionError = string.Empty;

            if (result.ExtendedError != null) {
                extendedError = "ExtendedError: " + result.ExtendedError.Message;
            }

            //var res1 = await InAppPurchases.GetRemainingBalance(id);

            switch (result.Status) {
                case StorePurchaseStatus.AlreadyPurchased:
                    descriptionError = App.ResourceLoader.GetString("PurchaseStatusAlreadyPurchased");
                    //InAppPurchases.ConsumeAddon(id);
                    break;

                case StorePurchaseStatus.Succeeded:
                    descriptionError = App.ResourceLoader.GetString("PurchaseStatusSucceeded");
                    //InAppPurchases.ConsumeAddon(id);
                    //var res2 = await InAppPurchases.GetRemainingBalance(id);
                    break;

                case StorePurchaseStatus.NotPurchased:
                    descriptionError = string.Format("{0}. {1}", 
                        App.ResourceLoader.GetString("PurchaseStatusNotPurchased"), 
                        extendedError);
                    break;

                case StorePurchaseStatus.NetworkError:
                    descriptionError = string.Format("{0}. {1}", 
                        App.ResourceLoader.GetString("PurchaseStatusNetworkError"), 
                        extendedError);
                    break;

                case StorePurchaseStatus.ServerError:
                    descriptionError = string.Format("{0}. {1}", 
                        App.ResourceLoader.GetString("PurchaseStatusServerError"), 
                        extendedError);
                    break;

                default:
                    descriptionError = string.Format("{0}. {1}", 
                        App.ResourceLoader.GetString("PurchaseStatusUknownError"), 
                        extendedError);
                    break;
            }

            DataTransfer.ShowLocalToast(descriptionError);
        }

        #endregion others

        private void PagePivot_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var pivot = (Pivot)sender;

            switch (pivot.SelectedIndex) {
                case 1:
                    int calls = App.DataSource.Client.ApiCallsMade != null ? 
                        (int)App.DataSource.Client.ApiCallsMade : 0;

                    ProgressAPICalls.Value = calls;

                    var dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
                    UI.AnimateNumericValue(calls, APICallsMadeValue, dispatcher, "/1000", 100);

                    break;
                default:
                    break;
            }
        }
    }
}
