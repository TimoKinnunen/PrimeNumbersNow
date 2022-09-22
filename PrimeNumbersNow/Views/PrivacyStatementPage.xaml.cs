using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace PrimeNumbersNow.Views
{
    public sealed partial class PrivacyStatementPage : Page
    {
        private readonly MainPage mainPage;

        public PrivacyStatementPage()
        {
            InitializeComponent();

            SizeChanged += PrivacyStatementPage_SizeChanged;

            Loaded += PrivacyStatementPage_Loaded;

            mainPage = MainPage.CurrentMainPage;
        }

        private void PrivacyStatementPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetPageContentStackPanelWidth();
        }

        private void PrivacyStatementPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetPageContentStackPanelWidth();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            // code here
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here
            // code here
        }

        private void SetPageContentStackPanelWidth()
        {
            PageContentStackPanel.Width = ActualWidth -
                PageContentScrollViewer.Margin.Left -
                PageContentScrollViewer.Padding.Right;
        }

        #region MenuAppBarButton
        private void HomeAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainPage.GoToHomePage();
        }
        #endregion MenuAppBarButton
    }
}
