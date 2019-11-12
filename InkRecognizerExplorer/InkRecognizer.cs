using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.UI.Input.Inking;

namespace InkRecognizerExplorer
{
    public class InkRecognizer : IDisposable
    {
        private const string inkRecognizerApiRoute = "/inkrecognizer/v1.0-preview/recognize";
        private HttpClient httpClient;
        private IDictionary<uint, InkStroke> StrokeMap { get; set; }
        private string LanguageCode;

        public InkRecognizer(string endpoint, string apiKey)
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(endpoint);
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            this.StrokeMap = new Dictionary<uint, InkStroke>();
            this.LanguageCode = "en-US";
        }

        public void AddStrokes(IReadOnlyList<InkStroke> strokes)
        {
            foreach (var stroke in strokes)
            {
                this.StrokeMap[stroke.Id] = stroke;
            }
        }

        public void ClearStrokes()
        {
            this.StrokeMap.Clear();
        }

        public void SetLanguage(string languageCode)
        {
            this.LanguageCode = languageCode;
        }

        public float GetDipsPerMm(float dpi)
        {
            float dipsPerMm = dpi / 25.4f;

            return dipsPerMm;
        }

        public JObject ConvertInkToJson()
        {
            // For demo purposes and keeping the initially loaded ink consistent a value of 96 for DPI was used
            // For production, it is most likely better to use the device's DPI when generating the request JSON and an example of that is below
            // var displayInformation = DisplayInformation.GetForCurrentView();
            // float dpi = displayInformation.LogicalDpi;
            // float dipsPerMm = GetDipsPerMm(dpi);

            float dipsPerMm = GetDipsPerMm(96);

            var payload = new JObject();
            var strokesArray = new JArray();

            foreach (InkStroke stroke in StrokeMap.Values)
            {
                var jStroke = new JObject();
                IReadOnlyList<InkPoint> pointsCollection = stroke.GetInkPoints();
                Matrix3x2 transform = stroke.PointTransform;

                jStroke["id"] = stroke.Id;

                if (pointsCollection.Count >= 2)
                {
                    var points = new StringBuilder();
                    for (int i = 0; i < pointsCollection.Count; i++)
                    {
                        var transformedPoint = Vector2.Transform(new Vector2((float)pointsCollection[i].Position.X, (float)pointsCollection[i].Position.Y), transform);
                        double x = transformedPoint.X / dipsPerMm;
                        double y = transformedPoint.Y / dipsPerMm;
                        points.Append($"{x},{y}");
                        if (i != pointsCollection.Count - 1)
                        {
                            points.Append(",");
                        }
                    }

                    jStroke["points"] = points.ToString();
                    strokesArray.Add(jStroke);
                }
            }

            payload["version"] = 1.0;
            payload["language"] = this.LanguageCode;
            payload["strokes"] = strokesArray;

            return payload;
        }

        public async Task<HttpResponseMessage> RecognizeAsync(JObject json)
        {
            string payload = json.ToString();
            var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = await httpClient.PutAsync(inkRecognizerApiRoute, httpContent);

            return httpResponse;
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
