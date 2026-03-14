# Code Review Guidelines

## What to Check

- **Tests**: New endpoints and features must have corresponding tests
- **Secrets**: Ensure no API keys, tokens, or credentials are committed
- **Error handling**: Verify errors are handled gracefully and consistently
- **Early returns**: Prefer early returns over deeply nested conditionals
- **Naming**: Variables, functions, and files should have clear, descriptive names

## Review Tone

- Be constructive and specific — suggest improvements, don't just point out problems
- Ask questions when intent is unclear rather than assuming
- Acknowledge good work and thoughtful solutions

## What to Skip

- Auto-generated files (lock files, build output, codegen)
- Formatting-only changes handled by automated tools
- Bikeshedding on subjective style preferences already covered by linters
