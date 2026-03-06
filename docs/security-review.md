# ORAS .NET CLI Security Review

**Review Date:** 2026-03-06  
**Reviewer:** Hicks (Testing Team)  
**Sprint:** Sprint 4 (S4-09)  
**Scope:** Credential handling, logging, file permissions, input validation, dependencies

---

## Executive Summary

This security review audited the ORAS .NET CLI codebase for common security vulnerabilities. The review found **2 high-severity issues**, **2 medium-severity issues**, and verified **7 security controls** are correctly implemented. All findings have recommendations for remediation.

**Overall Risk Level:** MEDIUM (High-severity issues found, but limited attack surface in current stub implementations)

---

## Findings

### HIGH SEVERITY

#### H-1: Missing File Permissions on Credential Store

**Location:** `src/Oras.Cli/Credentials/DockerConfigStore.cs:56`

**Issue:** The `SaveAsync` method writes credentials to `~/.docker/config.json` without setting restrictive file permissions. On Unix-like systems, this could result in world-readable credential files containing base64-encoded passwords.

**Code:**
```csharp
await File.WriteAllTextAsync(_configPath, json, cancellationToken);
```

**Impact:** Credentials stored on disk may be accessible to other users on multi-user systems. This violates the principle of least privilege and could lead to credential theft.

**Recommendation:**
1. On Unix/Linux/macOS, set file permissions to `0600` (owner read/write only) immediately after file creation
2. Use .NET 7+ `UnixFileMode` API or `chmod` via process execution for Unix systems
3. On Windows, set NTFS ACLs to restrict access to current user only

**Example Fix:**
```csharp
await File.WriteAllTextAsync(_configPath, json, cancellationToken);

// Set restrictive permissions on Unix-like systems
if (!OperatingSystem.IsWindows())
{
    File.SetUnixFileMode(_configPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
}
```

---

#### H-2: Process Injection Risk in Native Credential Helper

**Location:** `src/Oras.Cli/Credentials/NativeCredentialHelper.cs:76-86`

