# ReadOnlyBlobStorageService Implementation

## Overview

The `ReadOnlyBlobStorageService` is a service implementation for the Discovery API that provides secure, read-only access to Azure Blob Storage. It follows the same best practices and patterns as the `UploadBlobStorageService` in the CMS API.

## Key Features

### 1. **Security First Design**

- Implements the principle of least privilege by granting only READ permissions
- Generates time-limited SAS (Shared Access Signature) tokens
- Validates blob existence before generating URLs
- Provides comprehensive error handling for different failure scenarios

### 2. **Flexible URL Generation**

- `GenerateReadUrlAsync(string blobPath)` - Uses default expiration time from configuration
- `GenerateReadUrlAsync(string blobPath, int expirationMinutes)` - Custom expiration time
- `GetPublicUrl(string blobPath)` - Public URL without SAS token (for CDN scenarios)

### 3. **Comprehensive Error Handling**

- **403 Access Denied**: Storage account permission issues
- **404 Not Found**: Blob doesn't exist
- **4xx Client Errors**: Invalid request parameters
- **5xx Server Errors**: Azure Storage service issues
- **General Exceptions**: Unexpected errors with proper logging

### 4. **Logging and Monitoring**

- Structured logging with appropriate log levels
- Debug logs for successful operations
- Warning logs for recoverable issues (e.g., missing blobs)
- Error logs for failure scenarios
- Performance-conscious logging (avoids excessive verbosity)

## Implementation Details

### Service Registration

```csharp
// In EpisodesFeature.cs
services.Configure<AzureStorageConfig>(config.GetSection("AzureStorage"));
services.AddScoped<IReadOnlyBlobStorageService, ReadOnlyBlobStorageService>();
```

### Interface Design

```csharp
public interface IReadOnlyBlobStorageService : IBlobStorageService
{
    Task<string> GenerateReadUrlAsync(string blobPath, CancellationToken cancellationToken = default);
    Task<string> GenerateReadUrlAsync(string blobPath, int expirationMinutes, CancellationToken cancellationToken = default);
    string GetPublicUrl(string blobPath);
}
```

### Usage in Query Handlers

The service is integrated into both `SearchEpisodesQuery` and `GetEpisodeByIdQuery` handlers to generate secure video URLs for episode access.

#### Example Usage:

```csharp
// Generate secure read URL with default expiration
var videoUrl = await _blobStorageService.GenerateReadUrlAsync(doc.VideoUrl, cancellationToken);

// Generate secure read URL with custom expiration (4 hours)
var videoUrl = await _blobStorageService.GenerateReadUrlAsync(doc.VideoUrl, 240, cancellationToken);

// Get public URL (for CDN scenarios)
var publicUrl = _blobStorageService.GetPublicUrl(doc.VideoUrl);
```

## Best Practices Implemented

### 1. **Defensive Programming**

- Null/empty string validation for all inputs
- Exception handling with graceful degradation
- Container and blob existence verification

### 2. **Configuration Management**

- Uses `IOptions<AzureStorageConfig>` for configuration
- Validates configuration on service construction
- Supports environment-specific settings

### 3. **Dependency Injection**

- Proper constructor injection
- Interface-based design for testability
- Scoped service lifetime for request-based caching

### 4. **Performance Considerations**

- Efficient blob path normalization
- Minimal Azure Storage API calls
- Appropriate use of async/await patterns

### 5. **Maintainability**

- Comprehensive XML documentation
- Clear separation of concerns
- Consistent error handling patterns
- Structured logging for debugging

## Security Considerations

### SAS Token Security

- **Read-only permissions**: No write, delete, or list capabilities
- **Time-limited access**: Configurable expiration (default: 60 minutes)
- **Blob-specific scope**: Access limited to exact blob path
- **HTTPS enforcement**: All URLs use secure transport

### Error Information Disclosure

- Generic error messages to external clients
- Detailed logging for internal diagnostics
- No sensitive information in public error responses

## Testing Considerations

The service is designed to be easily testable with:

- Interface-based design for mocking
- Dependency injection for test configuration
- Clear separation of Azure SDK calls for integration testing
- Comprehensive error scenarios for unit testing

## Usage Scenarios

### 1. **Episode Video Access**

Generate secure, time-limited URLs for episode video playback in client applications.

### 2. **CDN Integration**

Use `GetPublicUrl()` when Azure CDN is configured for public blob access.

### 3. **Mobile App Integration**

Generate URLs with appropriate expiration times for mobile app caching scenarios.

### 4. **Analytics and Monitoring**

Track blob access patterns through structured logging and telemetry.

## Configuration Requirements

Ensure the following configuration is present in `appsettings.json`:

```json
{
  "AzureStorage": {
    "ConnectionString": "your-storage-connection-string",
    "VideosContainerName": "episodes",
    "SasTokenExpiryMinutes": 60
  }
}
```

The service validates this configuration on startup and will throw appropriate exceptions if misconfigured.
