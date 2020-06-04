using ClassTranscribeDatabase.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTCommons.MSTranscription
{
    public class Languages
    {
        public static string ENGLISH = "en-US";
        public static string SIMPLIFIED_CHINESE = "zh-Hans";
        public static string KOREAN = "ko";
        public static string SPANISH = "es";
        public static string FRENCH = "fr";
    }
    public class MSTranscriptionService
    {
        
        public MSTranscriptionService()
        {
        }
        public async Task<Tuple<Dictionary<string, List<Caption>>, string>> RecognitionWithAudioStreamAsync(string file, string apikey, string region)
        {
            
            SpeechTranslationConfig _speechConfig = SpeechTranslationConfig.FromSubscription(apikey, region);
            _speechConfig.RequestWordLevelTimestamps();
            // Sets source and target languages.
            _speechConfig.SpeechRecognitionLanguage = Languages.ENGLISH;
            _speechConfig.AddTargetLanguage(Languages.SIMPLIFIED_CHINESE);
            _speechConfig.AddTargetLanguage(Languages.KOREAN);
            _speechConfig.AddTargetLanguage(Languages.SPANISH);
            _speechConfig.AddTargetLanguage(Languages.FRENCH);
            _speechConfig.OutputFormat = OutputFormat.Detailed;

            string errorCode = "";
            Console.OutputEncoding = Encoding.Unicode;
            Dictionary<string, List<Caption>> captions = new Dictionary<string, List<Caption>>
            {
                { Languages.ENGLISH, new List<Caption>() },
                { Languages.SIMPLIFIED_CHINESE, new List<Caption>() },
                { Languages.KOREAN, new List<Caption>() },
                { Languages.SPANISH, new List<Caption>() },
                { Languages.FRENCH, new List<Caption>() }
            };

            
            var stopRecognition = new TaskCompletionSource<int>();
            // Create an audio stream from a wav file.
            // Replace with your own audio file name.
            using (var audioInput = Helper.OpenWavFile(file))
            {
                // Creates a speech recognizer using audio stream input.
                using (var recognizer = new TranslationRecognizer(_speechConfig, audioInput))
                {
                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.TranslatedSpeech)
                        {
                            JObject jObject = JObject.Parse(e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult));
                            var wordLevelCaptions = jObject["Words"]
                            .ToObject<List<MSTWord>>()
                            .OrderBy(w => w.Offset)
                            .ToList();

                            if (wordLevelCaptions.Any())
                            {
                                var offsetDifference = e.Result.OffsetInTicks - wordLevelCaptions.FirstOrDefault().Offset;
                                wordLevelCaptions.ForEach(w => w.Offset += offsetDifference);
                            }

                            var sentenceLevelCaptions = MSTWord.WordLevelTimingsToSentenceLevelTimings(e.Result.Text, wordLevelCaptions);

                            TimeSpan offset = new TimeSpan(e.Result.OffsetInTicks);
                            Console.WriteLine($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}", offset);
                            TimeSpan end = e.Result.Duration.Add(offset);
                            Console.WriteLine($"End={end.Minutes}:{end.Seconds},{end.Milliseconds}");
                            MSTWord.AppendCaptions(captions[Languages.ENGLISH], sentenceLevelCaptions);

                            foreach (var element in e.Result.Translations)
                            {
                                Caption.AppendCaptions(captions[element.Key], offset, end, element.Value);
                            }
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        errorCode = e.ErrorCode.ToString();
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");
                            if (e.ErrorCode == CancellationErrorCode.AuthenticationFailure)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");
                                //_slackLogger.PostErrorAsync(new Exception($"Transcription Failure, Authentication failure, VideoId {audioFile.Id}"),
                                //    $"Transcription Failure, Authentication failure, VideoId {audioFile.Id}").GetAwaiter().GetResult();
                            }
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine("\nSession started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\nSession stopped event.");
                        Console.WriteLine("\nStop recognition.");
                        stopRecognition.TrySetResult(0);
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    return new Tuple<Dictionary<string, List<Caption>>, string>(captions, errorCode);
                }
            }
            // </recognitionAudioStream>
        }
    }
}
