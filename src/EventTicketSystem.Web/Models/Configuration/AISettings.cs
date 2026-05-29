namespace EventTicketSystem.Web.Models.Configuration;

public class AISettings
{
    public string ApiKey   { get; set; } = string.Empty;
    public string Model    { get; set; } = "llama-3.3-70b-versatile";
    public string Endpoint { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
}
