# Prompt 30: Validation Service

Continuing Factory Configurator. Prompt 29 complete, 133 tests passing, routing canvas complete.

Objective: Implement factory-wide validation. All tests must pass.

## 1. Models

### ValidationResult.cs:
- IsValid (bool)
- Errors (List<ValidationIssue>)
- Warnings (List<ValidationIssue>)
- ErrorCount (computed)
- WarningCount (computed)

### ValidationIssue.cs:
- Category (string) - "Parts", "Workstations", "Routings", "Operators", "Transport"
- Severity (ValidationSeverity enum) - Error, Warning
- EntityType (string) - "PartFamily", "PartVariant", "Workstation", etc.
- EntityId (int?)
- EntityName (string?)
- Message (string)

### ValidationSeverity enum:
- Error
- Warning

## 2. Service

### IValidationService:
- Task<ValidationResult> ValidateFactoryAsync(int scenarioId)
- Task<ValidationResult> ValidateVariantAsync(int variantId, int scenarioId)
- Task<ValidationResult> ValidateWorkstationAsync(int workstationId, int scenarioId)
- Task<ValidationResult> ValidateRoutingAsync(int routingId, int scenarioId)

### ValidationService.ValidateFactoryAsync checks:

#### Parts:
- Warning: Variant has no workstation assignments
- Warning: FinishedGood variant has no BOM
- Warning: FinishedGood variant has no routing

#### Workstations:
- Warning: Workstation has no process times defined (no default, no specific)
- Warning: Workstation has required skills but no qualified operators

#### Routings:
- Error: Routing has no Start node
- Error: Routing has unreachable nodes
- Error: Routing step references deleted workstation (WorkstationId not null but not found)
- Warning: Routing step has no workstation assigned
- Warning: Routing has connections without transport times

#### Operators:
- Warning: Operator has certification expiring within 30 days

#### Transport:
- Warning: No available transport equipment of any category

## 3. Tests in Tests/Services/ValidationServiceTests.cs

- Validate_CompleteFactory_ReturnsValid
- Validate_VariantNoWorkstation_ReturnsWarning
- Validate_WorkstationNoProcessTime_ReturnsWarning
- Validate_RoutingInvalid_ReturnsError
- Validate_NoQualifiedOperators_ReturnsWarning
- Validate_ExpiringCertification_ReturnsWarning
- ValidateVariant_ChecksOnlyThatVariant

## Run Tests

Run dotnet test. All 140 tests should pass.
