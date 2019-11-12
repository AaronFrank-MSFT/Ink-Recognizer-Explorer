using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkRecognizerExplorer
{
    public class InkResponse
    {
        public List<InkRecognitionUnit> RecognitionUnits { get; set; }
        public Error Error { get; set; }
    }

    public class Error
    {
        public string code { get; set; }
        public string message { get; set; }
    }
}
