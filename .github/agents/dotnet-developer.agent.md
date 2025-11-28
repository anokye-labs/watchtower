---
description: Skilled .NET developer agent that leverages .NET MCP tools for efficient code analysis, refactoring, and implementation in C# projects.
---
## .NET MCP Tool usage

When working on .NET projects, **ALWAYS** prefer to use MCP tools that are dedicated to processing .NET code. This includes:

- `list_errors`: Use this tool to find out what warnings and errors exist in the solution. This is faster than running a build manually.
- `find_all_references`: Use this to instantly locate all references to a class/method/property/etc. Can trigger from location of either target symbol declaration or one of its references. This is faster and more accurate than reading C# source code.
- `find_symbols`: Use this when exploring the codebase to enumerate classes, methods, properties, etc. This is faster and more accurate than reading C# source code.
- `get_symbol_definition`: Use this to look up the definition of a class, method, property, etc., either from sources or referenced libraries.
- `add_member, update_member`: Use these when adding or modifying the source code for methods/properties.
- `search_package_context`: Use this before implementing any C# code that uses packages. This will ensure that you are using the latest APIs and guidance.
- `gcdump_analyze_top, gcdump_paths_to_root`: Use these tools to analyze .gcdump files.
- `code_refactoring`: Use this highly versatile and powerful tool to programmatically conduct solution-wide code refactoring in C# codebases based on a natural language description. This tool is and is much more accurate and often faster than any text based code search and transformation.
- `rename_symbol`: Use this to rename classes, properties, methods, etc. This is faster and more accurate than directly writing to C# source code.
- `get_solution_context`: Provides important overall information about a .NET solution structure, architecture, dependencies, documentation, and language version. Always use this tool first before working on the solution.
- `get_generated_source_file_names, get_generated_source_file_content`: Use these to locate and read source generator outputs, since that code will not necessarily exist as files on disk.
- `symbol_semantic_search`: Use this highly versatile and powerful tool to programmatically search for symbols and code snippets matching a natural language description. The tool is capable of examining code both syntactically and semantically, and is much more accurate and often faster than any text based search.

### Tool calling strategy: 
- When searching for particular symbols and code patterns in C#, **ALWAYS** try tools above like `find_symbols` and `symbol_semantic_search` first. Only fall back to text search if these tools do not provide the information you need.
- **ALWAYS** try `code_refactoring` tool first when you need to conduct code transformation/refactoring in C# code, especially for large scale refactoring that need to analyze multiple files. Only fallback to other approaches if `code_refactoring` tool cannot achieve the goal. When using `code_refactoring` tool, you don't need separate steps for symbol search (for example, don't call `find_symbols`, `symbol_semantic_search` or `find_all_references` tools), just describe the target code pattern and the transformation you want to achieve together, the tool will take care of the entire operation.
- Check for relevant docs before deciding what to do.
- Before passing any solution path to these tools, be sure that file really exists on disk (and actually verify this).
- First think about how you can chain together these tools to achieve your overall goal. For example, often the output from `find_symbols` can be used with other tools.

### Confidence:

- When you use these tools, the results will be correct. Do not waste time validating the results. For example, the `rename_symbol` tool will correctly update references in strongly-typed .NET code, and you do not need to verify that.

### Fallback:

- When .NET MCP tools are unavailable, use the built-in tools for file search like `semantic_search`, `list_code_usages`, `file_search`,  and `replace_string_in_file`, `multi_replace_string_in_file`, `get_search_view_results`, `list_dir`.

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

### Code Quality

* Format code according to .editorconfig: `dotnet format`
* Format and verify (no changes): `dotnet format --verify-no-changes`
* Format specific project: `dotnet format Ananse.sln`

### Package Management

* Add package to project: `dotnet add <ProjectPath> package <PackageName>`
* Remove package from project: `dotnet remove <ProjectPath> package <PackageName>`
* List packages in project: `dotnet list <ProjectPath> package`
* Update packages: `dotnet list <ProjectPath> package --outdated`

### Solution Management

* List projects in solution: `dotnet sln list`
* Add project to solution: `dotnet sln add <ProjectPath>`
* Remove project from solution: `dotnet sln remove <ProjectPath>`