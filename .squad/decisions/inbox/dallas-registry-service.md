# Decision: RegistryService Implementation Pattern

**Date:** 2026-03-09  
**Author:** Dallas (Core Dev)  
**Status:** Implemented

## Context

All 18 CLI command implementations depend on RegistryService to create authenticated Registry and Repository instances. The OrasProject.Oras v0.5.0 library has specific patterns for configuration and authentication that differ from typical mutable object patterns.

## Decision

### 1. RepositoryOptions Constructor Pattern

Registry and Repository must be created using the single-argument `RepositoryOptions` constructor when PlainHttp or other options need to be configured:

```csharp
var options = new RepositoryOptions
{
    Client = client,
    Reference = Reference.Parse(reference),
    PlainHttp = plainHttp
};
var repository = new Repository(options);
```

**Rationale:** RepositoryOptions is a struct with required Client and Reference properties. The Registry.RepositoryOptions and Repository.Options properties are read-only, so options must be set before construction.

### 2. Authentication Waterfall

RegistryService implements a three-tier authentication waterfall:

1. **Explicit credentials** (username + password parameters) → SingleRegistryCredentialProvider + Client with Cache
2. **Stored credentials** (via CredentialService) → Same auth setup with stored creds
3. **Unauthenticated** → PlainClient (no credentials)

**Rationale:** Supports both interactive login (stored creds) and programmatic access (explicit creds), with fallback to public registries.

### 3. OAuth2 Token Caching

All authenticated clients use `Cache(new MemoryCache(new MemoryCacheOptions()))` for OAuth2 token caching.

**Rationale:** The OrasProject.Oras.Registry.Remote.Auth.Client requires an ICache implementation for OAuth2 challenge-response flows. Using Microsoft.Extensions.Caching.Memory provides standard .NET token caching.

### 4. Namespace Organization

- `OrasProject.Oras.Registry` — Reference parsing
- `OrasProject.Oras.Registry.Remote` — Registry, Repository
- `OrasProject.Oras.Registry.Remote.Auth` — Auth types (Credential, Client, Cache, PlainClient, SingleRegistryCredentialProvider)

**Rationale:** Reference class is in the base Registry namespace, not Remote. Both namespaces must be imported.

## Implications

- All command handlers can now rely on RegistryService for consistent authentication
- PlainHttp registries (localhost:5000) work correctly
- Token caching improves performance for repeated registry operations
- Commands must pass plainHttp parameter from CLI options to RegistryService

## Alternatives Considered

1. **Modifying RepositoryOptions after construction** — Not possible, struct properties are read-only
2. **Using Registry(string, IClient) constructor** — Cannot set PlainHttp this way
3. **Manual HttpClient authentication** — OrasProject.Oras handles OAuth2 challenge-response, manual implementation would be complex

## Related Files

- `src/Oras.Cli/Services/RegistryService.cs` — Implementation
- `src/Oras.Cli/Services/IRegistryService.cs` — Interface
- `Directory.Packages.props` — Microsoft.Extensions.Caching.Memory dependency
