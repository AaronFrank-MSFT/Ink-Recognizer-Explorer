using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace InkRecognizerExplorer
{
    public sealed partial class MainPage : Page
    {
        private const string defaultInkRecognizerEndpoint = "https://api.cognitive.microsoft.com";
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public MainPage()
        {
            this.InitializeComponent();

            string endpoint = localSettings.Values["InkRecognizerEndpoint"] as string;
            if (string.IsNullOrEmpty(endpoint))
            {
                localSettings.Values["InkRecognizerEndpoint"] = defaultInkRecognizerEndpoint;
            }

            frame.SourcePageType = typeof(InkMirror);
            navView.SelectedItem = navView.MenuItems[0];
            commandBar.Text = "Ink Recognizer Explorer - Ink Mirror";
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var item = sender.SelectedItem as NavigationViewItem;

            switch (item.Name)
            {
                case "inkMirror":
                    frame.Navigate(typeof(InkMirror));
                    commandBar.Text = "Ink Recognizer Explorer - Ink Mirror";
                    break;
                case "formFiller":
                    frame.Navigate(typeof(FormFiller));
                    commandBar.Text = "Ink Recognizer Explorer - Form Filler";
                    break;
                default:
                    frame.Navigate(typeof(SettingsPage));
                    commandBar.Text = "Ink Recognizer Explorer - Settings";
                    break;
            }
        }
    }
}
