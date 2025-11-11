<!--
Sync Impact Report
==================
Version: (new) → 1.0.0
Change Type: MAJOR (Initial constitution establishment)
Date: 2025-11-10

Modified Principles:
  - All principles are new (initial establishment)

Added Sections:
  - Core Principles (5 principles)
  - Template Standards
  - Workflow Discipline
  - Governance

Removed Sections:
  - None (initial version)

Templates Validation Status:
  ✅ .specify/templates/plan-template.md - Aligned with phased planning and constitution check
  ✅ .specify/templates/spec-template.md - Aligned with specification-first principle
  ✅ .specify/templates/tasks-template.md - Aligned with independent testing principle
  ✅ .roo/commands/*.md - Command files reference constitution correctly
  ⚠️ README.md - Not present; create if needed for project overview

Follow-up TODOs:
  - None

Notes:
  - This is the initial ratification of the SpecKit constitution
  - All principles derive from existing template structure and workflow patterns
  - Version 1.0.0 establishes baseline governance for the specification workflow toolkit
-->

# Watchtower Constitution

## Core Principles

### I. Template-Driven Workflow

All artifacts MUST follow standardized templates located in `.specify/templates/`. Templates

provide consistent structure, required sections, and validation checkpoints. Deviations require

explicit justification in the artifact itself.

**Rationale**: Consistency ensures predictability, reduces cognitive load, and enables automated

validation. Templates encode best practices and prevent documentation drift.

### II. Specification First

User requirements and business value MUST be captured in technology-agnostic specifications before

any technical design or implementation begins. Specifications focus on WHAT users need and WHY,

never HOW to implement.

**Rationale**: Separating requirements from implementation enables better understanding, stakeholder

alignment, and design flexibility. Technical solutions change; user needs persist.

### III. Phased Planning (NON-NEGOTIABLE)

Implementation planning MUST follow sequential phases:

- Phase 0: Research & Resolution (resolve all NEEDS CLARIFICATION markers)
- Phase 1: Design & Contracts (data models, API contracts, quickstart guides)
- Phase 2: Task Generation (independent, testable task breakdown)

Each phase MUST complete before the next begins. No implementation starts before Phase 2 completes.

**Rationale**: Upfront clarity prevents costly mid-implementation pivots. Each phase validates

assumptions and builds on solid foundations. Sequential gates ensure quality.

### IV. Independent Testing

User stories MUST be independently testable. Each story MUST:

- Deliver standalone value (viable as MVP)
- Be implementable without other stories
- Have clear acceptance criteria
- Be verifiable in isolation

**Rationale**: Independence enables incremental delivery, parallel development, and easy rollback.

MVP-focused stories reduce risk and accelerate feedback cycles.

### V. Constitution Compliance

All planning artifacts MUST include a Constitution Check section that validates against these

principles. Violations MUST be explicitly justified with:

- Why the violation is necessary
- What simpler alternatives were considered and rejected
- Impact on project complexity

**Rationale**: Active compliance verification prevents governance erosion. Justified exceptions

create audit trails and force deliberate complexity decisions.

## Template Standards

### Required Template Sections

All templates MUST clearly distinguish:

- **Mandatory sections**: Required for every artifact
- **Optional sections**: Include only when relevant
- **Conditional sections**: Required based on feature characteristics

### Placeholder Conventions

Templates use `[ALL_CAPS_IDENTIFIER]` for placeholders. Generated artifacts MUST:

- Replace all placeholders with concrete values
- Remove example comments after replacement
- Mark unknown values as `NEEDS CLARIFICATION: specific question`
- Limit clarification markers to maximum 3 per artifact

### Validation Requirements

Each template MUST specify:

- Success criteria for completion
- Validation checklist items
- Dependencies on other artifacts
- Output file locations and naming conventions

## Workflow Discipline

### Command Execution Order

SpecKit commands MUST execute in dependency order:

1. `/speckit.specify` - Create feature specification
2. `/speckit.clarify` - Resolve specification ambiguities (optional)
3. `/speckit.plan` - Generate implementation plan and design artifacts
4. `/speckit.tasks` - Break down into implementable tasks
5. `/speckit.implement` - Execute tasks with constitution compliance

### Branch and Directory Structure

Features MUST use numbered branches and spec directories:

- Format: `[###-feature-name]` (e.g., `001-user-auth`)
- Number assignment: Next available number for that feature short-name
- Specs directory: `specs/[###-feature-name]/`

### Artifact Dependencies

Generated artifacts form a dependency chain:

```text
spec.md (user requirements)
  ↓
plan.md (technical approach, constitution check)
  ↓
research.md (Phase 0), data-model.md, contracts/, quickstart.md (Phase 1)
  ↓
tasks.md (Phase 2, implementation breakdown)
```

Each artifact MUST reference its input artifacts and assume downstream artifacts depend on it.

## Governance

### Amendment Procedure

Constitution amendments require:

1. Version increment following semantic versioning (MAJOR.MINOR.PATCH)
2. Sync Impact Report documenting all changes
3. Validation of all templates for alignment
4. Update of dependent artifacts and command files

### Version Semantics

- **MAJOR**: Backward incompatible changes (principle removal/redefinition)
- **MINOR**: New principles, sections, or material expansions
- **PATCH**: Clarifications, wording improvements, typo fixes

### Compliance Review

Constitution compliance is verified at:

- Specification creation (via template requirements)
- Implementation planning (via Constitution Check section)
- Task generation (via principle-driven categorization)
- Code review (via governance checklist)

### Complexity Justification

Any complexity that violates simplicity principles MUST be documented in a Complexity Tracking

table with:

- What principle is violated
- Why the complexity is necessary
- What simpler alternatives were rejected and why

Unjustified complexity violations MUST block artifact approval.

**Version**: 1.0.0 | **Ratified**: 2025-11-10 | **Last Amended**: 2025-11-10