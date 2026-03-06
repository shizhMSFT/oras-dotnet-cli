# ORAS CLI to oras-dotnet Library Mapping Reference

This document maps Go `oras` CLI commands to their corresponding implementations in the **OrasProject.Oras** .NET library. This reference is used to guide CLI command implementation planning.

---

## Library Overview

**NuGet Package:** `OrasProject.Oras`  
**Target Framework:** .NET 8.0+  
**Key Namespaces:**
- `OrasProject.Oras.Registry.Remote` - Registry and repository clients
- `OrasProject.Oras.Registry.Remote.Auth` - Authentication and credential handling
- `OrasProject.Oras.Content` - Blob/content storage operations
- `OrasProject.Oras.Oci` - OCI models (Manifest, Descriptor, Index, Platform)
- `OrasProject.Oras.Exceptions` - Custom exception types

---

## Top-Level Commands

### 1. `oras pull`
**Purpose:** Download artifacts from a registry

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras pull <reference> [--output-dir \<dir\>]` |
| **Core API** | `IRepository.FetchAsync()` + `ITarget.FetchAsync()` |
| **Key Classes** | `Registry`, `Repository`, `BlobStore`, `ManifestStore` |
| **Namespace** | `OrasProject.Oras.Registry.Remote` |
| **Related Interfaces** | `IReadOnlyTarget`, `IFetchable` |
| **Flow** | 1. Resolve reference → 2. Fetch manifest → 3. Fetch blobs (layers) |
| **Auth** | `Registry` constructor with `ICredentialProvider` |

**Notes:**
- Manifest resolution handled by `IReferenceFetchable.ResolveAsync()`
- Blob downloads via `IBlobStore.FetchAsync()`
- Supports both container images and arbitrary artifacts

---

### 2. `oras push`
**Purpose:** Upload artifacts to a registry

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras push <reference> [--artifact-type \<type\>] [file ...]` |
| **Core API** | `ITarget.PushAsync()` + `Packer.PackManifestAsync()` |
| **Key Classes** | `Registry`, `Repository`, `BlobStore`, `ManifestStore`, `Packer` |
| **Namespace** | `OrasProject.Oras.Registry.Remote`, `OrasProject.Oras` |
| **Related Interfaces** | `ITarget`, `IPushable`, `ITaggable` |
| **Options** | `PackManifestOptions` (artifact type, annotations, version) |
| **Flow** | 1. Load files → 2. Create descriptors → 3. Pack manifest → 4. Push blobs → 5. Push manifest |

**Notes:**
- `Packer.PackManifestAsync()` creates OCI manifests (v1.0 or v1.1 formats)
- Blob upload via `IBlobStore.PushAsync()`
- Manifest upload via `IManifestStore.PushAsync()`
- Support for artifact types, subjects (referrers), and annotations

---

### 3. `oras login`
**Purpose:** Authenticate with a registry

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras login <registry> [-u \<username\>] [-p \<password\>]` |
| **Core API** | `SingleRegistryCredentialProvider` or custom `ICredentialProvider` |
| **Key Classes** | `SingleRegistryCredentialProvider`, `Challenge`, `Scope` |
| **Namespace** | `OrasProject.Oras.Registry.Remote.Auth` |
| **Related Interfaces** | `ICredentialProvider` |
| **OAuth Support** | `Challenge` and `Scope` classes for OAuth2 flows |
| **Caching** | `ICache` interface for token caching |

**Notes:**
- Credentials passed to `Registry` constructor
- OAuth2 challenge/response flow managed by `Challenge` class
- Token caching via `ICache` implementation (e.g., in-memory cache)
- No explicit "login" method; credentials provided at client creation

---

### 4. `oras logout`
**Purpose:** Remove stored credentials

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras logout <registry>` |
| **Core API** | Custom `ICache` implementation clearing credentials |
| **Key Classes** | `ICache` (cache interface) |
| **Namespace** | `OrasProject.Oras.Registry.Remote.Auth` |
| **Notes** | CLI will need to manage credential storage; library handles in-memory caching |

---

