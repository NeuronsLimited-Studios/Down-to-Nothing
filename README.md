# DOWN TO NOTHING

> Si parte da una stella immensa e si scende, stadio dopo stadio, fino al **niente**.

![Unity](https://img.shields.io/badge/Unity-6000.6.0f1-black?logo=unity)
![Render Pipeline](https://img.shields.io/badge/render-URP%202D-blue)
![Version](https://img.shields.io/badge/version-1.0.0-brightgreen)

**Down to Nothing** è un gioco 2D in pixel art sviluppato da **NeuronsLimited Studios**.
Il giocatore attraversa sette stadi, ognuno con un gameplay diverso, diventando ogni
volta qualcosa di più piccolo: una stella, un pianeta, un asteroide, un iceberg, una
goccia, un atomo… e infine un'entità così instabile da rompere il gioco stesso.

---

## Come si gioca

Ogni stadio ha una **barra** in alto: le azioni giuste la riempiono, gli errori la fanno
calare. Quando la barra è piena si preme **SPAZIO** e si scende allo stadio successivo,
attraverso uno schermo bianco: *REACH THE LIMIT*.

| # | Entità | Gameplay | Controlli |
|---|---|---|---|
| 1 | **Stella** | Vaghi nello spazio e ti fai divorare dai buchi neri, che ti rimpiccioliscono. I pianeti attratti dalla tua gravità ti si schiantano addosso e ti fanno ricrescere. | WASD |
| 2 | **Pianeta** | Scorri da un lato all'altro: raccogli le sfere di energia ed evita gli asteroidi. | A / D |
| 3 | **Asteroide** | Precipiti in diagonale verso un pianeta tra satelliti e asteroidi di lava. A barra piena, lo schianto. | W / S |
| 4 | **Iceberg** | Galleggi sul mare e ti sciogli al sole; i lastroni di ghiaccio alla deriva ti raffreddano. Vento e meteo ti spingono. *(Occhio ai gamberetti.)* | A / D |
| 5 | **Goccia** | L'ultima goccia dell'iceberg scivola giù per la parete di un ghiacciaio. Sul ghiaccio asciutto ti consumi; altre gocce, brina e rigagnoli ti ostacolano. | A / D |
| 6 | **Atomo** | L'atomo di ossigeno si dondola come Tarzan con una corda agganciata alle molecole, attraversando i protoni e schivando gli elettroni. | Click (corda), WASD |
| 7 | **Entità instabile** | Il mondo microscopico si sta corrompendo: shader rotti magenta ti inseguono, le regole (tasti, gravità, tempo) smettono di funzionare e tutto ciò che tocchi si rompe davvero, fino a cancellare ogni cosa. | WASD (finché funzionano) |

Nella maggior parte degli stadi i due tasti non muovono il giocatore ma **cambiano la
direzione** in cui sta andando.

---

## Aprire il progetto

1. Installa **Unity Hub** e l'Editor **Unity 6000.6.0f1**.
2. Clona il repository:
   ```bash
   git clone https://github.com/NeuronsLimited-Studios/Down-to-Nothing.git
   ```
3. In Unity Hub: `Add` → `Add project from disk` e seleziona la cartella del progetto.
4. Apri `Assets/Scenes/Menu.unity` e premi **Play**.

---

## Stack tecnico

- **Unity** 6000.6.0f1
- **Universal Render Pipeline** (2D Renderer) con post processing
- Input: Input Manager classico + Input System
- TextMesh Pro, uGUI
- Pacchetti 2D: Sprite, Animation, Aseprite importer, PSD importer, SpriteShape, Tilemap

---

## Struttura del progetto

```
Assets/
  Scenes/       # Menu, Stage1 … Stage7
  Scripts/
    menu/       # Menu principale, impostazioni (volumi), logo
    Stage1-7/   # Gameplay ed effetti di ogni stadio
    Audio/      # Gestione suoni e musica
    StageScript.cs      # Barra di progresso e passaggio allo stadio successivo
    SceneFadeScript.cs  # Dissolvenze e schermo bianco tra gli stadi
  Prefabs/      # Pianeti, buchi neri, asteroidi, molecole…
  Sprites/      # Pixel art
  Sounds/       # Effetti e musica
  Shaders/
  Settings/     # URP, Input System, build profile
Packages/
ProjectSettings/
```

---

## Crediti

Creato da **NeuronsLimited Studios**.

## Licenza

© 2026 NeuronsLimited Studios. **Tutti i diritti riservati.**

Il codice e gli asset sono pubblici solo per essere **consultati**. Non è consentito
copiare, modificare, ridistribuire o pubblicare versioni modificate del progetto, in tutto
o in parte, senza il permesso scritto di NeuronsLimited Studios. Dettagli in [LICENSE](LICENSE).
