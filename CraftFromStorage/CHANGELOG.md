# Changelog

## 1.0.0

- Add focused station crafting and equipment upgrades from accessible storage within a fixed 20m player-centered sphere.
- Keep inventory-first consumption, nearest storage ordering, normal access rules and vanilla station/recipe checks.
- Include storage in crafting requirement checks and coloring.
- Add exact ingredient planning, vanilla chest ownership requests, cancellation/timeout handling and signature/behavior validation.
- Cancel safely when storage is invalidated during an ownership wait; allow retries and validated manual opening after lost responses while suppressing unsolicited late replies.
- Exercise production Harmony patches and exception cleanup in independent, documented behavior and integration tests.
- Follow the shared mod build, deployment, icon and Thunderstore package structure.
- Complete local in-game testing; dedicated multiplayer and persistence validation remains outstanding.