# Event story presentation

The event screen uses the existing event selection and effect services. Event text, prices, rewards, encounters, dungeon navigation and nested-event references remain authored in `MapEvent_SO` and its options. This change replaces their presentation, without changing their numbers or outcome probabilities.

## Scene art

`MapEventSceneArt.Resolve` first uses an event's existing `eventIllustration`. Otherwise it loads `Assets/Resources/EventUI/Scenes/NNN.png` using the numeric event family. Both underscore branches (`031_A_01`) and legacy hyphen branches (`014-1`, `025-1`) are supported. Non-numeric custom events without an illustration use the forest backdrop.

The 33 paintings cover the 33 current main event families. All 74 authored event assets resolve to their family painting or an explicit override. Follow-ups within a story retain the family's scene; authors can assign `eventIllustration` when a branch needs a different location. For a new main family, add its own painting and update `art-manifest.json`; the coverage regression will flag missing art.

`art-manifest.json` records the built-in image generation prompts, asset paths and original PNG SHA-256 checksums. The PNGs are unmodified generated paintings; Unity imports them as opaque, clamped sprites with mipmaps disabled and a 2048 maximum texture size.

## Reading and interaction

- Opening fades in over 0.38 seconds, with a gentle scene push and dialogue entrance.
- Text reveals at 42 characters per second with punctuation pauses. TMP pagination retains rich text and resource icons. The first click completes the current page; a second click advances. A long frame cannot reveal an entire unread page, and resizing preserves the reveal index.
- Options appear after the last narrative page, with a 0.24-second fade and slide. Rows share their width and height. Long labels wrap; larger option lists and consequence previews scroll independently.
- Hover or keyboard focus previews an option and selects it if available. A disabled option's requirements remain inspectable. The preview stays pinned for reading; hovering another available choice restores confirmation. Only confirmation calls the selection handler, and it rechecks current conditions.
- Actual outcome text and effect feedback play in the dialogue bar. Follow-up events reuse this flow. The final close fades for 0.28 seconds before `Hidden` performs the existing encounter or scene handoff. Input stays blocked through the fade.
- The original backpack entry opens inventory above the event. Story progress pauses while inventory or an acquisition modal obscures it. Returning refreshes availability without changing the selected choice.
- Story timing uses unscaled time. Mouse and touch use native UGUI buttons; Space/Enter provide dialogue progression when appropriate. Existing hidden-consequence rules are preserved.

## Typography

The dedicated `EventUI/Theme` references a dynamic Source Han Serif SC font asset. The original font and its SIL Open Font License are included in `Assets/Resources/EventUI/Typography/`. Upstream: [Adobe Source Han Serif](https://github.com/adobe-fonts/source-han-serif), [license](https://github.com/adobe-fonts/source-han-serif/blob/release/LICENSE.txt). The map and inventory keep their existing fonts.

## Unity MCP validation and capture

Run `MapEventStoryVisualTools.ConfigureArt()` to configure the imports and create the reading theme if needed. `MapEventStoryVisualTools.Capture(...)` renders production components in a disposable preview scene with the map scene's configured settings and a working inventory entry. Use the supplied width, height and safe-area arguments to inspect widescreen and tablet layouts.

`MapEventStoryVisualTools.Record()` records an entire opening / narration / choice / aftermath / closing sequence at a fixed frame step through the production component. Its preview handler does not execute game rewards or touch player saves. PNG frames and a state trace are written under `outputs/agent-battle/event-story-20261109/`; this ignored validation output is not a runtime dependency. Runtime tests separately exercise native raycasts, unscaled playback, repeated confirmation, backpack interruption and reopening.
