# Contributing to Blocker

Thank you for considering contributing to Blocker! Every contribution helps make this project better.

## Getting Started

1. **Fork** the repository
2. **Clone** your fork locally
3. **Create a branch** for your change (`git checkout -b feature/my-change`)
4. **Make your changes** and test them
5. **Commit** with a clear message
6. **Push** to your fork and open a **Pull Request**

## Development Setup

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Windows 10/11 (WPF is Windows-only)
- Visual Studio 2022+ or VS Code with C# Dev Kit
- Administrator privileges (required for firewall and hosts file operations)

### Building

```bash
dotnet build Blocker.sln
```

### Running

The application requires administrator privileges. Run from an elevated terminal or Visual Studio running as administrator.

## Code Style

- Follow the existing code conventions in the project
- Use `nullable` annotations consistently
- Keep methods focused and concise
- Use interfaces for service abstractions (`Contracts/` folder)
- Place implementations in the `Services/` folder

## Pull Request Guidelines

- Keep PRs focused on a single change
- Describe what the PR does and why
- Reference any related issues
- Ensure the project builds without warnings

## Reporting Bugs

Use the [GitHub Issues](../../issues) tab with the **Bug Report** template. Include:

- Steps to reproduce
- Expected vs. actual behavior
- Windows version and .NET runtime version
- Relevant log excerpts from `%ProgramData%\Blocker\logs\`

## Feature Requests

Use the [GitHub Issues](../../issues) tab with the **Feature Request** template.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
