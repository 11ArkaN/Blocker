# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| latest  | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability, please report it responsibly:

1. **Do not** open a public GitHub issue for security vulnerabilities
2. Instead, use [GitHub Security Advisories](../../security/advisories/new) to privately report the issue
3. Include a detailed description, steps to reproduce, and potential impact

You should receive an acknowledgment within 48 hours. We will work with you to understand the issue and coordinate a fix before any public disclosure.

## Security Considerations

Blocker operates with elevated privileges (administrator) and modifies system-level resources:

- **Windows Firewall rules** — creates outbound/inbound block rules for target executables
- **Hosts file** — adds entries to `%SystemRoot%\System32\drivers\etc\hosts`
- **Windows Registry** — writes to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` for auto-start
- **Process management** — terminates target processes

All changes are fully reversible when blocking is disabled. The application stores state in `%ProgramData%\Blocker\`.
