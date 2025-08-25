---
applyTo: "**/*.*"
---

# Creating a New API Project

## Initial Setup

```bash
dotnet new ssw-vsa --name {{SolutionName}}
```

## Adding a New Feature

Navigate to the WebApi directory and create a new feature:

```bash
cd src/WebApi/
dotnet new ssw-vsa-slice --feature Person --feature-plural People
```

### Parameters

- `--feature` or `-f`: The singular name of the feature
- `--feature-plural` or `-fp`: The plural name of the feature
