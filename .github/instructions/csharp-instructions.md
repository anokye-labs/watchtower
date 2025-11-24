---
applyTo: '**/*.cs'
---
# C# Instructions

* Use the microsoft-docs tools as a primary documentation source
* Use deepWiki tools as secondary source of truth and for access to implementation details./
* Use the built-in tools for file search like `semantic_search`, `list_code_usages`, `file_search`,  and `replace_string_in_file`, `multi_replace_string_in_file`, `get_search_view_results`, `list_dir`.

## C# Code Style

Follow the [.NET Runtime Coding Style Guidelines](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md).

### Key C# Coding Conventions

**General Principle:** Use Visual Studio defaults and enforce via `.editorconfig`.

1. **Braces:** Use Allman style (each brace on a new line). Single-line statement blocks can omit braces but must be properly indented.
2. **Indentation:** Use 4 spaces (no tabs).
3. **Field Naming:**
   - Private/internal instance fields: `_camelCase`
   - Static fields: `s_camelCase`
   - Thread static fields: `t_camelCase`
   - Public fields: `PascalCase` (use sparingly)
   - Use `readonly` where possible (after `static` for static fields)
4. **Avoid `this.`** unless absolutely necessary.
5. **Visibility:** Always specify visibility modifiers first (e.g., `private string _foo`).
6. **Namespace Imports:** Place at top of file, outside namespace declarations, sorted alphabetically with `System.*` namespaces first.
7. **Empty Lines:** Avoid more than one consecutive empty line.
8. **Whitespace:** Avoid spurious free spaces.
9. **`var` Keyword:** Use only when type is explicitly named on right-hand side (e.g., `var stream = new FileStream(...)`).
10. **Type Keywords:** Use language keywords (`int`, `string`, `float`) instead of BCL types (`Int32`, `String`, `Single`).
11. **Constants:** Use PascalCasing for all constant local variables and fields (except interop scenarios).
12. **Method Names:** Use PascalCasing, including local functions.
13. **`nameof(...)`:** Use instead of string literals whenever possible.
14. **Field Placement:** Specify fields at the top within type declarations.
15. **Non-ASCII Characters:** Use Unicode escape sequences (`\uXXXX`) instead of literal characters.
16. **Labels:** Indent one less than current indentation.
17. **Single-Statement `if`:**
    - Never use single-line form
    - Braces required if any block in `if`/`else if`/`else` compound uses braces or if body spans multiple lines
    - Braces may be omitted only if all blocks are single-line
18. **Type Modifiers:** Make internal and private types `static` or `sealed` unless derivation is required.
19. **Primary Constructor Parameters:** Use `camelCase` (no `_` prefix) for small types; assign to `_camelCase` fields for larger types.

### Tooling

- An `.editorconfig` file enforces these rules automatically
- Use `dotnet format` to ensure consistent code style