### 5. `oras version`
**Purpose:** Display CLI and library version

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras version` |
| **Core API** | Assembly version reflection + library package version |
| **Implementation** | Custom CLI implementation (not directly in library) |

---

### 6. `oras discover`
**Purpose:** Find referrers (artifacts that reference a given manifest)

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras discover [--distribution-spec] <reference>` |
| **Core API** | `IPredecessorFindable.PredecessorsAsync()` |
| **Key Interfaces** | `IPredecessorFindable`, `IReadOnlyGraphStorage` |
| **Related Classes** | `Descriptor`, `Index` (OCI Image Index for multi-platform) |
| **Namespace** | `OrasProject.Oras.Content` |
| **Flow** | 1. Resolve reference → 2. Query referrers → 3. Return matching descriptors |

**Notes:**
- Returns list of descriptors referencing the given manifest
- Subject field in manifest indicates reference relationships
- Supports OCI Distribution Spec for referrer queries

---

### 7. `oras resolve`
**Purpose:** Resolve a reference to its digest

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras resolve <reference>` |
| **Core API** | `IReferenceFetchable.ResolveAsync()` or `IResolvable.ResolveAsync()` |
| **Key Interfaces** | `IResolvable`, `IReferenceFetchable` |
| **Return Type** | `Descriptor` (contains digest, size, media type) |
| **Namespace** | `OrasProject.Oras.Content`, `OrasProject.Oras.Registry.Remote` |

**Notes:**
- Returns manifest descriptor with resolved digest
- Foundation for all reference-based operations

---

### 8. `oras copy`
**Purpose:** Copy artifacts between registries or within the same registry

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras copy <src-reference> <dst-reference> [--recursive]` |
| **Core API** | `ReadOnlyTargetExtensions.CopyAsync()` |
| **Key Classes** | `ReadOnlyTargetExtensions`, `CopyOptions` |
| **Namespace** | `OrasProject.Oras` (extension methods) |
| **Options** | `CopyOptions` for controlling recursion and filtering |
| **Flow** | 1. Source resolve → 2. Fetch source DAG → 3. Push to destination |

**Notes:**
- Copies entire DAG (Directed Acyclic Graph) of related blobs
- Recursive option includes all referenced artifacts
- Works across registries or in-registry copies

---

### 9. `oras tag`
**Purpose:** Assign tags/aliases to an artifact

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras tag <source-reference> <target-reference>` |
| **Core API** | `IRepository.TagAsync()` or `ITaggable.TagAsync()` |
| **Key Interfaces** | `ITaggable`, `IRepository` |
| **Return Type** | `Descriptor` (updated descriptor with new tag) |
| **Namespace** | `OrasProject.Oras.Registry.Remote` |

**Notes:**
- Creates a new tag pointing to the same manifest digest
- Manifest Store handles tag updates
- Non-destructive operation

---

### 10. `oras attach`
**Purpose:** Attach a referrer manifest to an existing manifest

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras attach <parent-reference> [--artifact-type \<type\>] [file ...]` |
| **Core API** | `Packer.PackManifestAsync()` with subject specified |
| **Key Classes** | `Packer`, `Manifest` (subject field), `PackManifestOptions` |
| **Namespace** | `OrasProject.Oras` |
| **Options** | `PackManifestOptions.Subject` field |
| **Flow** | 1. Resolve parent → 2. Create referrer manifest with subject → 3. Push blobs & manifest |

**Notes:**
- Creates artifact that references another manifest via subject field
- Enables artifact linking and relationships
- `subject` field in manifest identifies parent

---

### 11. `oras backup`
**Purpose:** Backup an artifact and its dependencies to a file or registry

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras backup <source-reference> [--archive \<file\>] [--to-remote \<registry\>]` |
| **Core API** | `ReadOnlyTargetExtensions.CopyAsync()` + custom serialization |
| **Key Classes** | `ReadOnlyTargetExtensions`, `MemoryStore` (for intermediate storage) |
| **Namespace** | `OrasProject.Oras` |
| **Related Storage** | `MemoryStore` for in-memory artifact staging |
| **Flow** | 1. Fetch complete DAG → 2. Serialize to archive/file → 3. Push to remote if specified |

**Notes:**
- Captures entire artifact tree and all blobs
- Can export to tar/gzip archive or push to another registry
- Requires custom file format handling (not in core library)

---

### 12. `oras restore`
**Purpose:** Restore an artifact from a backup

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras restore [--archive \<file\>] <destination-reference>` |
| **Core API** | Custom deserialization + `ITarget.PushAsync()` |
| **Key Classes** | `Repository`, `BlobStore`, `ManifestStore` |
| **Namespace** | `OrasProject.Oras.Registry.Remote` |
| **Flow** | 1. Deserialize archive → 2. Push blobs → 3. Push manifests |

