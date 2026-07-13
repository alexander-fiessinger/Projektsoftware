using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Projektsoftware.Models
{
    // LogicC API Request Models
    public class LogicCChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<LogicCMessage> Messages { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    public class LogicCMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } // "system", "user", "assistant"

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    // LogicC API Response Models
    public class LogicCChatResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public List<LogicCChoice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public LogicCUsage Usage { get; set; }
    }

    public class LogicCChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public LogicCMessage Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class LogicCUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    // Application-specific AI Models
    public class TicketCategorizationResult
    {
        public string Category { get; set; }
        public string Priority { get; set; }
        public double Confidence { get; set; }
        public string Reasoning { get; set; }
    }

    public class EmailSummary
    {
        public string Summary { get; set; }
        public List<string> ActionItems { get; set; } = new();
        public string Sentiment { get; set; }
        public string Category { get; set; } // Added
        public string Priority { get; set; } // Added
    }

    public class LeadScoringResult
    {
        public int Score { get; set; } // 0-100
        public double SuccessProbability { get; set; } // Added - same as Score but 0-100
        public string Reasoning { get; set; }
        public List<string> PositiveFactors { get; set; } = new();
        public List<string> NegativeFactors { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new(); // Added
        public List<string> RiskFactors { get; set; } = new(); // Added
        public string NextBestAction { get; set; }
    }

    public class ProjectEstimationResult
    {
        public double EstimatedHours { get; set; }
        public double ConfidenceLevel { get; set; }
        public string Reasoning { get; set; }
        public List<string> RiskFactors { get; set; }
    }

    public class DocumentGenerationRequest
    {
        public string DocumentType { get; set; } // "letter", "contract", "offer"
        public string Context { get; set; }
        public Dictionary<string, string> Variables { get; set; }
    }

    public class DocumentGenerationResult
    {
        public string GeneratedText { get; set; }
        public List<string> Suggestions { get; set; }
    }
}
