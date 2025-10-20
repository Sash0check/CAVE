# 🕳️ Unity CAVE System

A modular **CAVE (Cave Automatic Virtual Environment)** generator for Unity — designed to create immersive multi-display projection environments with configurable screen geometry, camera alignment, and real-time compositing of multiple render textures into one output texture.

---

## 🚀 Features

- 🧩 **Config-driven setup**  
  Build and layout the entire CAVE environment using a simple JSON configuration file.
  
- 🎥 **Multi-display support**  
  Each wall renders from its own camera with proper alignment and projection.

- 🧱 **Automatic wall generation**  
  Front, left, right, top, bottom walls are built dynamically based on configuration.

- 🔭 **View-perfect calibration**  
  Cameras are positioned and rotated so that projected 3D objects transition seamlessly across walls.

- 🖼️ **Merged render texture output**  
  All wall render textures are combined into one final composite texture — suitable for streaming, recording, or network transport.

- 🕹️ **Root transform control**  
  The entire CAVE can be moved or rotated by manipulating the root `CAVE` GameObject in the scene.

---

## 🧰 Requirements

- **Unity 2021.3+**
- Built-in Render Pipeline (URP/HDRP may require small material adjustments)
- Spout plugin fot Unity: https://github.com/keijiro/KlakSpout
- C# 8.0 support

---

## ⚙️ Configuration

CAVE structure is defined in a simple JSON file:

```json
{
    "eyePosition": [ 0, 0, 0 ],
    "displays": [
        {
            "name": "Front",
            "DisplayIndex": "2",
            "width": 1.92,
            "height": 1.08,
            "textureWidth": 1920,
            "textureHeight": 1080
        },
        {
            "name": "Left",
            "DisplayIndex": "1",
            "width": 1.92,
            "height": 1.08,
            "textureWidth": 1920,
            "textureHeight": 1080
        },
        {
            "name": "Right",
            "DisplayIndex": "3",
            "width": 1.92,
            "height": 1.08,
            "textureWidth": 1920,
            "textureHeight": 1080
        }
    ]
}
```

| Field                           | Type   | Description                                           |
| ------------------------------- | ------ | ----------------------------------------------------- |
| `DisplayIndex`                  | string | Unity display index (0 = main, 1 = secondary, etc.)   |
| `width`, `height`               | float  | Physical wall size in meters                          |
| `textureWidth`, `textureHeight` | int    | Per-wall render texture resolution                    |

---

🧩 Usage

Create empty GameObject → name it CAVE

Attach the CAVEBuilder script

Add layer "CaveWalls" in ProjectSettings

Assign:

JSON config file path (relative or absolute)

Base material for wall surfaces

Press Play
→ The system builds the walls, cameras, and render targets automatically. Also it creates SpoutSender for each wall.
