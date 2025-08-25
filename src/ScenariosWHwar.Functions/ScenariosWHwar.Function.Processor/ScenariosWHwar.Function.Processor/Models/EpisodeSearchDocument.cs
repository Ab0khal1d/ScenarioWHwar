using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Text.Json.Serialization;

namespace ScenariosWHwar.Function.Processor.Models;

/// <summary>
/// Azure Cognitive Search document model for Episode indexing
/// Maps Episode domain entity to search index structure
/// </summary>
//public class EpisodeSearchDocument
//{
//    [SearchableField(IsKey = true)]
//    [JsonPropertyName("id")]
//    public string Id { get; set; } = string.Empty;

//    [SearchableField(IsSortable = true)]
//    [JsonPropertyName("title")]
//    public string Title { get; set; } = string.Empty;

//    [SearchableField]
//    [JsonPropertyName("description")]
//    public string Description { get; set; } = string.Empty;

//    [SearchableField(IsFacetable = true, IsFilterable = true)]
//    [JsonPropertyName("category")]
//    public string Category { get; set; } = string.Empty;

//    [SearchableField(IsFacetable = true, IsFilterable = true)]
//    [JsonPropertyName("language")]
//    public string Language { get; set; } = string.Empty;
    
//    [SearchableField(IsFacetable = true, IsFilterable = true)]
//    [JsonPropertyName("blobPath")]
//    public string BlobPath { get; set; } = string.Empty;

//    [SimpleField(IsFacetable = true, IsSortable = true)]
//    [JsonPropertyName("duration")]
//    public double Duration { get; set; }

//    [SimpleField(IsFacetable = true, IsSortable = true, IsFilterable = true)]
//    [JsonPropertyName("publishDate")]
//    public DateTimeOffset PublishDate { get; set; }

//    [SimpleField]
//    [JsonPropertyName("sourceUrl")]
//    public string SourceUrl { get; set; } = string.Empty;

//    [SearchableField(IsFacetable = true, IsFilterable = true)]
//    [JsonPropertyName("sourceType")]
//    public string SourceType { get; set; } = string.Empty;

//    [SimpleField(IsSortable = true)]
//    [JsonPropertyName("createdAt")]
//    public DateTimeOffset CreatedAt { get; set; }

//    [SimpleField(IsSortable = true)]
//    [JsonPropertyName("updatedAt")]
//    public DateTimeOffset UpdatedAt { get; set; }
//}
