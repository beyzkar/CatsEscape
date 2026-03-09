# CatsEscape 🐾

CatsEscape is a dynamic 2D infinite runner developed in Unity. Take control of a nimble cat navigating through increasingly difficult urban environments, dodging obstacles, and collecting rewards to safely find its way home.

## � Key Features

### 🎮 Dynamic Progression System
Experience a tiered difficulty system across **4 distinct levels**:
- **Level 1:** Introduction to urban hazards (Obstacle Bags).
- **Level 2:** Introduces the **Bodyguard** for a higher challenge.
- **Level 3:** Adds the **Wall**, requiring precise timing and height management.
- **Level 4:** The final stretch with **Barbed Wire** obstacles. Complete the goal to reach "Home."

### 📈 Advanced Scoring & Combo Mechanics
- **Distance-Based XP:** Earn experience points as you travel further.
- **Bonus XP Rewards:**
    - **Bag Crush:** +20 XP for landing on top of obstacles.
    - **Cat Food Collection:** +50 XP for gathering supplies.
- **Clean Jump Combo:** Maintain a streak of 5 successful obstacle passes to activate a +5 XP score multiplier per jump. Streak resets upon any collision.

### 💖 Health & Survival
- **Dynamic Heart System:** Manage your health with a starting capacity of 3 hearts.
- **Scaling Vitality:** Collect `CatFood` while at full health to increase your maximum heart capacity (up to 9).
- **Healing Mechanics:** Recover lost hearts by finding food throughout the levels.

---

## 🛠️ Technical Architecture

- **Engine:** Unity 2D
- **Language:** C#
- **Core Systems:**
    - `LevelManager.cs`: Handles state machine transitions, level goals, and victory UI.
    - `ScoreManager.cs`: Centralized XP calculation, streak logic, and floating UI feedback.
    - `ObstacleSpawner.cs`: Level-aware procedural generation using a weighted probability distribution.
    - `PlayerObstacleRules.cs`: Advanced collision matrix and health management.

## 🕹️ Controls

| Action | Control |
| :--- | :--- |
| **Jump** | `Space Bar` or `Left Mouse Click` |
| **Double Jump** | Press while in mid-air |
| **Navigation** | Automatic horizontal progression |

---

## 🚀 Getting Started

### Prerequisites
- Unity Editor (Recommended: 2021.3 LTS or newer)
- TextMeshPro package (included in project dependencies)

### Installation
1.  Clone the repository:
    ```bash
    git clone https://github.com/your-username/CatsEscape.git
    ```
2.  Open the project in **Unity Hub**.
3.  Navigate to `Assets/Scenes/MainScene.unity` and press **Play**.

---

## 📝 Author
**Beyza Karaalp**
*Game Designer & Developer*

---
*Safe travels, little cat!* 🐈💨
