---
applyTo: '**/*.csproj, **/*.sln,**/*.slnx'
--- 

## Tools and Commands
When manipulating .NET projects and solutions, the following commands are commonly used:

### Build, Run and Test
Prefer built-in Tools where available over CLI commands:

* Common Workspace Tasks:
  - `runTask`, `runSubagent`, `create_and_run_task`, `runTests`, `test_failure`, `getTaskOutput`, `get_errors`

* CLI Commands:  
  * Restore dependencies: `dotnet restore`
  * Build the solution: `dotnet build`
  * Build specific project: `dotnet build <ProjectPath>`
  * Clean build artifacts: `dotnet clean`
  * Run the Aspire AppHost (starts all services): `dotnet run --project <ProjectPath>`
  * Watch mode (auto-reload on changes): `dotnet watch --project <ProjectPath>`
  * Run all tests: `dotnet test`
  * Run tests with coverage: `dotnet test --collect:"XPlat Code Coverage"`
  * Run specific test project: `dotnet test <ProjectPath>`
  * Watch mode for tests: `dotnet watch test --project <ProjectPath>`

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
