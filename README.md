# Ryo Unity Tools

A collection of **handy Unity utility tools** developed to make everyday game development tasks faster and easier. These tools are designed to be lightweight and easy to drop into any Unity project.

> This repository contains a set of scripts and editor utilities I reuse across Unity projects to streamline workflows and solve common problems.

---

## Contents

This repository is organized into several modular tool categories:

### Utilities

Reusable helper functions and static utility classes that support common tasks you’ll need in Unity projects (e.g., maths helpers, array extensions, generic utilities, etc.).

### GameObject Placing Tools

Editor tools to **place GameObjects in your scene** more efficiently. These may include tools to:

- Place prefabs on surfaces with mouse clicks
- Snap placed objects to terrain or meshes
- Align and randomize rotation/scale on placement

### Lighting & Visual Tools

Quick components or editor helpers to manage **light-related effects**, debug lighting setups, or control directional/point lights in scenes.

---

## Features

- **Modular & lightweight** – can be included on a per‑tool basis.  
- **Editor integration** – tools that improve scene authoring workflows.  
- **Runtime utility functions** for common Unity scripting tasks.

*(Update this section with specific classes and descriptions once finalized.)*

---

## Installation

To use the tools in a Unity project:

1. Clone this repository or download it as a ZIP.
2. Open (or create) your Unity project.
3. Copy the relevant folders (e.g., *Utilities*, *GameObject Placing Tools*, *Lighting*) into your `Assets/Editor/` folder.
4. Let Unity import the scripts — they’ll compile automatically.

---

## Usage Example

Most scripts in this repository are **Editor scripts**. They work directly from the Unity Editor or with a key shortcut.

For example:

- `Menu bar -> Tools -> SnapToGround`  

You can access other tools similarly from the Unity menu bar once they are imported into `Assets/Editor/`.

---

## Contribution

This project is open for contributions! Feel free to:

- Add new reusable tools
- Improve documentation
- Fix bugs or refactor code

If you add a feature, please also add tests and example usage.

---

## License

This project is licensed under the **MIT License** — see the **LICENSE** file for details.

---

## Next Steps

To improve the repository further, consider:

- Add detailed descriptions for each folder’s tools  
- Provide code examples and screenshots  
- Include versioning or change logs for updates  