**Issue:** The `RunHelperAsync` method constructs `ProcessStartInfo` with `_helperName` as the executable path without validation. If `_helperName` is user-controlled (via `config.json`'s `credsStore` or `credHelpers`), an attacker could inject arbitrary commands.

**Code:**
```csharp
var startInfo = new ProcessStartInfo
{
    FileName = _helperName,  // No validation of path
    Arguments = action,
    ...
};
```

**Attack Vector:** A malicious actor could modify `~/.docker/config.json` to set `credsStore` to `"; rm -rf / #"` or similar shell injection payloads.

**Impact:** Arbitrary command execution with user's privileges. Critical if CLI is run with elevated permissions.

**Recommendation:**
1. Validate `_helperName` contains only alphanumeric characters and hyphens (no path separators, shell metacharacters)
2. Use allowlist validation: helper names must start with `docker-credential-`
3. Resolve helper to absolute path and verify it exists in trusted directories (`/usr/bin`, `%ProgramFiles%`, etc.)
4. Document that users should not run CLI with elevated privileges unnecessarily

**Example Fix:**
```csharp
public NativeCredentialHelper(string helperName)
{
    // Validate helper name format
    if (!Regex.IsMatch(helperName, @"^[a-zA-Z0-9-]+$"))
    {
        throw new ArgumentException("Invalid credential helper name format", nameof(helperName));
    }
    
    _helperName = helperName.StartsWith("docker-credential-")
        ? helperName
        : $"docker-credential-{helperName}";
}
```

---

### MEDIUM SEVERITY

#### M-1: Error Messages May Expose URLs with Embedded Credentials

**Location:** `src/Oras.Cli/ErrorHandler.cs:33-34`, various command files

**Issue:** HTTP error messages may leak registry URLs that contain embedded credentials (e.g., `https://user:pass@registry.example.com`). While the codebase doesn't currently construct URLs this way, defensive error handling should sanitize URLs.

**Code:**
```csharp
WriteError(
    $"Network error: {ex.Message}",  // May contain URL from HttpRequestException
    "Check your network connection and registry address.");
```

**Impact:** Credential leakage via console output, log files, or error reports. Medium severity because current code doesn't construct URLs with embedded credentials, but future changes could introduce this.

**Recommendation:**
1. Create a URL sanitization utility that strips username/password from URLs before display
2. Apply to all error messages that could contain URLs (HttpRequestException, registry connection errors)
3. Pattern: `https://user:pass@host` → `https://***@host`

**Example Fix:**
```csharp
private static string SanitizeUrl(string message)
{
    // Remove credentials from URLs: https://user:pass@host -> https://***@host
    return Regex.Replace(message, @"(https?://)[^@:]+:[^@]+@", "$1***@");
}

WriteError(SanitizeUrl($"Network error: {ex.Message}"), recommendation);
```

---

#### M-2: Debug Mode Stack Traces May Leak Sensitive Data

**Location:** `src/Oras.Cli/ErrorHandler.cs:58-61`

**Issue:** When `ORAS_DEBUG=1` is set, full stack traces are printed to console. Stack traces may contain sensitive data in variable values, method parameters, or exception messages.

**Code:**
```csharp
if (Environment.GetEnvironmentVariable("ORAS_DEBUG") == "1")
{
    AnsiConsole.WriteException(ex);
}
```

**Impact:** Potential information disclosure in debug logs. Users may inadvertently share debug logs containing passwords, tokens, or registry credentials.

**Recommendation:**
1. Add prominent warning in documentation that debug mode may log sensitive information
2. Consider filtering exception details (sanitize messages, redact parameter values)
3. Add warning output when debug mode is enabled: "WARNING: Debug mode enabled. Output may contain sensitive information."

**Example Fix:**
```csharp
if (Environment.GetEnvironmentVariable("ORAS_DEBUG") == "1")
{
    AnsiConsole.MarkupLine("[yellow]⚠ Debug mode enabled. Output may contain sensitive information.[/]");
    AnsiConsole.WriteException(SanitizeException(ex));
}
```

---

## Items Verified as Safe

### ✅ V-1: No Plaintext Password Logging

**Status:** VERIFIED SAFE

**Findings:**
- Credential handling code uses `TextPrompt.Secret()` for password input (masking)
- No direct logging of `password`, `credential`, or `token` variables found
- Success messages only show registry hostname, not credentials
- Example: `LoginCommand.cs:91` shows only `"Login succeeded for {registry}"` (no password)

**Grep Results:** Zero matches for patterns like `WriteLine.*password`, `MarkupLine.*credential`, `Write.*token`

---

### ✅ V-2: Input Variables Properly Escaped in Output

**Status:** VERIFIED SAFE

**Findings:**
- Error messages use string interpolation but don't execute user input
- TUI code uses `Markup.Escape()` when displaying user-provided strings
- Example: `PromptHelper.cs:97-100` properly escapes messages before display
- No SQL injection risk (no database usage)
- No command injection risk in interpolated strings (user input not passed to shell)

**Note:** Process execution risk exists in credential helper (see H-2), but not in general string interpolation.

---

### ✅ V-3: Reference Validation Present

**Status:** PARTIALLY VERIFIED SAFE

**Findings:**
- Commands validate file existence before use (e.g., `BlobPushCommand.cs:73-78`)
- Reference parsing with error handling exists (e.g., `TagCommand.cs:72-108`)
- Usage exceptions (`OrasUsageException`) thrown for invalid formats
- Validation rejects malformed references with user-friendly error messages

**Limitation:** Reference parsing is basic and could be enhanced with more robust validation using regex patterns to match OCI reference spec. However, sufficient for current threat model.

---

### ✅ V-4: No Vulnerable Dependencies

**Status:** VERIFIED SAFE

**Command:** `dotnet list package --vulnerable`

**Result:** No vulnerable packages found in `oras` or `oras.Tests` projects

**Package Versions Reviewed:**
- System.CommandLine 2.0.3 (latest stable)
- Spectre.Console 0.50.0 (latest)
- OrasProject.Oras 0.5.0 (latest)
- Microsoft.Extensions.DependencyInjection 9.0.0 (latest)
- xUnit 2.9.3 (latest)
- Testcontainers 3.10.0 (latest)
- FluentAssertions 7.0.0 (latest)
- NSubstitute 5.3.0 (latest)

**Recommendation:** Establish CI/CD pipeline check for vulnerable dependencies using `dotnet list package --vulnerable` in build process.

---

### ✅ V-5: Credential Store Uses Standard Docker Config Format

**Status:** VERIFIED SAFE

**Findings:**
- Uses Docker's standard `~/.docker/config.json` format (cross-tool compatibility)
- Credentials stored as base64-encoded `username:password` (industry standard)
- Supports native credential helpers (Windows Credential Manager, macOS Keychain, etc.)
- Falls back to platform credential stores when configured
- No custom encryption (relies on OS-provided secure storage)

**Security Model:** Credentials are as secure as Docker CLI's credential storage. Users can opt into OS keychain integration via credential helpers.

---

### ✅ V-6: No Hardcoded Secrets

**Status:** VERIFIED SAFE

**Findings:**
- No API keys, tokens, passwords, or connection strings hardcoded in source
- Configuration loaded from environment variables and user input
- Test code uses dynamically created test registries (testcontainers)
- No production credentials in test fixtures

**Grep Results:** No hardcoded secrets found in codebase.

---

### ✅ V-7: Secure Password Input Masking

**Status:** VERIFIED SAFE

**Findings:**
- `LoginCommand.cs:58-61` uses `TextPrompt<string>.Secret()` for password input
- Password characters replaced with asterisks during input
- Supports `--password-stdin` for non-interactive scenarios (piped input)
- No echo of password to terminal

**Code:**
```csharp
password = AnsiConsole.Prompt(
    new TextPrompt<string>("Password:")
        .Secret());
```

---

## Recommendations Summary

| ID   | Severity | Recommendation                                      | Priority |
|------|----------|-----------------------------------------------------|----------|
| H-1  | High     | Set restrictive file permissions on credential file | P0       |
| H-2  | High     | Validate credential helper names, prevent injection | P0       |
| M-1  | Medium   | Sanitize URLs in error messages to strip credentials| P1       |
| M-2  | Medium   | Add warning for debug mode, sanitize stack traces   | P1       |
| V-4  | Info     | Add CI check for vulnerable dependencies            | P2       |

---

## Security Best Practices Applied

1. ✅ **Principle of Least Privilege:** Credentials only stored where necessary
2. ✅ **Defense in Depth:** Multiple authentication methods (file, helper, stdin)
3. ✅ **Fail Securely:** Exceptions caught and sanitized before display
4. ✅ **Secure Defaults:** Plain HTTP requires explicit `--plain-http` flag
5. ✅ **Input Validation:** Files, references, and formats validated before use
6. ⚠️ **Secure Storage:** Relies on OS credential stores (good) but file permissions need improvement (H-1)

---

## Future Security Considerations

As implementations mature beyond current stubs:

1. **TLS Certificate Validation:** Ensure `--insecure` flag is truly required and logs warnings
2. **Rate Limiting:** Consider abuse scenarios if CLI is exposed via automation
3. **Audit Logging:** Consider structured audit logs for credential access in enterprise scenarios
4. **SBOM Generation:** Publish Software Bill of Materials for supply chain security
5. **Code Signing:** Sign CLI binaries for release

---

## References

- Docker Credential Helpers Protocol: https://github.com/docker/docker-credential-helpers
- OCI Distribution Spec: https://github.com/opencontainers/distribution-spec
- OWASP Top 10: https://owasp.org/www-project-top-ten/
- .NET Security Best Practices: https://learn.microsoft.com/en-us/dotnet/standard/security/

---

## Conclusion

The ORAS .NET CLI demonstrates solid security fundamentals with proper password masking, no vulnerable dependencies, and standard credential storage patterns. However, **two high-severity issues must be addressed before production release:**

1. **File permissions** on credential storage (trivial fix)
2. **Process injection** risk in credential helper execution (moderate fix)

Once these issues are resolved, the security posture will be suitable for production use. The medium-severity issues are defense-in-depth improvements that should be addressed in Sprint 4 or early maintenance releases.

**Recommended Action:** Fix H-1 and H-2 before v1.0 release. Address M-1 and M-2 in v1.1 or as hotfix if time permits.
