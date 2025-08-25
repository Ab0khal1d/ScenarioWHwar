---
applyTo: "**/*.*"
---

# System Components Breakdown

The system is composed of the following key components, grouped by their responsibility.

## 1. Core Application Services

These are the main services that contain the business logic and serve API requests.

### CMS API

- **Technology**: C# ASP.NET Core (App Service)
- **Description**: The primary backend for administrative actions. Exposes RESTful endpoints for managing episode metadata, generating secure upload URLs, and initiating import jobs.
- **Purpose**: System's entry point for admins and orchestrator.

### Discovery API

- **Technology**: C# ASP.NET Core (App Service)
- **Description**: The public-facing backend for end-users.
- **Purpose**: Exposes search endpoint (/api/episodes/search) that queries the search index. Decoupled from primary database for read scalability.

## 2. Data Storage Services

These services are responsible for the persistence and retrieval of data.

### Primary SQL Database

- **Technology**: Azure SQL Database
- **Description**: Source of truth for all episode metadata (id, title, description, category, status, blob path, etc.)
- **Purpose**: OLTP database used by CMS API for write operations and processors for indexing.

### Search Index

- **Technology**: Azure Cognitive Search
- **Description**: Highly optimized, query-only index of episode metadata.
- **Purpose**: OLAP service used by Discovery API for fast, scalable full-text search and filtering.

### Blob Storage

- **Technology**: Azure Blob Storage (Container: videos)
- **Description**: Stores video files (MP4, etc.)
- **Purpose**: Secure, durable, and scalable object storage with direct URL access.

## 3. Event & Message Processing Services

### Event Grid

- **Technology**: Azure Event Grid
- **Description**: Serverless event routing service
- **Purpose**: Routes events from Azure services to downstream processors.

### Service Bus

- **Technology**: Azure Service Bus (Queue: import-commands)
- **Description**: Reliable message broker
- **Purpose**: Decouples CMS API from long-running import processes.

### Processor Function

- **Technology**: Azure Function (C#)
- **Description**: Event Grid triggered processor
- **Purpose**: Updates SQL Database status and pushes data to Search Index.

### Import Worker Function

- **Technology**: Azure Function (C#)
- **Description**: Service Bus triggered worker
- **Purpose**: Handles content import from external sources and uploads to Blob Storage.

## 4. Supporting Services

### Azure Active Directory

- **Technology**: Azure AD / B2C
- **Purpose**: Authentication and authorization management.

### CDN

- **Technology**: Azure CDN
- **Purpose**: Global video file distribution with low latency.

### Key Vault

- **Technology**: Azure Key Vault
- **Purpose**: Secure secret and credentials management.

### Application Insights

- **Technology**: Azure Monitor
- **Purpose**: Application monitoring, logging, and telemetry.
