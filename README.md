# 👽 gnosia-customizer — Extended Fork (WIP)

This repository is a functional fork of the original **gnosia-customizer** project.
It adds new customization systems focused on character dialogue behavior, profile editing, and data-page overrides.

This mod is built on **BepInEx 5** and **HarmonyX**, and works on the **Steam (PC) version of Gnosia**.

Original repository: https://github.com/shapeintheglass/gnosia-customizer

---

## ✅️ Added Features (Fork Enhancements)

### **1. Per-Speaker Nickname Overrides**
Characters can now refer to each other differently depending on who is speaking.
This enables:
- Calling one person by surname while calling another by a pet name
- Character-specific relationship dynamics

A mapping of **speaker → target → nickname** is supported.

---

### **2. Profile Override Support**
Fields on the crew data page can now be replaced:
- bio1
- bio2 (Gina only)

---

### **3. Age Field Now Accepts Strings**
The `age` field can now be any string, not just a number.

Examples:
- `"Unknown"`
- `"Timeless"`
- `"1,000,000 years old"`

Useful for supernatural, comedic, or abstract character concepts.

---

### **4. Special Notes Text Replacement**
All special notes can be replaced through the YAML config.
This enables:
- Rewriting or adding unique flavor text

---

## 💿️ Installation

1. Install **BepInEx 5**.
2. Run the game once to generate the `plugins/` folder.
3. Extract this fork’s release archive into: `GNOSIA/BepInEx/plugins/`
4. Ensure a folder named `gnosia_customizer` appears next to the .dll files.
5. Launch the game and verify the log output to confirm the fork loaded correctly.

### Directory Structure
<pre>
plugins/
  ├ gnosia_customizer/
  │   ├ other_textures/
  │   ├ p01/
  │   ├ p02/
  │   ├ ...
  │   └ p14/
  ├ GnosiaCustomizer.dll
  └ YamlDotNet.dll
</pre>

---

## 🗒️ Character Configuration Guide

Each character folder contains a `config.yaml` file.
The following extensions are supported by this fork.

---

### 1. Nickname Override Example
Write the following in the speaker's config.yaml:
```yaml
nicknames:
  Comet: "Comet-chan"
  Jonas: "Professor"
  SQ: "Miss SQ"
  Yuriko: "Lady Yuriko"
```

### 2. Special Notes Text Example
Add the following to the config.yaml file for the character you want to change:

```yaml
notes:
  - "Has difficulty telling lies."
  - "Gets lost even on a straight path."
  - "Rumored to have lived for centuries."
```

---

## 🙅 Current Limitations / Not Yet Supported

- Adding more `notes:` entries than the original character has slots will cause the game to treat them as locked, resulting in them not being displayed.
- Player dialogue branches are not yet customizable.
- The hidden “Love” stat still uses default values and cannot be overridden.
- Preferred / Non-preferred places during the night phase cannot be edited.
- Story cutscenes outside normal Gnosia loops (one-time events) are not affected by customization.
- Text for `stealth_grovel (dogeza)` success and failure events is not customizable.

## 🙇 Contributions Welcome

Suggestions, improvements, and forks are very welcome.
Pull requests expanding functionality or improving stability are encouraged.