**Notes:**
- Inverse of backup operation
- Requires custom file format parsing (not in core library)
- Uses standard push operations for restoration

---

## Subcommands

### Blob Operations: `oras blob`

#### `oras blob fetch`
**Purpose:** Fetch a blob by digest

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras blob fetch --digest \<digest\> <reference>` |
| **Core API** | `IBlobStore.FetchAsync()` |
| **Key Classes** | `BlobStore`, `Digest` |
| **Namespace** | `OrasProject.Oras.Registry.Remote`, `OrasProject.Oras.Content` |
| **Return Type** | `Stream` (blob content) |

---

#### `oras blob push`
**Purpose:** Push a blob to a registry

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras blob push <reference> [--file \<path\>]` |
| **Core API** | `IBlobStore.PushAsync()` |
| **Key Classes** | `BlobStore` |
| **Namespace** | `OrasProject.Oras.Registry.Remote` |
| **Return Type** | `Descriptor` (blob descriptor with digest and size) |

---

#### `oras blob delete`
**Purpose:** Delete a blob from a registry

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras blob delete <reference> --digest \<digest\>` |
| **Core API** | `IDeletable.DeleteAsync()` |
| **Key Interfaces** | `IDeletable` |
| **Namespace** | `OrasProject.Oras.Content` |

---

### Manifest Operations: `oras manifest`

#### `oras manifest fetch`
**Purpose:** Fetch a manifest by reference

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras manifest fetch [--descriptor] <reference>` |
| **Core API** | `IManifestStore.FetchAsync()` |
| **Key Classes** | `ManifestStore`, `Manifest`, `Descriptor` |
| **Namespace** | `OrasProject.Oras.Registry.Remote`, `OrasProject.Oras.Oci` |
| **Return Type** | `Manifest` or `Descriptor` (with JSON content) |

---

#### `oras manifest fetch-config`
**Purpose:** Fetch a config blob referenced by a manifest

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras manifest fetch-config <reference>` |
| **Core API** | 1. `IManifestStore.FetchAsync()` → 2. `IBlobStore.FetchAsync(config)` |
| **Key Classes** | `ManifestStore`, `BlobStore`, `Manifest` |
| **Namespace** | `OrasProject.Oras.Registry.Remote` |
| **Flow** | Resolve manifest → get config descriptor → fetch config blob |

---

#### `oras manifest delete`
**Purpose:** Delete a manifest by reference (tag)

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras manifest delete <reference>` |
| **Core API** | `IDeletable.DeleteAsync()` or `IManifestStore.DeleteAsync()` |
| **Key Interfaces** | `IDeletable` |
| **Namespace** | `OrasProject.Oras.Content` |

---

#### `oras manifest index`
**Purpose:** Manage OCI Index (multi-platform) manifests

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras manifest index` (with various subcommands) |
| **Core API** | `Index` class, multi-platform manifest operations |
| **Key Classes** | `Index`, `Manifest`, `Platform`, `Descriptor` |
| **Namespace** | `OrasProject.Oras.Oci` |
| **Related** | `Packer.PackManifestAsync()` for creating indices |

---

### Repository Operations: `oras repo`

#### `oras repo ls`
**Purpose:** List all repositories in a registry

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras repo ls <registry>` |
| **Core API** | `IRegistry.ListRepositoriesAsync()` |
| **Key Interfaces** | `IRegistry` |
| **Return Type** | `IAsyncEnumerable<string>` (repository names) |
| **Namespace** | `OrasProject.Oras.Registry.Remote` |

---

#### `oras repo tags`
**Purpose:** List tags in a repository

