//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Azure.AI.TextAnalytics;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Azure.Cognitive.Services.Speech.Samples
{
    /// <summary>
    /// Azure Function for processing the Audio files from Blob Storage and converting them to text.
    /// </summary>
    public static class SpeechToText
    {
        private static string _trancriptionSqlDatabaseConnectionString;
        private static Stopwatch _stopwatch;
        private static TextAnalyticsApiKeyCredential _textAnalyticsApiKeyCredential;
        private static Uri _textAnalyticsEndpoint;
        private static string _speechApiKeyCredential;
        private static string _speechApiRegion;
        private static ILogger _logger;

        /// <summary>
        /// Method called by the runtime when the function is triggered when the blob is placed in the container. 
        /// The BlobTrigger parameter specifies which container to monitor and the Connection the 
        /// connection string to the Blob storage that saved in the environment setting to use 
        /// to connect to the blob storage.
        /// </summary>
        /// <param name="blobStream"> The incoming blob file.</param>
        /// <param name="name">The name of the blob file.</param>
        /// <param name="log">Common logger used for the function.</param>
        /// <param name="context">The execution context.</param>
        [FunctionName("SpeechToText")]
        public static async Task Run([BlobTrigger("incoming/{name}", Connection = "AudioBlobConnectionString")]Stream blobStream, string name, ILogger log, ExecutionContext context)
        {
            _logger = log;

            // grab the local config values for testing
            var config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            _trancriptionSqlDatabaseConnectionString = config.GetConnectionString("TrancriptionSqlDatabaseConnectionString");
            _textAnalyticsApiKeyCredential = new TextAnalyticsApiKeyCredential(Environment.GetEnvironmentVariable("TextAnalyticsApiKeyCredential"));
            _textAnalyticsEndpoint = new Uri(Environment.GetEnvironmentVariable("TextAnalyticsEndpoint"));
            _speechApiKeyCredential = Environment.GetEnvironmentVariable("SpeechApiKeyCredential");
            _speechApiRegion = Environment.GetEnvironmentVariable("SpeechApiRegion");

            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            _logger.LogInformation($"{name}: Size: {blobStream.Length} Bytes");

            await RecognizeSpeechAsync(name, blobStream);
        }

        private static async Task RecognizeSpeechAsync(string blobName, Stream blobStream)
        {
            // Configure the subscription information for the service to access.
            // Use either key1 or key2 from the Speech Service resource you have created
            var config = SpeechConfig.FromSubscription(_speechApiKeyCredential, _speechApiRegion);

            // Setup the audio configuration, in this case, using a file that is in local storage.
            var tempfileName = Path.GetTempFileName();

            var fileStream = new FileStream(tempfileName, FileMode.Create, FileAccess.Write);
            blobStream.CopyTo(fileStream);
            fileStream.Dispose();

            var stopRecognition = new TaskCompletionSource<int>();

            // Setup the audio configuration, in this case, using a file that is in local storage.
            using var audioInput = AudioConfig.FromWavFileInput(tempfileName);

            // Pass the required parameters to the Speech Service which includes the configuration information
            // and the audio file name that you will use as input
            using var recognizer = new SpeechRecognizer(config, audioInput);

            StringBuilder sb = new StringBuilder();

            // set up the various event
            recognizer.SessionStarted += (s, e) =>
            {
                _logger.LogInformation($"{blobName}: Audio recognizing session started event.");
            };

            // append each Recognized to the buffer
            recognizer.Recognized += (s, e) =>
            {
                var result = e.Result;
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    sb.AppendLine(result.Text);
                }
            };

            // save the file results when the recognizer session ends
            recognizer.SessionStopped += (s, e) =>
            {
                _logger.LogInformation($"{blobName}: Audio recognizing session stopped event.");
                SentimentAnalysis(blobName, sb.ToString());
            };

            // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            // Waits for completion, Use Task.WaitAny to keep the task rooted.
            Task.WaitAny(new[] { stopRecognition.Task });

            // Stops recognition.
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

            // delete old file
            if (File.Exists(tempfileName))
            {
                File.Delete(tempfileName);
            }
        }

        private static void SentimentAnalysis(string blobName, string recognizedText)
        {
            TextAnalyticsClient client = new TextAnalyticsClient(_textAnalyticsEndpoint, _textAnalyticsApiKeyCredential);

            DocumentSentiment documentSentiment = client.AnalyzeSentiment(recognizedText);

            if (documentSentiment != null)
            {
                _logger.LogInformation($"{blobName}: Text sentiment: {documentSentiment.Sentiment}");

                //Extract Key keyPhrases
                Response<IReadOnlyCollection<string>> response = client.ExtractKeyPhrases(recognizedText);
                IEnumerable<string> keyPhrases = response.Value;
                StringBuilder keyPhrasesValues = new StringBuilder();
                foreach (string keyPhrase in keyPhrases)
                {
                    keyPhrasesValues.Append(keyPhrase);
                }

                SaveTrancription(blobName, recognizedText, documentSentiment.Sentiment.ToString(), keyPhrasesValues.ToString());

            }
            else
            {
                _logger.LogError($"{blobName}: Unable to process sentiment");
            }
        }

        private static void SaveTrancription(string blobName, string transcription, string sentiments, string keyPhrases)
        {
            // save to the database
            using var db = new TextTrancriptionContext(_trancriptionSqlDatabaseConnectionString);

            _stopwatch.Stop();

            db.Trancriptions.Add(new TextTrancription
            {
                CreationDate = DateTime.UtcNow,
                Name = blobName,
                Trancription = transcription,
                KeyPhrases = keyPhrases,
                Sentiments = sentiments,
                ProcessTime = TimeSpan.FromMilliseconds(_stopwatch.ElapsedMilliseconds).TotalSeconds
            }); ;

            _logger.LogInformation($"{blobName}: Saving text to database");

            db.SaveChanges();
        }
    }
}
