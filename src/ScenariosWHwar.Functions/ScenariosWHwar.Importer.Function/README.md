# ScenariosWHwar.Importer.Function

## Overview

Azure Function App responsible for importing episodes from external sources like URLs, RSS feeds, and other content providers.

## Functions

### ImportEpisodeFromUrl

- **Trigger**: HTTP POST `/api/import/url`
- **Purpose**: Import episodes from direct URLs (YouTube, podcasts, etc.)
- **Input**: UrlImportRequest with source URL and metadata
- **Output**: Import job status and tracking ID

### ImportEpisodeFromFeed

- **Trigger**: Timer (every 6 hours)
- **Purpose**: Automatically import new episodes from configured RSS feeds
- **Input**: Configured RSS feed URLs
- **Output**: New episodes queued for processing

### GetImportJobStatus

- **Trigger**: HTTP GET `/api/import/status/{jobId}`
- **Purpose**: Check the status of ongoing import jobs
- **Input**: Job ID
- **Output**: Import job status and progress

## Configuration

### Required Settings

- `AzureWebJobsStorage`: Azure Storage connection string for function state
- `DatabaseConnection`: SQL Server connection string for episode data
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Application Insights for monitoring

### Supported Import Sources

- Direct video URLs
- RSS/Atom feeds
- Podcast feeds
- YouTube channels/playlists
- Vimeo channels

## Import Flow

1. Validate source URL/feed
2. Extract metadata (title, description, thumbnail)
3. Download media file to temporary storage
4. Create episode record in database
5. Queue for media processing
6. Update import job status

## Dependencies

- System.ServiceModel.Syndication for RSS feed parsing
- Azure Functions Worker SDK
- Application Insights for monitoring

## Deployment

This function app handles import operations and should be configured with appropriate timeout values for long-running imports.
