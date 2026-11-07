# Repository coding standards

Apply these rules to all maintained C# source and tests. Preserve behavior when making style-only changes.

## Agent knowledge workflow

- At the start of each repository task, read the [mod API reference index](docs/reference/README.md). For mod implementation, debugging, review, or API questions, read the relevant topic pages before searching libraries or inspecting assemblies. Documentation-only tasks need only the index and affected pages.
- Reuse documented APIs and repository examples first. Load only relevant topics; do not dump entire assemblies or load the whole reference into every session.
- Follow the [API discovery skill](.github/skills/valheim-api-discovery/SKILL.md) when coverage is missing, uncertain, contradicted, or stale. Missing documentation is not evidence that functionality does not exist.
- As part of the same task, update the reference with reusable verified discoveries, corrected assumptions, compatibility changes, and important bounded negative findings. Add new topics to the index. Do not wait for the user to request maintenance.
- Record evidence level, verification date, assembly fingerprint when applicable, and remaining unknowns. Never present repository usage, stub tests, or signature inspection as proof of live game behavior. Revalidate only affected knowledge when assemblies change; do not silently relabel old findings as current.
- Before completion, run the [reference validator](scripts/tests/Test-AgentReference.ps1) after reference, discovery skill, or workflow changes, and report relevant knowledge updates and unresolved verification limits. For runtime changes also follow the normal build, test, and in-game validation requirements.

## All project files

- Every maintained repository text file must end immediately after its last content character: no final newline, trailing blank line, or trailing whitespace at EOF. This applies to source, scripts, documentation, configuration, and project/solution metadata, not just C# or `.csproj` files.
- Preserve binary files and generated artifacts; do not rebuild distributions just to change file endings.
- Follow the root `.editorconfig` setting `insert_final_newline = false`. This overrides the usual final-newline convention, including SA1518 guidance below.

## C# layout

- Use file-scoped namespaces, one namespace per file. Test doubles for Valheim's global-namespace API must remain global; keep those in separate files.
- Put `using` directives before the file-scoped namespace declaration.
- Remove unused `using` directives, including unused aliases and static imports, from every maintained C# file (IDE0005). Keep only imports required by that file.
- Do not prefix types with `global::` unless a real name-resolution conflict requires it. Prefer clear member names that do not shadow types; explain any unavoidable qualification.
- Use four-space indentation.
- Put a blank line before `return`, `yield return`, or `yield break` when another statement precedes it in the same block. Do not add a leading blank line when it is the first statement. Keep any comment describing the return directly attached to it, placing the separator before the comment instead. Expression-bodied members need no extra blank line.
- **REPO-LAYOUT-001:** Put a blank line before a block-form `if`, `for`, `foreach`, `while`, `do`, `switch`, `try`, `using`, `lock`, `fixed`, `checked`, `unchecked` or `unsafe` statement when another statement precedes it in the same scope. Do not add a leading blank line at the start of a scope, split a statement header, separate attached comments from their statement, or detach `else`, `catch`, `finally` or a `do`-`while` footer. This is a repository rule, not SA1513.

## Explicit exceptions and external contracts

- Do not apply SA1200, SA1301 or SA1101. Put usings before namespaces, and do not add `this.` prefixes.
- Reverse SA1309: ordinary fields, including static and readonly fields, use `_camelCase`. Constants retain `PascalCase`. The one leading underscore is also an explicit exception to SA1300/SA1304/SA1306/SA1307/SA1310 where their default field rules conflict; do not use additional underscores or type/scope prefixes.
- Keep externally required names and member kinds intact: Valheim/Unity API test doubles mirror the game's fields, properties, global namespaces and nested types; Harmony injection parameters keep names such as `__instance`, `__result`, `__state` and `__0`. Do not rename those contracts for style compliance.
- Constants remain constants, including plugin metadata used in attributes and package-version checks. SA1401 applies to ordinary class fields, not constants. Use properties to expose other state.
- Related nested implementation types may stay inside their owning type. SA1402 requires one top-level type per file, with matching file and type names.

## Maintainability

