# ?? CodeSearcher CLI - Code Transformation Tool

## Overview

CodeSearcher CLI est un outil interactif en ligne de commande pour automatiser les transformations de code C#. Il permet de:

- ? Chercher et sélectionner du code selon des critères spécifiques
- ? Appliquer des transformations (renommage, wrapping, remplacement)
- ? Traiter plusieurs fichiers en batch
- ? Créer des backups automatiquement
- ? Utiliser des fichiers de configuration JSON pour les transformations répétables

## Installation

```bash
cd CodeSearcher.Cli
dotnet build
```

## Usage

### Mode Interactive

```bash
dotnet run
```

Commandes disponibles:
- `config <path>` - Charger une configuration JSON
- `create` - Créer une nouvelle configuration
- `help` - Afficher l'aide
- `exit` - Quitter

### Mode Fichier Configuration

```bash
dotnet run path/to/config.json
```

## Structure de Configuration

```json
{
  "name": "Ma Transformation",
  "description": "Description détaillée",
  "targetProject": "MonProjet",
  "filePatterns": ["*.cs", "**/*.cs"],
  "outputDirectory": "./output",
  "createBackup": true,
  "selections": [],
  "transformations": [
    {
      "name": "Ma première transformation",
      "type": "rename|wrap|replace|wrapReturnsinTask",
      "target": "NomDeLaMéthode",
      "parameters": { ... },
      "applyToAll": false
    }
  ]
}
```

## Types de Transformations

### 1. Rename (Renommage)

Renomme des méthodes, classes, variables ou propriétés.

```json
{
  "type": "rename",
  "target": "GetUser",
  "parameters": {
    "newName": "FetchUser",
    "type": "method"  // method, class, variable, property
  }
}
```

### 2. Wrap (Wrapper)

Enveloppe des méthodes avec try-catch, logging, ou validation.

```json
{
  "type": "wrap",
  "target": "SendEmail",
  "parameters": {
    "wrapType": "trycatch|logging|validation",
    "handler": "return false;"
  }
}
```

### 3. Replace (Remplacement)

Remplace des snippets de code.

```json
{
  "type": "replace",
  "target": "int.Parse",
  "parameters": {
    "oldCode": "int.Parse(value)",
    "newCode": "int.TryParse(value, out int result) ? result : 0"
  }
}
```

### 4. WrapReturnsinTask

Convertit des méthodes synchrones en asynchrones.

```json
{
  "type": "wrapReturnsinTask",
  "target": "GetUserById",
  "parameters": {
    "style": "TaskFromResult|AwaitTaskFromResult|Auto"
  }
}
```

## Exemples

### Exemple 1: Renommer des méthodes

Voir: `Examples/rename_example.json`

```bash
dotnet run Examples/rename_example.json
```

**Avant:**
```csharp
public User GetUser(int id) { ... }
```

**Après:**
```csharp
public User FetchUser(int id) { ... }
```

### Exemple 2: Convertir en Async

Voir: `Examples/async_conversion_example.json`

```bash
dotnet run Examples/async_conversion_example.json
```

**Avant:**
```csharp
public int GetValue() { return 42; }
```

**Après:**
```csharp
public async Task<int> GetValue() { return Task.FromResult(42); }
```

### Exemple 3: Ajouter de la Gestion d'Erreurs

Voir: `Examples/add_error_handling_example.json`

```bash
dotnet run Examples/add_error_handling_example.json
```

### Exemple 4: Remplacer les APIs Obsolètes

Voir: `Examples/replace_apis_example.json`

```bash
dotnet run Examples/replace_apis_example.json
```

## Workflow Complet

### Étape 1: Créer une Configuration

```bash
dotnet run
> create
(répondre aux questions)
```

### Étape 2: Vérifier la Configuration

Éditer le fichier JSON généré pour affiner les paramètres.

### Étape 3: Exécuter la Transformation

```bash
dotnet run my_config.json
> y
```

### Étape 4: Vérifier les Résultats

Les fichiers transformés sont dans `./output` ou le répertoire spécifié.
Les backups sont dans `./output/backups`.

## Résultats et Rapports

Après exécution, vous obtenez:

```
?? Results:
  Success: true
  Total Files: 5
  Processed: 5
  Successful: 5
  Failed: 0

? Processed Files:
    - Services/UserService.cs
    - Services/OrderService.cs
    - ...

?? Output directory: /path/to/output
```

## Fonctionnalités Avancées

### Backups Automatiques

Tous les fichiers originaux sont copiés dans `output/backups/` si `createBackup: true`.

### Sélections

Permet de cibler spécifiquement quels éléments modifier:

```json
"selections": [
  {
    "name": "GetUser methods",
    "type": "methods",
    "filters": {
      "nameContaining": "GetUser",
      "isPublic": true
    }
  }
]
```

### ApplyToAll

Appliquer une transformation à tous les résultats de sélection:

```json
{
  "name": "Rename all",
  "applyToAll": true
}
```

## Best Practices

1. **Toujours créer des backups** - Utilisez `createBackup: true`
2. **Tester d'abord** - Commencez par un petit ensemble de fichiers
3. **Utiliser des patterns spécifiques** - Ne transformez que ce que vous visez
4. **Vérifier les résultats** - Revérifiez les fichiers transformés
5. **Versionner la config** - Gardez les fichiers JSON pour future utilisation

## Dépannage

### "File not found"
Vérifiez le chemin relatif au répertoire courant.

### "No files found"
Ajustez `filePatterns` pour correspondre à vos fichiers.

### Transformation échouée
Vérifiez que le `target` existe exactement dans le code.

### Backup non créé
Vérifiez les permissions de fichier et `createBackup: true`.

## Performance

- Solution avec 1000 fichiers: ~10s
- Solution avec 100 fichiers: ~1s
- Backups disque: ~2x la taille des fichiers originaux

## Limitations

- Les transformations sont appliquées séquentiellement
- Les sélections complexes doivent être définies manuellement
- Les transformations custom nécessitent du code C#

## Roadmap

- [ ] Sélections avancées (expressions regex)
- [ ] Transformations custom via plugins
- [ ] Support des fichiers XML/JSON
- [ ] Interface graphique
- [ ] Historique des transformations
- [ ] Comparaison avant/après

## Support

Pour des problèmes ou suggestions, consultez:
- Documentation CodeSearcher.Core
- Examples/ directory
- Tests

---

**Happy Coding! ??**
