# CatsEscape 🐾

CatsEscape is a dynamic 2D infinite runner developed in Unity. Take control of a nimble cat navigating through increasingly difficult urban environments, dodging obstacles, and collecting rewards to safely find its way home.

---

## 🌟 Key Features

### 🐈 Character Selection
Choose your feline hero from the main menu before starting your journey. The game supports multiple cat characters with unique visual styles.

### 🎮 Dynamic Level & Speed System
Experience a tiered difficulty system across **5 distinct levels**, each with unique environmental themes, obstacle sets, and **progressive speed scaling**:
- **Level 1 (Mountain):** Calm start with basic hazards like Obstacle Bags.
- **Level 2 (Desert):** Increased complexity. Rewards are **locked** until 3 lethal obstacles are passed.
- **Level 3 (Graveyard):** Eerie challenges featuring the Wall obstacle and 1.7x speed.
- **Level 4 (Snow):** High-speed trek with Barbed Wire and blistering 2.1x speed.
- **Level 5 (Forest):** The ultimate challenge featuring **dynamic pits** and bridge mechanics. Rewards are rare (80-unit cooldown).

### 🧪 Potion Power-up
Find the rare **Potion Bottle** to transform your cat!
- **Growth:** Become 1.5x larger and gain a speed surge.
- **Super Jump:** Massive air-time for clearing large obstacles.
- **Heart Protection:** Active potions absorb one hit from enemies/bushes, saving your life but ending the effect.

### 🧠 Advanced Gameplay Logic
- **Skill-Lock System:** In intermediate levels, rewards (Fish/Potion) only spawn after proving mastery by jumping over **3 lethal obstacles**.
- **Lethal Recognition:** Only Enemies and Bushes count towards unlocking rewards; simple bags do not trigger the skill count.
- **Collision Polish:** Optimized interaction logic that eliminates visual gaps between the cat and obstacles for a more natural feel.

### 🛠️ Smart Physics & Environment
- **Smart Auto-Fit Colliders:** A unique algorithm calculates "Tight Bounds" for every sprite, automatically excluding transparent pixels for pixel-perfect collisions.
- **Manual Physics Overrides:** Support for per-level custom collider dimensions and **PhysicsMaterial2D** (friction/bounciness) via the Inspector.
- **Intelligent Parallax:** Multi-layered backgrounds with cinematic depth and precise alignment for seamless looping.
- **Dynamic Terrain (Level 5):** Procedural ground system that generates pits where the cat can fall (boundary clamping disabled).

---

## 🕹️ Controls

| Action | Control |
| :--- | :--- |
| **Jump** | `Space Bar`, `Up Arrow`, or `Left Mouse Click` |
| **Double Jump** | Press while in mid-air |
| **Movement** | `A/D` or `Arrow Keys` (Viewport padding floor: 1.2) |

---

## 🛠️ Technical Architecture

- **Engine:** Unity 2D (C#)
- **Weighted Spawner:** Procedural generation using level-aware probability distributions and skill-based filters.
- **JSON Persistence:** High-score data management using `JsonUtility`.
- **Theme Manager:** Centralized `LevelManager` handling sprites, scales, and audio per level theme.

---

## 🚀 Getting Started

### Prerequisites
- **Unity Editor** (Recommended: 2021.3 LTS or newer)
- **TextMeshPro** package

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/beyzkar/CatsEscape.git
   ```
2. Open the project in Unity.
3. Use the `Main Menu` scene to start your adventure.