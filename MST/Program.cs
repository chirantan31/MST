using CTCommons.MSTranscription;
using System;
using System.Threading.Tasks;

namespace MST
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Temp();
        }

        public static void Temp()
        {
            TempAsync().GetAwaiter().GetResult();
        }

        private static async Task TempAsync()
        {
            MSTranscriptionService _transcriptionService = new MSTranscriptionService();
            // A dummy awaited function call.
            await Task.Delay(0);
            // Add any temporary code.

            Console.WriteLine("Hi");
            var filepath = "bfc2a97c-b511-4ef5-9b36-b4544c72c121.wav";
            var x = await _transcriptionService.RecognitionWithAudioStreamAsync(filepath, "86092d275e38442c9aa5112e1271fe76", "eastus");
            Console.WriteLine("Hi");
        }
    }
}
