# DOWN TO NOTHING

> Start as an immense star and go down, stage after stage, until there is **nothing** left.

![Unity](https://img.shields.io/badge/Unity-6000.6.0f1-black?logo=unity)
![Render Pipeline](https://img.shields.io/badge/render-URP%202D-blue)
![Version](https://img.shields.io/badge/version-1.0.0-brightgreen)

**Down to Nothing** is a 2D pixel art game made by **NeuronsLimited Studios**.
The player goes through seven stages, each with a different gameplay, becoming something
smaller every time: a star, a planet, an asteroid, an iceberg, a drop, an atom… and
finally an entity so unstable that it breaks the game itself.

---

## How to play

Every stage has a **bar** at the top: the right moves fill it, mistakes drain it. When
the bar is full, press **SPACE** to go down to the next stage, through a white screen:
*REACH THE LIMIT*.

| # | Entity | Gameplay | Controls |
|---|---|---|---|
| 1 | **Star** | Wander through space and let black holes devour you to shrink. Planets pulled in by your gravity crash into you and make you grow back. | WASD |
| 2 | **Planet** | Slide from side to side: collect the energy orbs and dodge the asteroids. | A / D |
| 3 | **Asteroid** | Fall diagonally toward a planet, between satellites and lava asteroids. When the bar is full: the crash. | W / S |
| 4 | **Iceberg** | Float on the sea and melt in the sun; drifting ice floes cool you down. Wind and weather push you around. *(Watch out for shrimps.)* | A / D |
| 5 | **Drop** | The last drop of the iceberg slides down the wall of a glacier. Dry ice wears you down; other drops, frost and streams get in your way. | A / D |
| 6 | **Atom** | The oxygen atom swings like Tarzan on a rope hooked to the molecules, going through the protons and dodging the electrons. | Click (rope), WASD |
| 7 | **Unstable entity** | The microscopic world is getting corrupted: broken magenta shaders chase you, the rules (keys, gravity, time) stop working, and everything you touch really breaks, until everything is deleted. | WASD (while they work) |

In most stages the two keys don't move the player: they **switch the direction** it's
going.

---

## Opening the project

1. Install **Unity Hub** and the **Unity 6000.6.0f1** Editor.
2. Clone the repository:
   ```bash
   git clone https://github.com/NeuronsLimited-Studios/Down-to-Nothing.git
   ```
3. In Unity Hub: `Add` → `Add project from disk` and select the project folder.
4. Open `Assets/Scenes/Menu.unity` and press **Play**.

---

## Tech stack

- **Unity** 6000.6.0f1
- **Universal Render Pipeline** (2D Renderer) with post processing
- Input: classic Input Manager + Input System
- TextMesh Pro, uGUI
- 2D packages: Sprite, Animation, Aseprite importer, PSD importer, SpriteShape, Tilemap

---

## Project structure

```
Assets/
  Scenes/       # Menu, Stage1 … Stage7
  Scripts/
    menu/       # Main menu, settings (volumes), logo
    Stage1-7/   # Gameplay and effects of each stage
    Audio/      # Sounds and music
    StageScript.cs      # Progress bar and moving on to the next stage
    SceneFadeScript.cs  # Fades and the white screen between stages
  Prefabs/      # Planets, black holes, asteroids, molecules…
  Sprites/      # Pixel art
  Sounds/       # Sound effects and music
  Shaders/
  Settings/     # URP, Input System, build profile
Packages/
ProjectSettings/
```

---

## Credits

Made by **NeuronsLimited Studios**.

## License

© 2026 NeuronsLimited Studios. **All rights reserved.**

The code and assets are public for **viewing only**. Copying, modifying, redistributing or
publishing modified versions of the project, in whole or in part, is not allowed without
written permission from NeuronsLimited Studios. See [LICENSE](LICENSE) for details.
