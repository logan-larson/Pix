
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Principal;

class Program
{
    static void OutputSpeechRecognitionResult(SpeechRecognitionResult speechRecognitionResult)
    {
        switch (speechRecognitionResult.Reason)
        {
            case ResultReason.RecognizedSpeech:
                Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                break;
            case ResultReason.NoMatch:
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                break;
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
        }
    }

    static void OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
    {
        switch (speechSynthesisResult.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                Console.WriteLine($"Speech synthesized for text: [{text}]");
                break;
            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
            default:
                break;
        }
    }

    static async Task<string> GetGptResponseAsync(List<Message> conversation, string gptBearer)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {gptBearer}");

            var payload = new
            {
                model = "gpt-3.5-turbo", // or another model
                messages = conversation
            };

            var body = JsonConvert.SerializeObject(payload);

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions",
                new StringContent(body, Encoding.UTF8, "application/json"));

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            return json["choices"]?.First?["message"]?["content"]?.ToString() ?? "I'm sorry, no response was provided.";
        }
    }

    async static Task Main(string[] args)
    {
        // This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"
        string? speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
        string? speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");
        string? gptBearer = Environment.GetEnvironmentVariable("GPT_BEARER");

        if (speechKey is null || speechRegion is null || gptBearer is null)
        {
            Console.WriteLine("You need to set the SPEECH_KEY, SPEECH_REGION, and GPT_BEARER environment variables.");
            return;
        }

        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechRecognitionLanguage = "en-US";
        speechConfig.SpeechSynthesisVoiceName = "en-US-ChristopherNeural";

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
        using var speechSynthesizer = new SpeechSynthesizer(speechConfig);
        using var keywordRecognizer = new KeywordRecognizer(audioConfig);

        //var keywordModel = KeywordRecognitionModel.FromFile("HeyPixKeyword.table");

        bool isRunning = true;

        //State state = State.WaitForName;

        keywordRecognizer.Recognized += (s, e) =>
        {
            if (e.Result.Text == "Hey Pix")
            {
                Console.WriteLine("Listening...");
                isRunning = true;
            }
        };

        List<Message> messages = new List<Message>();

        messages.Add(new Message("system", "You are a helpful assistant who forms his responses in a conversation-like fashion."));

        while (isRunning)
        {
            // TODO: Add more complex state machine logic here

            // Wait for user to say "Hey Pix"
            //Console.WriteLine("Say 'Hey Pix' to wake up.");
            //await keywordRecognizer.RecognizeOnceAsync(keywordModel);

            // If the user said "Hey Pix", then start listening for input
            var question = await speechRecognizer.RecognizeOnceAsync();
            Console.WriteLine("Question:\n" + question.Text);

            messages.Add(new Message("user", question.Text));

            // Get the response from the GPT API
            Console.WriteLine("Getting response from ChatGPT...");
            var response = await GetGptResponseAsync(messages, gptBearer);

            messages.Add(new Message("assistant", response));

            // Speak the response
            Console.WriteLine("Response:\n" + response);
            await speechSynthesizer.SpeakTextAsync(response ?? "I'm sorry, no response was provided.");

            Console.WriteLine("Press ENTER to continue or press CTRL+C to force quit.");
            Console.ReadLine();
        }

    }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }

    public Message(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

public enum State
{
    WaitForName,
    ListenToInput,
    ChatGPTApiRequest,
    ChatGPTApiResponse,
    SpeakResponse,
    PauseResponse
}
