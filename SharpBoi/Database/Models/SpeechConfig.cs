using Google.Cloud.TextToSpeech.V1;

namespace SharpBoi.Database.Models
{
    public class SpeechConfig
    {
        public string VoiceName { get; set; }
        public string LangCode { get; set; }
        public SsmlVoiceGender Gender { get; set; }
    }
}
