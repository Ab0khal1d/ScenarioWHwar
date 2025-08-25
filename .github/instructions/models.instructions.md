---
applyTo: "**/*.*"
---

# Domain Models & DTOs Documentation

## 1. Core Domain Model (Database Entity)

### Episode

Primary data model stored in Azure SQL Database (source of truth).

#### Properties

- **Id** (Integer, Primary Key): Unique identifier for the episode
- **Title** (String, Required): The title of the episode
- **Description** (String): A longer description of the episode's content
- **Category** (enum): The genre or category (e.g., Technology, Culture)
- **Language** (String): The language of the episode (defaults to 'ar' for Arabic)
- **Duration** (Float): The length of the episode in seconds
- **PublishDate** (DateTime): The date the episode was or will be published
- **Status** (enum, Required): Tracks the episode's lifecycle (Values: PendingUpload, Processing, Ready, Failed)
- **BlobPath** (String): The full path to the video file in Azure Blob Storage
- **SourceType** (enum): Defines how the episode was created (Values: DirectUpload, YoutubeImport, RssImport)
- **CreatedAt** (DateTime): Timestamp of when the record was created
- **UpdatedAt** (DateTime): Timestamp of the last update to the record

## 2. CMS API Models & DTOs

### EpisodeCreateUpdateRequestDto

Sent to POST/PUT `/api/admin/episodes` endpoints.

- Properties: Title, Description, Category, Language, Duration, PublishDate

### EpisodeCreationResponseDto

Response from POST `/api/admin/episodes` endpoint.

- **Episode** (Object): Contains saved metadata
- **SasUrl** (String): Pre-authorized upload URL
- **BlobPath** (String): Target upload path

### EpisodeResponseDto

Complete episode state for GET `/api/admin/episodes/{id}`.

- All Episode model properties plus VideoUrl

### EpisodeImportRequestDto

Import request for POST `/api/admin/episodes/import`.

- **SourceType** (enum, Required)
- **SourceUrl** (String, Required)
- **Metadata** (Object, Optional)

### ImportJobStatusResponseDto

Status response for GET `/api/admin/episodes/import/status/{jobId}`.

- JobId, Status, EpisodeId, ErrorMessage

## 3. Discovery API Models

### EpisodeSearchRequestDto

Query parameters for GET `/api/episodes/search`.

- Query, Category, Language, Page, PageSize

### EpisodeSearchResultDto

Single episode in search results.

- Id, Title, Description, Category, Language, Duration, PublishDate, ThumbnailUrl, VideoUrl

### PaginatedSearchResultDto<EpisodeSearchResultDto>

Search endpoint response.

- Results, TotalCount, Page, PageSize

## 4. Azure Function Models

### BlobCreatedEventData

Event Grid blob creation event.

- Api, Url

### ImportEpisodeCommand

Service Bus import command message.

- JobId, EpisodeId, SourceType, SourceUrl, Metadata

### EpisodeSearchDocument

Azure Cognitive Search document model.

- Id, Title, Description, Category, Language, Duration, PublishDate, VideoUrl

## 5. Configuration Models

### AzureStorageConfig

- ConnectionString, VideosContainerName, SasTokenExpiryMinutes

### AzureSearchConfig

- ServiceEndpoint, IndexName, AdminApiKey

### ServiceBusConfig

- ConnectionString, ImportQueueName
