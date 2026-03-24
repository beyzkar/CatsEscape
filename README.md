# CatsEscape 🐾

CatsEscape is a dynamic 2D infinite runner developed in Unity. Take control of a nimble cat navigating through increasingly difficult urban environments, dodging obstacles, and collecting rewards to safely find its way home.

## 🌟 Key Features

### 🐈 Character Selection
Choose your feline hero from the main menu before starting your journey. The game supports multiple cat characters with unique visual styles.

### 🎮 Dynamic Level & Speed System
Experience a tiered difficulty system across **4 distinct levels**, each with unique environmental themes, obstacle sets, and **progressive speed scaling**:
- **Level 1:** Suburban start with basic hazards like Obstacle Bags.
- **Level 2:** Increased complexity and speed (1.35x). Bodyguards introduced.
- **Level 3:** Challenges featuring the Wall obstacle and 1.7x speed.
- **Level 4:** The final stretch with Barbed Wire and blistering 2.1x speed. Complete the goal to reach "Home."

### 🧪 Potion Power-up
Find the rare **Potion Bottle** to transform your cat!
- **Growth:** Become 1.5x larger.
- **Super Jump:** Jump height is increased by 1.5x for massive air-time.
- **Speed Surge:** Gain a speed multiplier relative to your current level.
- *Note: The effect is lost instantly if you collide with any obstacle.*

### 🏆 Persistent Leaderboard
Compete for the top spot! The game features a persistent "Hall of Fame":
- **Save Score:** Enter your name after every run to see if you make the Top 5.
- **JSON Persistence:** Scores are saved locally in a JSON format (`leaderboard.json`), ensuring your records survive across sessions.

### 📈 Advanced Scoring & Combo Mechanics
- **Distance-Based XP:** Earn experience points naturally as you travel further.
- **Bonus XP Rewards:** 
  - **Crush Bonus:** Land on top of obstacles to "crush" them for +20 XP.
  - **Supply Collection:** Gather cat food for +50 XP.
- **Clean Jump Combo:** Maintain a streak of 5 successful obstacle passes without collision to activate a score multiplier.

### 💖 Health & Vitality
- **Dynamic Hearts:** Start with 3 hearts and collect food to heal or expand your capacity up to 5 hearts.

### 🎨 Immersive Visuals & Audio
- **Enhanced Parallax:** Multi-layered backgrounds with cinematic depth.
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
