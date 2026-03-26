# CatsEscape 🐾

CatsEscape is a dynamic 2D infinite runner developed in Unity. Take control of a nimble cat navigating through increasingly difficult urban environments, dodging obstacles, and collecting rewards to safely find its way home.

## 🌟 Key Features

### 🐈 Character Selection
Choose your feline hero from the main menu before starting your journey. The game supports multiple cat characters with unique visual styles.

### 🎮 Dynamic Level & Speed System
Experience a tiered difficulty system across **5 distinct levels**, each with unique environmental themes, obstacle sets, and **progressive speed scaling**:
- **Level 1 (Mountain):** Calm start with basic hazards like Obstacle Bags.
- **Level 2 (Desert):** Increased complexity and speed (1.35x). Bodyguards introduced.
- **Level 3 (Graveyard):** Eerie challenges featuring the Wall obstacle and 1.7x speed.
- **Level 4 (Snow):** The final trek with Barbed Wire and blistering 2.1x speed.
- **Level 5 (Forest):** The ultimate challenge with dynamic pits and bridge mechanics at 2.5x speed.

### 🧪 Potion Power-up
Find the rare **Potion Bottle** to transform your cat!
- **Growth:** Become 1.5x larger.
- **Super Jump:** Jump height is increased by 1.5x for massive air-time.
- **Speed Surge:** Gain a speed multiplier relative to your current level.
- *Note: The effect is lost instantly if you collide with any obstacle.*

### 🛠️ Enhanced Background & Terrain
- **Intelligent Parallax:** Multi-layered backgrounds with cinematic depth, featuring auto-scaling to camera height and precise layer alignment for seamless looping.
- **Dynamic Terrain Spawner:** Level 5 introduces a modular ground system that procedurally generates pits and bridges, ensuring a unique path in every run.
- **Theme-Aware Obstacles:** Obstacles like Bags and Walls automatically adjust their visuals, scales, and offsets to match the current level's theme.
- **Custom Sound Effects:** Unique SFX for jumping, growth/shrink effects, and UI interactions.

---

## 🕹️ Controls

| Action | Control |
| :--- | :--- |
| **Jump** | `Space Bar`, `Up Arrow`, or `Left Mouse Click` |
| **Double Jump** | Press while in mid-air |
| **Start/Restart** | via UI Buttons |

---

## 🛠️ Technical Architecture

- **Engine:** Unity 2D (C#)
- **Weighted Spawner:** Procedural generation using level-aware probability distributions for obstacles, fish, and potion pickups.
- **JSON Persistence:** High-score data management using `JsonUtility` and `System.IO`.
- **Centralized Managers:** `LevelManager`, `ScoreManager`, `LeaderboardManager`, and `AudioManager` handle game state and progression using efficient singleton patterns.

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
