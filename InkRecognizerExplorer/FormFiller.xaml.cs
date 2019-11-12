using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace InkRecognizerExplorer
{
    public sealed partial class FormFiller : Page
    {
        private readonly DispatcherTimer inkRecoTimer;
        private readonly DispatcherTimer textToggleTimer;

        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        InkRecognizer inkRecognizer;
        InkResponse inkResponse;
        InkCanvas currentCanvas;

        Dictionary<string, Stack<InkStroke>> redoStacks;
        Dictionary<string, List<InkStroke>> clearedStrokesLists;
        InkToolbarToolButton activeTool;
        bool inkCleared = false;

        // Each form field and their contents have a "prefix" associated with them in their names to allow easy targeting of those elements
        private string[] prefixes = new string[]
        {
            "year",
            "make",
            "model",
            "license",
            "date",
            "time",
            "damage"
        };

        private Symbol TouchWriting = (Symbol)0xED5F;
        private Symbol Accept = (Symbol)0xE8FB;
        private Symbol Undo = (Symbol)0xE7A7;
        private Symbol Redo = (Symbol)0xE7A6;
        private Symbol ClearAll = (Symbol)0xE74D;
        private Symbol Car = (Symbol)0xE804;

        public FormFiller()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            redoStacks = new Dictionary<string, Stack<InkStroke>>();
            clearedStrokesLists = new Dictionary<string, List<InkStroke>>();
            activeTool = ballpointPen;

            // Set default ink color to blue
            ballpointPen.SelectedBrushIndex = 16;
            pencil.SelectedBrushIndex = 16;

            // Add event handlers and create redo stacks for each form field's ink canvases
            foreach (string prefix in prefixes)
            {
                var canvas = this.FindName($"{prefix}Canvas") as InkCanvas;
                canvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
                canvas.InkPresenter.StrokeInput.StrokeStarted += InkPresenter_StrokeInputStarted;
                canvas.InkPresenter.StrokeInput.StrokeEnded += InkPresenter_StrokeInputEnded;
                canvas.InkPresenter.StrokesErased += InkPresenter_StrokeErased;

                redoStacks.Add(prefix, new Stack<InkStroke>());
                clearedStrokesLists.Add(prefix, new List<InkStroke>());
            }

            // Timer created for ink recognition to happen after a set time period once a stroke ends
            inkRecoTimer = new DispatcherTimer();
            inkRecoTimer.Tick += InkRecoTimer_Tick;
            inkRecoTimer.Interval = TimeSpan.FromMilliseconds(350);

            // Timer created to switch from ink to text when user is idle in form field after a set time
            textToggleTimer = new DispatcherTimer();
            textToggleTimer.Tick += TextToggleTimer_Tick;
            textToggleTimer.Interval = TimeSpan.FromSeconds(3);
        }

        #region Event Handlers - Page
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // When the page is Unloaded, InkRecognizer is disposed. To preserve the state of the page we need to re-instantiate this object.
            string endpoint = localSettings.Values["InkRecognizerEndpoint"] as string;
            string apiKey = localSettings.Values["InkRecognizerApiKey"] as string;

            if (!Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
            {
                await new MessageDialog("Ensure you have a valid URI set in the Settings page.", "Invalid URI").ShowAsync();
            }
            else
            {
                inkRecognizer = new InkRecognizer(endpoint, apiKey);
            }

            base.OnNavigatedTo(e);
        }

        void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (inkRecognizer != null)
            {
                // Calling Dispose() on InkRecognizer to dispose of resources being used by HttpClient
                inkRecognizer.Dispose();
                inkRecognizer = null;
            }
        }
        #endregion

        #region Event Handlers - Canvas, Timer, Form Field
        private void InkPresenter_StrokeInputStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            inkRecoTimer.Stop();
            textToggleTimer.Stop();

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            clearedStrokesLists[prefix].Clear();
            inkCleared = false;

            activeTool = inkToolbar.ActiveTool;
        }

        private void InkPresenter_StrokeInputEnded(InkStrokeInput sender, PointerEventArgs args)
        {
            inkRecoTimer.Start();
            textToggleTimer.Start();
        }

        private void InkPresenter_StrokeErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            inkRecoTimer.Start();
            textToggleTimer.Start();

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            foreach (var stroke in args.Strokes)
            {
                redoStacks[prefix].Push(stroke);
            }

            var strokes = currentCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (strokes.Count == 0)
            {
                var result = this.FindName($"{prefix}Result") as TextBlock;
                result.Text = string.Empty;
            }
        }

        private async void InkRecoTimer_Tick(object sender, object e)
        {
            inkRecoTimer.Stop();

            if (inkRecognizer != null)
            {
                int index = currentCanvas.Name.IndexOf("Canvas");
                string prefix = currentCanvas.Name.Substring(0, index);

                // Get strokes of the currently selected ink canvas and convert them to JSON for the request
                var strokes = currentCanvas.InkPresenter.StrokeContainer.GetStrokes();
                inkRecognizer.ClearStrokes();
                inkRecognizer.AddStrokes(strokes);
                JObject json = inkRecognizer.ConvertInkToJson();

                // Recognize the strokes of the current ink canvas and convert the response JSON into an InkResponse
                var response = await inkRecognizer.RecognizeAsync(json);
                string responseString = await response.Content.ReadAsStringAsync();
                inkResponse = JsonConvert.DeserializeObject<InkResponse>(responseString);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = this.FindName($"{prefix}Result") as TextBlock;
                    result.Text = string.Empty;

                    foreach (var recoUnit in inkResponse.RecognitionUnits)
                    {
                        if (recoUnit.category == "line")
                        {
                            if (prefix == "damage")
                            {
                                result.Text += $"{recoUnit.recognizedText}\n";
                            }
                            else
                            {
                                result.Text += $"{recoUnit.recognizedText} ";
                            }
                        }
                    }
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound)
                    {
                        await new MessageDialog("Access denied due to invalid subscription key or wrong API endpoint. Make sure to provide a valid key for an active subscription and use a correct API endpoint in the Settings page.", $"Response Code: {inkResponse.Error.code}").ShowAsync();
                    }
                    else
                    {
                        await new MessageDialog(inkResponse.Error.message, $"Response Code: {inkResponse.Error.code}").ShowAsync();
                    }
                }
            }
        }

        private void TextToggleTimer_Tick(object sender, object e)
        {
            textToggleTimer.Stop();

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            ToggleFormFieldText(prefix);
        }

        private void FormField_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var formField = sender as Grid;
            string canvasPrefix = formField.Name;

            var canvas = this.FindName($"{canvasPrefix}Canvas") as InkCanvas;
            inkToolbar.TargetInkCanvas = canvas;
            currentCanvas = canvas;

            foreach (string prefix in prefixes)
            {
                ToggleFormFieldText(prefix);
            }

            ToggleFormFieldCanvas(canvasPrefix);
        }
        #endregion

        #region Event Handlers - Canvas and Toolbar Butttons
        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            textToggleTimer.Stop();

            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            ToggleFormFieldText(prefix);
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            inkRecoTimer.Start();
            textToggleTimer.Start();

            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            if (inkCleared)
            {
                foreach (var stroke in clearedStrokesLists[prefix])
                {
                    currentCanvas.InkPresenter.StrokeContainer.AddStroke(stroke.Clone());
                }

                clearedStrokesLists[prefix].Clear();
                inkCleared = false;
            }
            else if (activeTool is InkToolbarEraserButton)
            {
                RedoButton_Click(null, null);
            }
            else
            {
                var strokes = currentCanvas.InkPresenter.StrokeContainer.GetStrokes();
                if (strokes.Count > 0)
                {
                    var stroke = strokes[strokes.Count - 1];

                    redoStacks[prefix].Push(stroke);

                    stroke.Selected = true;
                    currentCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                }
            }
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            inkRecoTimer.Start();
            textToggleTimer.Start();

            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            if (redoStacks[prefix].Count > 0)
            {
                var stroke = redoStacks[prefix].Pop();

                currentCanvas.InkPresenter.StrokeContainer.AddStroke(stroke.Clone());
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as InkToolbarCustomToolButton;
            button.IsChecked = false;

            int index = currentCanvas.Name.IndexOf("Canvas");
            string prefix = currentCanvas.Name.Substring(0, index);

            var strokes = currentCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                clearedStrokesLists[prefix].Add(stroke);
            }

            inkCleared = true;
            currentCanvas.InkPresenter.StrokeContainer.Clear();

            var result = this.FindName($"{prefix}Result") as TextBlock;
            result.Text = string.Empty;
        }

        private void TouchButton_Click(object sender, RoutedEventArgs e)
        {
            if (touchButton.IsChecked == true)
            {
                foreach (string prefix in prefixes)
                {
                    var canvas = this.FindName($"{prefix}Canvas") as InkCanvas;
                    canvas.InkPresenter.InputDeviceTypes |= CoreInputDeviceTypes.Touch;
                }
            }
            else
            {
                foreach (string prefix in prefixes)
                {
                    var canvas = this.FindName($"{prefix}Canvas") as InkCanvas;
                    canvas.InkPresenter.InputDeviceTypes &= ~CoreInputDeviceTypes.Touch;
                }
            }
        }
        #endregion

        #region Helpers
        private void ToggleFormFieldText(string prefix)
        {
            var canvasGrid = this.FindName($"{prefix}CanvasGrid") as Grid;
            canvasGrid.Visibility = Visibility.Collapsed;

            var textResult = this.FindName($"{prefix}Result") as TextBlock;
            textResult.Visibility = Visibility.Visible;

            var formfield = this.FindName(prefix) as Grid;
            var color = GetColorFromHex("#1F1F1F");
            formfield.Background = new SolidColorBrush(color);
        }

        private void ToggleFormFieldCanvas(string prefix)
        {
            var textResult = this.FindName($"{prefix}Result") as TextBlock;
            textResult.Visibility = Visibility.Collapsed;

            var canvasGrid = this.FindName($"{prefix}CanvasGrid") as Grid;
            canvasGrid.Visibility = Visibility.Visible;

            var formfield = this.FindName(prefix) as Grid;
            var color = GetColorFromHex("#2B2B2B");
            formfield.Background = new SolidColorBrush(color);
        }

        private Color GetColorFromHex(string hexCode)
        {
            byte a = 255;
            byte r = Convert.ToByte(hexCode.Substring(1, 2), 16);
            byte g = Convert.ToByte(hexCode.Substring(3, 2), 16);
            byte b = Convert.ToByte(hexCode.Substring(5, 2), 16);

            return Color.FromArgb(a, r, g, b);
        }
        #endregion
    }
}
