# CLAUDE.md - AI Assistant Guide for OopFactory.X12

## Project Overview

OopFactory.X12 is an open-source .NET C# implementation of an X12 EDI (Electronic Data Interchange) parser, originally from CodePlex. The project provides a specification-driven parser that converts X12 EDI documents into a hierarchical XML representation without requiring database integration.

### Key Capabilities
- Parse any X12 transaction set using XML specifications
- Support for 4010 and 5010 standards (healthcare HIPAA focus)
- Bidirectional transformation: X12 <-> XML <-> HTML
- Healthcare claim parsing (837 Professional, Institutional, Dental)
- Validation and acknowledgment generation (997/999)
- Optional SQL Server persistence

## Repository Structure

```
OopFactory.X12/
├── trunk/                           # Main development branch
│   ├── src/                         # Source code (10 projects)
│   │   ├── OopFactory.X12/          # Core parsing library
│   │   ├── OopFactory.X12.Hipaa/    # HIPAA claim extensions
│   │   ├── OopFactory.X12.Validation/ # Validation & acknowledgments
│   │   ├── OopFactory.X12.Sql/      # SQL Server repository
│   │   └── [Console Apps]           # Various CLI tools
│   ├── tests/                       # Test projects (5 projects)
│   │   ├── OopFactory.X12.Tests.Unit/
│   │   ├── OopFactory.X12.Hipaa.Tests.Unit/
│   │   ├── OopFactory.X12.Validation.Tests.Unit/
│   │   └── OopFactory.X12.Tests.Integration/
│   └── lib/                         # External dependencies
├── branches/                        # Feature branches (270_271Typed, 837Typed)
├── README.md
└── LICENSE.md                       # Apache 2.0
```

## Build System

### Framework & Tools
- **Target Framework**: .NET Framework 4.0
- **IDE**: Visual Studio 2010 (Solution Format 11.00)
- **Build Tool**: MSBuild (ToolsVersion 4.0)
- **Platform**: AnyCPU (some console apps target x86)

### Solution File
The main solution is at `trunk/OopFactory.X12.sln` containing:
- 10 source projects
- 5 test projects
- Solution folders for organization (Libraries, Tests)

### Build Configurations
- **Debug**: Full debug symbols, no optimization
- **Release**: PDB symbols, optimized, XML documentation generated

### Building
```bash
# From trunk directory
msbuild OopFactory.X12.sln /p:Configuration=Release
```

## Core Architecture

### Object Model Hierarchy

```
Interchange (ISA/IEA envelope)
└── FunctionGroup (GS/GE envelope)
    └── Transaction (ST/SE envelope)
        └── Loop / HierarchicalLoop
            └── Segment
                └── Elements (indexed access)
```

### Key Classes

| Class | Location | Purpose |
|-------|----------|---------|
| `X12Parser` | `Parsing/X12Parser.cs` | Main entry point for parsing |
| `Interchange` | `Parsing/Model/Interchange.cs` | Root container, implements IXmlSerializable |
| `FunctionGroup` | `Parsing/Model/FunctionGroup.cs` | GS envelope container |
| `Transaction` | `Parsing/Model/Transaction.cs` | ST envelope with hierarchical loops |
| `Loop` | `Parsing/Model/Loop.cs` | Generic loop with specification |
| `HierarchicalLoop` | `Parsing/Model/HierarchicalLoop.cs` | HL-based parent-child loops |
| `Segment` | `Parsing/Model/Segment.cs` | Individual EDI segment |
| `SpecificationFinder` | `Parsing/SpecificationFinder.cs` | Loads embedded XML specs dynamically |

### Typed API

The project provides strongly-typed segment and loop classes in `Parsing/Model/Typed/`:

```csharp
// Typed segments follow pattern: TypedSegment{SegmentID}
TypedSegmentBIG, TypedSegmentCLM, TypedSegmentDTP, TypedSegmentN1, etc.

// Typed loops follow pattern: TypedLoop{LoopID}
TypedLoopCLM, TypedLoopNM1, TypedLoopSBR, TypedLoopIT1, etc.

// Properties follow pattern: {SegmentID}{ElementPosition}_{Description}
segment.BIG01_InvoiceDate
segment.CLM01_PatientControlNumber
```

