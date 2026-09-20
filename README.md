# DOWN TO NOTHING

> Gioco Unity in cui si **scende di scala**: si parte da un'entità immensa, una stella
> chiamata **Absolute**, e a ogni stadio si diventa qualcosa di più piccolo, fino a
> un'entità così instabile da **corrompere tutte le regole del gameplay**.

![Unity](https://img.shields.io/badge/Unity-6000.6.0f1-black?logo=unity)
![Render Pipeline](https://img.shields.io/badge/render-URP%202D-blue)
![Input](https://img.shields.io/badge/input-New%20Input%20System-blueviolet)
![Status](https://img.shields.io/badge/status-concept-orange)

---

## Concept

Il giocatore parte dal punto più alto della scala dell'esistenza: **Absolute**, una
stella immensa. Il gioco è diviso in **stadi**, e ogni stadio ha un **gameplay
diverso** dagli altri. Alla fine di ognuno si **scende di entità**: da stella a
pianeta, e così via, sempre più in basso, sempre più piccoli.

In fondo alla scala c'è un'entità minuscola e **talmente instabile da corrompere
tutte le regole del gameplay**. È lì che la storia del gioco finisce.

### Discesa

Scendere non è solo cambiare ambientazione: cambia il **modo di giocare**. Ogni
transizione chiude uno stadio, cambia l'entità controllata e porta il giocatore in un
gameplay nuovo. Più si scende, più l'entità è piccola e meno stabile.

### Pilastri di design (draft)

1. **Ogni stadio è un gioco a sé.** Cambiare entità significa cambiare gameplay, non
   solo grafica.
2. **La scala è il tema.** Dall'immenso al minuscolo: dimensione e prospettiva cambiano
   a ogni discesa.
3. **Il finale rompe le regole.** L'ultima entità è instabile e corrompe il gameplay
   stesso: è così che la storia si chiude.

---

## Loop di gioco

```
Stadio N ──► gameplay dello stadio ──► fine stadio ──► discesa di entità
   ▲                                                          │
   └──────────────── stadio N+1, nuove regole  ◄──────────────┘

Ultimo stadio: entità instabile ──► regole corrotte ──► fine della storia
```

---

## Scala degli stadi (draft)

| # | Entità | Scala | Gameplay |
|---|---|---|---|
| 1 | **Absolute** (stella, nome provvisorio) | Immensa | Da definire |
| 2 | Pianeta | Molto grande | Da definire |
| … | … | … | Da definire |
| Finale | Entità instabile | Minuscola | Le regole del gameplay si corrompono |

Gli stadi intermedi e il gameplay di ciascuno sono ancora da decidere.

---

## Come aprire il progetto

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
- **Universal Render Pipeline** 17.6.0 (2D Renderer)
- **New Input System** (`Assets/Settings/InputSystem_Actions.inputactions`)
- Pacchetti 2D: Animation, Aseprite importer, PSD importer, SpriteShape, Tilemap (+ Extras)
- Timeline, uGUI

---

## Struttura del progetto

```
Assets/
  Scenes/          # Menu
  Settings/        # URP, Input System, template scena 2D lit
Packages/          # manifest.json + lock dei pacchetti
ProjectSettings/   # Impostazioni progetto Unity
```

---

## Roadmap

- [ ] **M0 – Fondamenta:** struttura del progetto, menu, gestione di scene e stadi, input
- [ ] **M1 – Primo stadio:** Absolute giocabile dall'inizio alla fine
- [ ] **M2 – Discesa:** transizione di entità tra uno stadio e il successivo
- [ ] **M3 – Stadi intermedi:** un gameplay diverso per ogni entità della scala
- [ ] **M4 – Stadio finale:** l'entità instabile e la corruzione delle regole
- [ ] **M5 – Storia:** narrativa e chiusura del gioco
- [ ] **M6 – Audio & feel:** musica, effetti e resa visiva di ogni scala

---

## Licenza

Nessuna licenza definita — tutti i diritti riservati fino a decisione contraria.
