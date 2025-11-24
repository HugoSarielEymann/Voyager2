# ?? DIAGNOSTIC TESTS - Identification des Problèmes

## Tests à Vérifier

### AdvancedFeaturesTests

1. **Logger Tests** ?
   - `FindMethods_WithConsoleLogger_LogsSelections`
   - `FindMethods_WithMemoryLogger_TracksAllOperations`
   - `FindMethods_WithNullLogger_NoLogging`
   - `CodeContext_WithCustomLogger_UsesInjectedInstance`

2. **Conditional Analysis Tests**
   - `FindConditionsLeadingTo_VariableInNestedIf_ReturnsConditions`
   - `FindConditionsLeadingTo_VarInForLoop_ReturnsForCondition`
   - `FindConditionsLeadingTo_VarInForeachLoop_ReturnsForeachCondition`
   - `FindConditionsLeadingTo_VarInWhileLoop_ReturnsWhileCondition`
   - `FindConditionsLeadingTo_ComplexNesting_ReturnsAllConditions`
   - `FindConditionsLeadingTo_StatementDirectlyInMethod_ReturnsNoConditions`
   - `IsStatementReachable_VarInIfCondition_IsReachable`
   - `IsStatementUnconditionallyReachable_DirectStatement_IsTrue`
   - `IsStatementUnconditionallyReachable_ConditionalStatement_IsFalse`
   - `FindAllConditionalPaths_InMethod_ReturnsAllPaths`
   - `FindConditions_WithLogging_LogsConditionalAnalysis`

## Problèmes Identifiés

### Issue 1: ConsoleLogger / MemoryLogger / NullLogger n'existent pas
Besoin de créer ces classes dans CodeSearcher.Core.Abstractions

### Issue 2: Logiques conditionnelles complexes
Les tests supposent des résultats très précis (ex: exactement 2 ou 3 conditions)
Besoin de vérifier la logique dans `ConditionalAnalyzer`

## Actions à Prendre

1. ? Créer les loggers manquants
2. ? Corriger ConditionalAnalyzer si nécessaire
3. ? Tester et valider

