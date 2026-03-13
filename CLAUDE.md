# Project Guidelines

## Commit Conventions

- Use [Conventional Commits](https://www.conventionalcommits.org/): `type(scope): description`
- Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `ci`
- Keep commits atomic — one logical change per commit
- Write clear, descriptive commit messages

## Testing

- All new features must include tests
- All bug fixes must include a regression test
- Run the full test suite before pushing: `npm test` (or equivalent)
- Aim for meaningful coverage — test behavior, not implementation details

## Security

- Never commit secrets, API keys, tokens, or credentials
- Use environment variables or secret management for sensitive values
- Review dependencies for known vulnerabilities

## Code Style

- Write clean, readable code — favor clarity over cleverness
- Use consistent naming conventions throughout the project
- Keep functions small and focused on a single responsibility
- Remove dead code — don't leave commented-out blocks

## Pull Requests

- PRs should be focused and reasonably sized
- Include a clear description of what changed and why
- Reference related issues (e.g., `Closes #123`)
- Ensure CI passes before requesting review

## Documentation

- Update README and relevant docs when changing user-facing behavior
- Document public APIs and non-obvious design decisions
- Keep documentation close to the code it describes