### Specification System

X12 specifications are stored as embedded XML resources in `Parsing/Specification/`:
- Located in: `trunk/src/OopFactory.X12/Parsing/Specification/`
- Pattern: `{Version}-{TransactionSetId}.xml` (e.g., `4010-837.xml`, `5010-835.xml`)
- 150+ embedded specification files

Key specification classes:
- `TransactionSpecification` - Defines loops/segments per transaction type
- `LoopSpecification` - Defines allowed segments per loop
- `SegmentSpecification` - Segment metadata (ID, usage, repeat, elements)
- `ElementSpecification` - Element constraints (type, length, required)

## Testing

### Framework
- **Test Framework**: MSTest (Microsoft.VisualStudio.TestTools.UnitTesting)
- **Version**: Visual Studio 2010 Test Framework

### Test Organization

| Project | Focus |
|---------|-------|
| `OopFactory.X12.Tests.Unit` | Core parsing, creation, flattening, unbundling |
| `OopFactory.X12.Hipaa.Tests.Unit` | HIPAA claims and eligibility |
| `OopFactory.X12.Validation.Tests.Unit` | Acknowledgment service tests |
| `OopFactory.X12.Tests.Integration` | SQL repository integration tests |

### Test Patterns

**Data-Driven Parsing Tests** (`Parsing/ParsingTester.cs`):
```csharp
[DeploymentItem("..\\SampleEdiFileInventory.xml")]
[DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", ...)]
[TestMethod]
public void ParseToXml()
{
    // Uses XML manifest with XPath validation queries
}
```

**Creation Tests** (`Creation/*CreationTester.cs`):
```csharp
[TestMethod]
public void Create810_4010Version()
{
    var message = new Interchange(date, controlNumber, false);
    var fg = message.AddFunctionGroup("IN", date, 1);
    var trans = fg.AddTransaction("810", "0001");
    var segment = trans.AddSegment(new TypedSegmentBIG());
    // Assert expected X12 output
}
```

### Test Resources
- Sample EDI files embedded in test assemblies
- Resource path pattern: `OopFactory.X12.Tests.Unit.Parsing._SampleEdiFiles.{filename}`
- XML manifest: `SampleEdiFileInventory.xml` for data-driven tests

## Code Conventions

### Naming Conventions
- **Classes/Methods/Properties**: PascalCase
- **Private fields**: camelCase with `_` prefix (e.g., `_specFinder`)
- **Segment classes**: `TypedSegment{SegmentID}` (e.g., `TypedSegmentDTP`)
- **Loop classes**: `TypedLoop{LoopID}` (e.g., `TypedLoopCLM`)
- **Property names**: `{SegmentID}{Position}_{Description}` (e.g., `CLM01_PatientControlNumber`)

### Namespaces
```
OopFactory.X12
OopFactory.X12.Parsing
OopFactory.X12.Parsing.Model
OopFactory.X12.Parsing.Model.Typed
OopFactory.X12.Parsing.Specification
OopFactory.X12.Transformations
OopFactory.X12.Validation
OopFactory.X12.Hipaa
OopFactory.X12.Hipaa.Claims
OopFactory.X12.Repositories
```

### Design Patterns Used
- **Strategy Pattern**: `ISpecificationFinder` for spec injection
- **Factory Pattern**: `SpecificationFinder` creates specifications dynamically
- **Composite Pattern**: Container/Segment hierarchy
- **Repository Pattern**: `SqlTransactionRepository` for persistence
- **Decorator Pattern**: `TypedSegment` wraps `Segment` with strong typing

### XML Serialization
All model classes implement `IXmlSerializable` with custom `WriteXml` methods for hierarchical XML output. Serialization uses:
```csharp
interchange.Serialize()  // Returns XML string
interchange.SerializeToX12(true)  // Returns X12 with line breaks
```

