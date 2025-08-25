---
applyTo: "**/*.*"
---

# API Endpoints Documentation

## 1. CMS API Endpoints (/api/admin/episodes)

This service is for internal content management.

### Endpoints

| Method | Endpoint               | Description                                                            | Parameters                                                                | Request Body                  | Response                              | Status Codes                           |
| ------ | ---------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------- | ----------------------------- | ------------------------------------- | -------------------------------------- |
| GET    | /                      | Gets a paginated list of all episodes for the admin UI                 | page (query, optional, default=1), pageSize (query, optional, default=20) | -                             | PaginatedResponse<EpisodeResponseDto> | 200 OK                                 |
| POST   | /                      | Creates a new episode record and generates a SAS URL for direct upload | -                                                                         | EpisodeCreateUpdateRequestDto | EpisodeCreationResponseDto            | 201 Created, 400 Bad Request           |
| GET    | /{id}                  | Gets the full details of a specific episode                            | id (path)                                                                 | -                             | EpisodeResponseDto                    | 200 OK, 404 Not Found                  |
| PUT    | /{id}                  | Updates the metadata of an existing episode                            | id (path)                                                                 | EpisodeCreateUpdateRequestDto | EpisodeResponseDto                    | 200 OK, 400 Bad Request, 404 Not Found |
| DELETE | /{id}                  | Deletes an episode and associated resources                            | id (path)                                                                 | -                             | -                                     | 204 No Content, 404 Not Found          |
| POST   | /import                | Initiates an async job to import from external source                  | -                                                                         | EpisodeImportRequestDto       | ImportJobStatusResponseDto            | 202 Accepted, 400 Bad Request          |
| GET    | /import/status/{jobId} | Checks the current status of an import job                             | jobId (path)                                                              | -                             | ImportJobStatusResponseDto            | 200 OK, 404 Not Found                  |

## 2. Discovery API Endpoints (/api/episodes)

This service is for public content discovery.

### Endpoints

| Method | Endpoint | Description                             | Parameters                                                  | Request Body | Response                                         | Status Codes          |
| ------ | -------- | --------------------------------------- | ----------------------------------------------------------- | ------------ | ------------------------------------------------ | --------------------- |
| GET    | /search  | Searches and filters published episodes | q, category, language (all query, optional), page, pageSize | -            | PaginatedSearchResultDto<EpisodeSearchResultDto> | 200 OK                |
| GET    | /{id}    | Gets public-facing episode details      | id (path)                                                   | -            | EpisodeSearchResultDto                           | 200 OK, 404 Not Found |

## 3. Processor Azure Function Endpoints

This function is triggered by events, not HTTP calls.

### Triggers

| Type        | Name             | Description                   | Input                | Output Actions                                                                     |
| ----------- | ---------------- | ----------------------------- | -------------------- | ---------------------------------------------------------------------------------- |
| Event Grid  | OnNewBlobProcess | Triggered on new video upload | BlobCreatedEventData | 1. Get Episode by ID<br>2. Update Status to "Ready"<br>3. Push to Cognitive Search |
| Service Bus | OnEpisodeDelete  | Handles cleanup of resources  | EpisodeDeleteCommand | 1. Delete blob<br>2. Delete search document<br>3. Delete DB record                 |

## 4. Import Worker Azure Function

### Triggers

| Type        | Name                   | Description              | Input                | Output Actions                                                                                          |
| ----------- | ---------------------- | ------------------------ | -------------------- | ------------------------------------------------------------------------------------------------------- |
| Service Bus | OnImportEpisodeCommand | Handles external imports | ImportEpisodeCommand | 1. Choose importer<br>2. Fetch external data<br>3. Upload to Blob Storage<br>4. Update Episode metadata |

## Key DTOs

- **EpisodeCreateUpdateRequestDto**: `{ Title, Description, Category, Language, Duration, PublishDate }`
- **EpisodeCreationResponseDto**: `{ Episode: {Id, Title, ...}, SasUrl, BlobPath }`
- **EpisodeResponseDto**: All Episode model properties + VideoUrl
- **EpisodeImportRequestDto**: `{ SourceType, SourceUrl, Metadata? }`
- **ImportJobStatusResponseDto**: `{ JobId, Status, EpisodeId?, ErrorMessage? }`
- **EpisodeSearchResultDto**: `{ Id, Title, Description, Category, Language, Duration, PublishDate, VideoUrl }`
- **PaginatedSearchResultDto<T>**: `{ Results: T[], TotalCount, Page, PageSize }`
- **ImportEpisodeCommand**: `{ JobId, EpisodeId, SourceType, SourceUrl, Metadata? }`
- **BlobCreatedEventData**: Contains URL of created blob