Simplified from the [StyleCop maintainability rules](https://documentation.help/StyleCop/Maintainability%20Rules.html):

| Rule | Required practice |
| --- | --- |
| SA1119 | Remove redundant parentheses; retain parentheses that make mixed-operator precedence explicit. |
| SA1400 | Declare access explicitly on types and members that support an access modifier. Interface members may retain implicit public access. |
| SA1401 | Keep ordinary class fields private; expose state through properties. Preserve the external contracts noted above. |
| SA1402 | Keep one top-level type per file; name the file after the type. |
| SA1403 | Keep one namespace per file; use a file-scoped namespace. |
| SA1404 | Give every code-analysis suppression a specific justification. |
| SA1405 | Supply an explanatory message to `Debug.Assert`. |
| SA1406 | Supply an explanatory message to `Debug.Fail`. |
| SA1407 | Parenthesize mixed arithmetic operators to show evaluation order without changing it. |
| SA1408 | Parenthesize mixed conditional operators, especially combinations of `&&` and `||`. |
| SA1409 | Remove redundant/empty constructs only when they have no behavioral purpose; intentional test no-ops are not redundant. |
| SA1410 | Omit the optional parameter parentheses in parameterless anonymous `delegate` expressions. |
| SA1411 | Omit empty attribute argument parentheses, for example `[HarmonyPrefix]`. |

## Naming

Simplified from the [StyleCop naming rules](https://documentation.help/StyleCop/Naming%20Rules.html), with the explicit field and API exceptions above:

| Rule | Required naming |
| --- | --- |
| SA1300 | Use `PascalCase` for namespaces, types, delegates, events, methods and properties. |
| SA1302 | Prefix interface names with `I`, followed by an uppercase letter. |
| SA1303 | Use `PascalCase` for constants. |
| SA1304 | Non-private readonly state should be a PascalCase property backed by a private `_camelCase` field when needed. |
| SA1305 | Use descriptive names, not Hungarian/type prefixes. |
| SA1306 | Use `camelCase` for locals and parameters; ordinary fields use `_camelCase`. |
| SA1307 | Expose state through PascalCase properties rather than accessible ordinary fields. |
| SA1308 | Do not prefix local variables or parameters; preserve required Harmony injection names. |
| SA1309 (reversed) | Start ordinary field names with exactly one underscore: `_camelCase`. Do not prefix constants. |
| SA1310 (adapted) | Do not put underscores inside names beyond the required leading field underscore or an external API contract. |

## Spacing (StyleCop SA1000-SA1027)

Simplified from the [StyleCop spacing rules](https://documentation.help/StyleCop/Spacing%20Rules.html):

| Rule | Required spacing |
| --- | --- |
| SA1000 | Use one space after control-flow/query keywords; none between `typeof`, `sizeof`, `checked`, `unchecked` or `default` and `(`. Use `new Type`, `new(...)` and `new[]` as appropriate to the syntax. |
| SA1001 | Put no space before a comma and one space after it unless followed by a newline. |
| SA1002 | Put no space before a semicolon; separate following same-line code with one space. |
| SA1003 | Put spaces around binary/assignment/conditional operators; keep unary operators attached to their operands. |
| SA1004 | Begin XML documentation text with one space after `///`. |
| SA1005 | Begin ordinary comment text with one space after `//`. |
| SA1006 | Do not insert whitespace between `#` and a preprocessor keyword. |
| SA1007 | Put a space after the declaration keyword `operator`. |
| SA1008 | Do not put whitespace just inside `(`; keep calls attached to the method name. |
| SA1009 | Do not put whitespace just inside `)`; space following tokens according to their syntax. |
| SA1010 | Keep index/array `[` attached to the preceding expression/type, with no inner padding. |
| SA1011 | Do not put whitespace before `]`; space following tokens according to their syntax. |
| SA1012 | If an opening brace shares a line with code, separate surrounding code with spaces. Multiline braces follow SA1500. |
| SA1013 | If a closing brace shares a line with code, use inner spacing; attach closing punctuation. |
| SA1014 | Keep generic `<` attached to its type/method name, with no inner padding. |
| SA1015 | Do not pad generic `>` internally or separate consecutive closing generic brackets. |
| SA1016 | Do not pad an attribute's opening `[` internally. |
| SA1017 | Do not pad an attribute's closing `]` internally; separate following same-line attributes. |
| SA1018 | Attach a nullable type's `?` to the type name. |
| SA1019 | Do not put spaces around member access `.`, `?.` or qualification `::`. |
| SA1020 | Attach `++` and `--` to their operands. |
| SA1021 | Attach unary `-` to its operand; space binary subtraction. |
| SA1022 | Attach unary `+` to its operand; space binary addition. |
| SA1023 | Attach pointer dereference/address-of operators to their operands; keep pointer type syntax unpadded. |
| SA1024 | Space inheritance/constraint/constructor and conditional colons on both sides; do not put a space before a label/case/named-argument colon. |
| SA1025 | Use one space between tokens, not repeated spaces for alignment; indentation is separate. |
| SA1026 | Write implicitly typed arrays as `new[]`, not `new []`. |
| SA1027 | Use spaces, not tabs, in C# files. |

## Readability

Simplified from the [StyleCop readability rules](https://documentation.help/StyleCop/Readability%20Rules.html); SA1101 is excluded:

| Rule | Required practice |
| --- | --- |
| SA1100 | Use `base.` only when disambiguating an inherited member hidden/overridden locally. |
| SA1102 | Keep each LINQ query clause immediately after its preceding clause; no intervening blank lines. |
| SA1103 | Keep query clauses all on one line or put each on its own line. |
| SA1104 | Start a new query clause on a new line when the previous clause is multiline. |
| SA1105 | Start every multiline query clause on its own line. |
| SA1106 | Remove empty `;` statements; use explicit blocks for intentional empty bodies. |
| SA1107 | Put separate statements on separate lines. |
| SA1108 | Do not place comments between a block statement's header and opening brace. |
| SA1109 | Do not place regions between a block statement's header and opening brace. |
| SA1110 | Keep the opening parameter parenthesis/bracket on the declaration/call line. |
| SA1111 | Keep the closing parenthesis/bracket on the last parameter's line. |
| SA1112 | Keep empty parameter parentheses/brackets together on one line. |
| SA1113 | Keep each separating comma on the preceding parameter's line. |
| SA1114 | Start the parameter list immediately after the declaration/call, without blank lines. |
| SA1115 | Do not put blank lines between parameters. |
| SA1116 | For a wrapped parameter list, start the first parameter on the line after the opening parenthesis. |
| SA1117 | Put parameters all on one line or each on a separate line; do not mix the two layouts. |
| SA1118 | Only the first argument may span multiple lines, except anonymous functions, which may do so in any position. Extract other complex arguments into named locals. |
| SA1120 | Comments must contain meaningful text; do not add empty comment markers. |
| SA1121 | Use built-in type aliases such as `int`, `string` and `object` instead of `Int32`, `String` or `Object`. |
| SA1122 | Use `string.Empty`, not `""`, for empty strings. |
| SA1123 | Do not place regions inside members. |
| SA1124 | Do not use regions. |
| SA1125 | Use nullable shorthand such as `int?`, not `Nullable<int>`. |

## Layout (StyleCop SA1500-SA1518)

Simplified from the [StyleCop layout rules](https://documentation.help/StyleCop/Layout%20Rules.html):

| Rule | Required layout |
| --- | --- |
| SA1500 | In multiline statements, declarations and expressions, put opening and closing braces on their own lines. Closing punctuation such as `};` or `});` stays attached. |
| SA1501 | Expand brace-delimited statement blocks across multiple lines; do not write an entire statement block on one line. |
| SA1502 | Expand brace-delimited declarations across multiple lines. Short accessors may be single-line; expression-bodied members and automatic properties remain allowed. |
| SA1503 | Always use braces for control-flow bodies, even when they contain only one statement. |
| SA1504 | Within a property, indexer or event, make all accessors consistently single-line or consistently multiline. |
| SA1505 | Do not put a blank line immediately after an opening brace. |
| SA1506 | Keep XML documentation attached to its declaration and attributes; no intervening blank line. |
| SA1507 | Do not use multiple consecutive blank lines. |
| SA1508 | Do not put a blank line immediately before a closing brace. |
| SA1509 | Do not put a blank line between a construct's header and its opening brace. |
| SA1510 | Keep related `else`, `catch` and `finally` blocks attached to the preceding block; no intervening blank line. |
| SA1511 | Keep a `do` block's trailing `while` attached to its closing brace; no intervening blank line. |
| SA1512 | Keep a standalone `//` comment block attached to the code it describes; no following blank line. Separate comment blocks may have a blank line between them. |
| SA1513 | Put a blank line after a closing brace before the next independent statement or declaration, but not before a related `else`, `catch`, `finally`, `do`-`while` footer, closing delimiter or enclosing closing brace. |
| SA1514 | Put a blank line before XML documentation, unless it is the first item in its scope. |
| SA1515 | Put a blank line before a standalone `//` comment block, unless it is the first item in its scope. Keep consecutive lines of the same comment block together. |
| SA1516 | Separate adjacent declarations and multiline accessors with a blank line. Consecutive fields may remain grouped. |
| SA1517 | Do not start a file with blank lines. |
| SA1518 (overridden) | End immediately after the last content character, with no final newline or trailing blank lines, as required for all project text files. |

## Ordering (StyleCop SA1201-SA1213)

Simplified from the [StyleCop ordering rules](https://documentation.help/StyleCop/Ordering%20Rules.html):

| Rule | Required ordering |
| --- | --- |
| SA1201 | File/namespace contents: extern aliases, usings, namespaces, delegates, enums, interfaces, structs, classes. Type members: fields, constructors, finalizers, delegates, events, enums, interfaces, properties, indexers, methods, structs, classes. |
| SA1202 | Within each member kind, order access: `public`, `internal`, `protected internal`, `protected`, `private`. |
| SA1203 | Put constants before non-constant fields. |
| SA1204 | Within the same member kind and access group, put static members before instance members. |
| SA1205 | Declare explicit access on every partial type/member. |
| SA1206 | Declaration modifiers: access, then `static`, then remaining modifiers. |
| SA1207 | Write `protected internal`, never `internal protected`. |
| SA1208 | Put `System` usings before other namespace usings. |
| SA1209 | Put using aliases after ordinary usings. |
| SA1210 | Sort ordinary usings alphabetically within the System and non-System groups. |
| SA1211 | Sort using aliases alphabetically by alias name. |
| SA1212 | Put `get` before `set` (or `init`) in properties/indexers. |
| SA1213 | Put event `add` before `remove`. |

These are ordering rules, not a requirement to alphabetize fields, properties, or methods. Preserve initialization dependencies when reordering fields.

## Validation and distributions

- Follow [docs/Development.md](docs/Development.md) for builds, validation, deployment and packaging.
- Run relevant builds/tests for C# changes. Signature checks and stub tests do not replace in-game validation.
- Keep prepared mod `dist/` DLLs and ZIPs in version control. Do not regenerate ZIPs on each edit; package only after changes are satisfactory and tested.