### Error Handling
- `ElementValidationException` for constraint violations
- `TransactionValidationException` for transaction-level errors
- Warning event pattern: `X12ParserWarningEventHandler`
- Constructor parameter: `throwExceptionOnSyntaxErrors` controls behavior

## Common Tasks

### Parsing X12 Documents
```csharp
var parser = new X12Parser();
List<Interchange> interchanges = parser.ParseMultiple(stream);
string xml = interchanges[0].Serialize();
```

### Creating X12 Documents
```csharp
var message = new Interchange(DateTime.Now, 1, true);
var fg = message.AddFunctionGroup("HC", DateTime.Now, 1);
var trans = fg.AddTransaction("837", "0001");
var segment = trans.AddSegment(new TypedSegmentBHT());
string x12 = message.SerializeToX12(true);
```

### Custom Specification Finder
```csharp
public class MySpecFinder : SpecificationFinder
{
    public override TransactionSpecification FindTransactionSpec(
        string functionalCode, string versionCode, string transactionSetCode)
    {
        // Custom logic or call base
        return base.FindTransactionSpec(functionalCode, versionCode, transactionSetCode);
    }
}
var parser = new X12Parser(new MySpecFinder());
```

### Generating Acknowledgments
```csharp
var service = new X12AcknowledgmentService();
var ack = service.Generate997(interchange);
// or for 5010: service.Generate999(interchange);
```

## Key Files for AI Reference

When working on this codebase, these files are particularly important:

### Core Parsing
- `trunk/src/OopFactory.X12/Parsing/X12Parser.cs` - Main parser entry point
- `trunk/src/OopFactory.X12/Parsing/Model/Interchange.cs` - Root model
- `trunk/src/OopFactory.X12/Parsing/Model/Segment.cs` - Segment implementation
- `trunk/src/OopFactory.X12/Parsing/SpecificationFinder.cs` - Spec loading

### Typed API
- `trunk/src/OopFactory.X12/Parsing/Model/Typed/TypedSegment.cs` - Base typed segment
- `trunk/src/OopFactory.X12/Parsing/Model/Typed/TypedLoop*.cs` - Loop wrappers

### Validation
- `trunk/src/OopFactory.X12/Validation/X12AcknowledgmentService.cs` - 997/999 generation

### Tests (for understanding usage patterns)
- `trunk/tests/OopFactory.X12.Tests.Unit/Creation/Invoice810CreationTester.cs`
- `trunk/tests/OopFactory.X12.Tests.Unit/Parsing/ParsingTester.cs`

## Dependencies

### Internal Dependencies
- All projects reference `OopFactory.X12` core library
- `OopFactory.X12.Hipaa` depends on core and `Fonet.dll`
- `OopFactory.X12.Sql` depends on core only
- `OopFactory.X12.Validation` depends on core only

### External Dependencies
- **Fonet.dll** (`trunk/lib/`) - XSL-FO processor for PDF generation
- **System libraries only** - No NuGet packages required

## Important Notes for AI Assistants

1. **This is a .NET Framework 4.0 project** - Do not suggest .NET Core/5+ features
2. **Visual Studio 2010 format** - Project files use older MSBuild syntax
3. **No async/await** - Code uses synchronous patterns throughout
4. **Embedded resources** - Specifications are compiled into the assembly
5. **IXmlSerializable** - Custom XML serialization, not automatic serialization
6. **X12 EDI domain knowledge** - Understanding ISA/GS/ST envelopes is helpful
7. **Healthcare focus** - HIPAA 837/835/270/271 transactions are primary use cases

## Fluent API Example

The library supports a fluent builder pattern for creating documents:

```csharp
var interchange = new Interchange(DateTime.Now, 1, true)
{
    InterchangeSenderId = "SENDER",
    InterchangeReceiverId = "RECEIVER"
};

interchange
    .AddFunctionGroup("HC", DateTime.Now, 1)
    .AddTransaction("837", "0001")
    .AddSegment(new TypedSegmentBHT())
    .AddLoop(new TypedLoopNM1())
    .AddSegment(new TypedSegmentN3());
```
