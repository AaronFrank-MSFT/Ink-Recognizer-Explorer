using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace InkRecognizerExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public SettingsPage()
        {
            this.InitializeComponent();

            string endpoint = localSettings.Values["InkRecognizerEndpoint"] as string;
            endpointField.Text = endpoint;

            string apiKey = localSettings.Values["InkRecognizerApiKey"] as string;
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKeyField.Password = string.Empty;
            }
        }

        private void EndpointField_TextChanged(object sender, TextChangedEventArgs e)
        {
            var endpointField = sender as TextBox;
            string newEndpoint = endpointField.Text;

            localSettings.Values["InkRecognizerEndpoint"] = newEndpoint;
        }

        private void ApiKeyField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var apiKeyField = sender as PasswordBox;
            string newKey = apiKeyField.Password;

            localSettings.Values["InkRecognizerApiKey"] = newKey;
        }
    }
}
