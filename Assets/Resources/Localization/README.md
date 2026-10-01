# English / 简体中文

The initial language is English, independent of the operating-system language.
Change it under Settings → Gameplay → Language. Changes apply immediately and
are saved separately from graphics/control drafts as `DarkBrine.Settings.Language`.

Translations live in `Assets/Game/Prefabs/UI/GameLocalization.cs`. Literal UI
labels use `LocalizedGameText`; formatted content uses `GameLocalization.Format`.
Equipment IDs, saves, key bindings and gameplay values are not translated.

Chinese uses readable controls and a light meme tone in character/flavour text.
The enemy's displayed names are “Karen Fairy” and “猪妖小仙人”. Legacy name
strings remain lookup aliases only, so existing prefabs and equipment saves
continue to work without exposing the previous name in the UI.
Sahur keeps its established name rather than translating the underlying cultural
term literally. “这把先寄了” and “技能还在修炼” are original UI adaptations.

Meme references consulted:
- https://dailydot.com/tung-tung-tung-sahur-meme-explained

Bundled typeface: Noto Sans CJK SC Regular, from the Noto Fonts project.
Source: https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf
License: SIL Open Font License 1.1, included as `OFL.txt`.
The bundled font supports both Unity legacy UI and a persistent TextMesh Pro dynamic atlas;
Chinese rendering does not depend on fonts installed on the player's computer.
