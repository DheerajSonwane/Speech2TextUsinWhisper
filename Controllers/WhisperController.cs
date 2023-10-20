using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[Route("api/whisper")]
[ApiController]
public class WhisperController : ControllerBase
{
    private readonly string whisperApiKey = "";

    [HttpPost("transcribe")]
    public async Task<IActionResult> TranscribeSpeech()
    {

        if (!Request.HasFormContentType)
        {
            return BadRequest("Invalid Content-Type. Expected 'multipart/form-data'.");
        }

        var form = await Request.ReadFormAsync();

        if (form.Files.Count == 0)
        {
            return BadRequest("No audio file provided.");
        }

        var audioFile = form.Files[0]; // Assuming the audio file is the first part

        if (!audioFile.ContentType.StartsWith("audio/wav"))
        {
            return BadRequest("Invalid Content-Type for audio. Expected 'audio/wav'.");
        }

        using (var stream = audioFile.OpenReadStream())
        {
            // Read the audio data from the stream
            byte[] audioData;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                audioData = memoryStream.ToArray();
            }

            // Now you have the audio data in the 'audioData' byte array
            string whisperUrl = "https://api.openai.com/v1/audio/transcriptions";
            HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", whisperApiKey);

            //working for mp3 to text
            //var content = new MultipartFormDataContent();
            //var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(@"C:\Users\hp\Desktop\Test.mp3.mp3"));
            //fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/mp3");
            //content.Add(fileContent, "file", "audio.mp3");
            //content.Add(new StringContent("whisper-1"), "model");

            // Retry configuration
            int maxRetries = 3;
            int delay = 1000; // Initial delay in milliseconds
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    var content = new MultipartFormDataContent();
                    var fileContent = new ByteArrayContent(audioData);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                    content.Add(fileContent, "file", "audio.wav");
                    content.Add(new StringContent("whisper-1"), "model");
                    HttpResponseMessage response = await http.PostAsync(whisperUrl, content);

                    if (response.IsSuccessStatusCode)
                    { 
                        var result = await response.Content.ReadAsStringAsync();
                        var responseObject = new { text = result }; // Create a JSON object with the transcribed text
                        return Ok(responseObject);
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        return StatusCode((int)response.StatusCode, errorResponse);
                    }
                }
                catch (HttpRequestException)
                {
                    // Handle the exception, perform retries with exponential backoff
                    if (retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        await Task.Delay(delay);
                        delay *= 2; // Exponential backoff
                    }
                    else
                    {
                        // Max retries reached, return an error response
                        return StatusCode(503, "Service Unavailable: Max retries reached.");
                    }
                }
            }
            // Max retries reached without success
            return StatusCode(503, "Service Unavailable: Max retries reached.");
        }
    }
}
 