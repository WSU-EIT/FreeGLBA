# Docs

Documentation project for the FreeGLBA GLBA Compliance Data Access Tracking System. Contains guides, architecture documentation, and project plans.

Developed by **Enrollment Information Technology** at **Washington State University**.

## Purpose

This project serves as the central documentation repository:
- **Architecture Documentation** - System design and patterns
- **Implementation Guides** - How to customize and extend
- **Style Guides** - Coding standards and conventions
- **Component Documentation** - UI component usage
- **Project Plans** - Feature roadmaps and specifications

## Documentation Files

### Quick Start & Guides

| File | Description |
|------|-------------|
| `000_quickstart.md` | Getting started guide |
| `001_roleplay.md` | Development roleplay scenarios |
| `002_docsguide.md` | Documentation conventions |

### Style & Standards

| File | Description |
|------|-------------|
| `003_templates.md` | Code templates |
| `004_styleguide.md` | General style guide |
| `005_style.md` | Coding standards |
| `005_style.comments.md` | Comment conventions |

### Architecture

| File | Description |
|------|-------------|
| `006_architecture.md` | System architecture overview |
| `006_architecture.freecrm_overview.md` | Base framework architecture |
| `006_architecture.unique_features.md` | FreeGLBA-specific features |

### Patterns & Best Practices

| File | Description |
|------|-------------|
| `007_patterns.md` | Design patterns used |
| `007_patterns.helpers.md` | Helper class patterns |
| `007_patterns.signalr.md` | SignalR patterns |

### UI Components

| File | Description |
|------|-------------|
| `008_components.md` | Component overview |
| `008_components.highcharts.md` | Chart components |
| `008_components.monaco.md` | Code editor component |
| `008_components.network_chart.md` | Network visualization |
| `008_components.razor_templates.md` | Razor component templates |
| `008_components.signature.md` | Signature capture |
| `008_components.wizard.md` | Wizard component |

### FreeGLBA-Specific Documentation

| File | Description |
|------|-------------|
| `FreeGLBA_DataModel_Documentation.md` | Database schema documentation |
| `FreeGLBA_Implementation_Guide.md` | Implementation and customization guide |
| `FreeGLBA_NuGet_Package_ProjectPlan.md` | NuGet client library project plan |

## Using This Documentation

### For New Developers

1. Start with `000_quickstart.md`
2. Review `006_architecture.md` for system overview
3. Read relevant style guides before contributing

### For Customization

1. Review `FreeGLBA_Implementation_Guide.md`
2. Check `006_architecture.unique_features.md` for extension points
3. Follow patterns in `007_patterns.md`

### For UI Development

1. Start with `008_components.md` for component overview
2. Review specific component docs as needed
3. Follow `004_styleguide.md` for consistency

## Contributing Documentation

When adding documentation:

1. Follow the naming convention: `NNN_category.topic.md`
2. Use markdown formatting
3. Include code examples where applicable
4. Update this README with new files

## About

FreeGLBA is developed and maintained by the **Enrollment Information Technology** team at **Washington State University**.

🔗 [Meet Our Staff](https://em.wsu.edu/eit/meet-our-staff/)

## Project Structure

```
Docs/
├── Docs.csproj                              # Empty project for IDE support
├── README.md                                # This file
│
├── # Getting Started
├── 000_quickstart.md
├── 001_roleplay.md
├── 002_docsguide.md
│
├── # Standards
├── 003_templates.md
├── 004_styleguide.md
├── 005_style.md
├── 005_style.comments.md
│
├── # Architecture
├── 006_architecture.md
├── 006_architecture.freecrm_overview.md
├── 006_architecture.unique_features.md
│
├── # Patterns
├── 007_patterns.md
├── 007_patterns.helpers.md
├── 007_patterns.signalr.md
│
├── # Components
├── 008_components.md
├── 008_components.highcharts.md
├── 008_components.monaco.md
├── 008_components.network_chart.md
├── 008_components.razor_templates.md
├── 008_components.signature.md
├── 008_components.wizard.md
│
└── # FreeGLBA Specific
    ├── FreeGLBA_DataModel_Documentation.md
    ├── FreeGLBA_Implementation_Guide.md
    └── FreeGLBA_NuGet_Package_ProjectPlan.md
```

## Related Projects

All FreeGLBA projects have their own README files:

- [FreeGLBA/README.md](../FreeGLBA/README.md) - Server application
- [FreeGLBA.Client/README.md](../FreeGLBA.Client/README.md) - Blazor UI
- [FreeGLBA.DataAccess/README.md](../FreeGLBA.DataAccess/README.md) - Business logic
- [FreeGLBA.DataObjects/README.md](../FreeGLBA.DataObjects/README.md) - DTOs
- [FreeGLBA.EFModels/README.md](../FreeGLBA.EFModels/README.md) - Database models
- [FreeGLBA.Plugins/README.md](../FreeGLBA.Plugins/README.md) - Plugin system
- [FreeGLBA.NugetClient/README.md](../FreeGLBA.NugetClient/README.md) - NuGet client library