| Aspect | Implementation |
|--------|-----------------|
| **CLI Command** | `oras repo tags <reference>` |
| **Core API** | `IRepository.TagListable.TagsAsync()` or `ITagListable.TagsAsync()` |
| **Key Interfaces** | `ITagListable`, `IRepository` |
| **Return Type** | `IAsyncEnumerable<string>` (tag names) |
| **Namespace** | `OrasProject.Oras.Registry.Remote` |

---

## Implementation Gaps & Notes

### Commands with Full Library Support ✅
- **pull** - Direct mapping to `IRepository.FetchAsync()`
- **push** - Direct mapping to `Packer.PackManifestAsync()` + `ITarget.PushAsync()`
- **tag** - Direct mapping to `IRepository.TagAsync()`
- **resolve** - Direct mapping to `IReferenceFetchable.ResolveAsync()`
- **copy** - Direct mapping to `ReadOnlyTargetExtensions.CopyAsync()`
- **attach** - Direct mapping to `Packer.PackManifestAsync()` with subject
- **discover** - Direct mapping to `IPredecessorFindable.PredecessorsAsync()`
- **blob fetch/push/delete** - Direct mapping to `IBlobStore` methods
- **manifest fetch/delete** - Direct mapping to `IManifestStore` methods
- **repo ls/tags** - Direct mapping to `IRegistry` and `ITagListable`

### Commands Requiring CLI-Level Implementation ⚠️
- **login/logout** - Library provides `ICredentialProvider` interface; CLI must implement credential storage and retrieval
- **version** - Library provides assembly versioning; CLI must implement version display
- **backup/restore** - Library provides DAG traversal via `CopyAsync()`; CLI must implement custom file format serialization/deserialization
- **manifest index** - Library provides `Index` class; CLI must implement manifest index creation and manipulation

### Key Design Considerations

1. **Async All The Way** - All library operations are async (`async/await`, `CancellationToken` support)
2. **Options Pattern** - Configuration via dedicated Options classes (`PackManifestOptions`, `RepositoryOptions`, `CopyOptions`)
3. **Interface-Driven** - Heavy use of abstractions for extensibility
4. **Error Handling** - Custom exceptions in `OrasProject.Oras.Exceptions` namespace
5. **Authentication** - Pluggable via `ICredentialProvider` interface

### Authentication Flow

```
Registry(uri, credentialProvider)
  └─ credentialProvider: ICredentialProvider
      └─ Returns auth token or challenge response
          └─ Challenge & Scope classes handle OAuth2 flow
              └─ ICache for token caching
```

---

## Core Namespaces Quick Reference

| Namespace | Purpose | Key Classes/Interfaces |
|-----------|---------|----------------------|
| `OrasProject.Oras.Registry.Remote` | Registry & repository clients | `Registry`, `Repository`, `BlobStore`, `ManifestStore` |
| `OrasProject.Oras.Registry.Remote.Auth` | Authentication | `ICredentialProvider`, `SingleRegistryCredentialProvider`, `Challenge`, `Scope`, `ICache` |
| `OrasProject.Oras.Content` | Blob/content storage | `IFetchable`, `IPushable`, `IDeletable`, `IResolvable`, `ITaggable`, `MemoryStore` |
| `OrasProject.Oras.Oci` | OCI models | `Manifest`, `Descriptor`, `Index`, `Platform`, `MediaType` |
| `OrasProject.Oras.Exceptions` | Error types | `AlreadyExistsException`, `NotFoundException`, `InvalidMediaTypeException`, etc. |
| `OrasProject.Oras` | Core utilities & extensions | `Packer`, `ReadOnlyTargetExtensions`, `CopyOptions` |

---

## Example Implementation Pattern

Most CLI commands follow this pattern:

```csharp
// 1. Create registry client with credentials
var registry = new Registry(uri, credentialProvider);

// 2. Get repository reference
var repo = await registry.GetRepositoryAsync(repositoryName);

// 3. Perform operation
var descriptor = await repo.FetchAsync(/* ... */);

// 4. Handle result or error
```

---

## References

- **Library Documentation:** https://oras-project.github.io/oras-dotnet/api/
- **Go CLI Reference:** https://oras.land/docs/category/oras-commands
- **OCI Spec:** https://github.com/opencontainers/spec
- **ORAS Project:** https://oras.land/
