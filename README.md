# 🐱 CatsEscape: A Professional 2D Runner Experience

**CatsEscape** is a high-octane, side-scrolling 2D runner built with Unity. It features a sophisticated difficulty scaling system, thematic level transitions, and precision-engineered player mechanics. Escape the obstacles, brave the enemies, and lead your cat to safety across 5 challenging levels.

---

## 🎮 Key Gameplay Features

### 🏁 5 Thematic Levels
Experience 5 unique environments, each with its own visual style, background music, and obstacle set. From tutorial-style intro levels to high-intensity final runs.

### 📈 Professional Difficulty Scaling
- **Geometric Speed Ramping:** Game speed doesn't just increase—it scales geometrically (from 0.8x to 2.6x), ensuring a rewarding learning curve.
- **Harmonic Agility:** As the world speeds up, the cat’s responsiveness (Acceleration/Turn speed) automatically scales, keeping the gameplay tight and reactive at any speed.
- **Dynamic Obstacle Density:** Obstacle spawn distances tighten as you progress, demanding quicker reflexes and better timing.

### 🐈 Advanced Player Mechanics
- **Precision Movement:** Crisp horizontal controls with physics-based acceleration and deceleration.
- **Multi-Jump System:** Single and Double jump capabilities with "Mobile-First" design in mind.
- **Horizontal Air Control:** Fine-tuned mid-air movement allows for precise landings, with horizontal speed reduced while airborne for added control.
- **Viewport Clamping:** Smart camera-clamping logic ensures the player stays within the visual frame at all times.

---

## 🛠 Technical Highlights

### 🕹️ Intelligent Spawning System (`ObstacleSpawner`)
A procedural distance-based spawning engine that manages:
- **Obstacle Variety:** Bags, Walls, Long Walls, and Bushes.
- **Enemy AI:** Level-specific enemies that walk and interact based on the game's current speed.
- **Reward Engine:** Balanced spawning of Fish (Score) and Potions (Health).

### 📐 Dynamic Theme System (`ObstacleThemeSetter`)
A robust data-driven system that updates sprites, scales, and colliders on-the-fly to match the current level's theme, ensuring visual and physical consistency.

### ❄️ Strategic "World Freeze" Mechanic
The cat can backtrack only up to **one full screen width**. If the player retreats too far, the world "freezes" (Stuck state), forcing the player to move forward to resume progress.

### 🔊 Audio Debouncing & Optimization
Collision sounds and effects are protected by a debounce timer, preventing audio stuttering and redundant logic triggers during wall interactions.

---

## ⌨️ Controls

| Action | Keyboard | Mobile |
| :--- | :--- | :--- |
| **Move Left** | `A` or `Left Arrow` | Left Side Hold |
| **Move Right**| `D` or `Right Arrow` | Right Side Hold |
| **Jump**      | `Space` or `Up Arrow`| Tap Center |

---

## 🚀 Technical Architecture
- **Language:** C#
- **Engine:** Unity 2022+
- **Rendering:** 2D Sprite-based with layering
- **Physics:** Rigidbody2D with custom kinematic-style interpolation

---

## 🐾 Developer's Note
*This project was meticulously refined to ensure every jump feels heavy, every collision feels fair, and every level feels like a new challenge. Enjoy the escape!*
