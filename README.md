# CatsEscape 🐾

CatsEscape is a dynamic 2D infinite runner developed in Unity. Take control of a nimble cat navigating through increasingly difficult urban environments, dodging obstacles, and collecting rewards to safely find its way home.

## 🌟 Key Features

### 🐈 Character Selection
Choose your feline hero from the main menu before starting your journey. The game supports multiple cat characters with unique visual styles.

### 🎮 Dynamic Level System
Experience a tiered difficulty system across **4 distinct levels**, each with unique environmental themes and obstacle sets:
- **Level 1:** Suburban start with basic hazards like Obstacle Bags.
- **Level 2:** Increased complexity with the introduction of the Bodyguard.
- **Level 3:** Urban challenges featuring the Wall obstacle, requiring precise jump timing.
- **Level 4:** The final stretch with Barbed Wire hazards—complete the goal to reach "Home."

### 📈 Advanced Scoring & Combo Mechanics
- **Distance-Based XP:** Earn experience points naturally as you travel further.
- **Bonus XP Rewards:** 
  - **Crush Bonus:** Land on top of obstacles to "crush" them and earn +20 XP.
  - **Supply Collection:** Gather cat food throughout the level for +50 XP.
- **Clean Jump Combo:** Maintain a streak of 5 successful obstacle passes without a collision to activate a score multiplier.

### 💖 Health & Vitality
- **Dynamic Hearts:** Start your run with 3 hearts.
- **Scaling Vitality:** Collect food while at full health to increase your maximum capacity (up to 5 hearts).
- **Healing:** Recover lost health by finding cat food scattered across the levels.

### 🎨 Immersive Visuals & Audio
- **Enhanced Parallax:** Multi-layered backgrounds provide deep, cinematic immersion that moves at different speeds.
- **Level-Specific Themes:** Sprites, obstacles, and backgrounds shift dynamically as you progress through different levels.
- **Premium UI:** Smooth micro-animations, vibrant color palettes, and stylized TextMeshPro elements for a high-end feel.
- **Dynamic Audio:** Adaptive background music and sound effects for jumps, level wins, and collections.

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
- **Weighted Spawner:** Procedural generation using level-aware probability distributions to ensure a fair but challenging difficulty curve.
- **Parallax System:** Automated alignment and speed scaling for background layers ensuring seamless loops.
- **Centralized Managers:** [LevelManager](cci:2://file:///Users/beyzakaraalp/Downloads/CatsEscape/Assets/Scripts/LevelManager.cs:4:0-205:1), [ScoreManager](cci:2://file:///Users/beyzakaraalp/Downloads/CatsEscape/Assets/Scripts/ScoreManager.cs:3:0-150:1), and `AudioManager` handle game state, progression, and audio feedback through efficient singleton patterns.

---

## 🚀 Getting Started

### Prerequisites
- **Unity Editor** (Recommended: 2021.3 LTS or newer)
- **TextMeshPro** package (included in project dependencies)

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/CatsEscape.git
