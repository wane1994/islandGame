# Unity Terrain Island

Simple 3D Unity project with a procedural terrain island scene.

Open this folder in Unity, then open `Assets/Scenes/Island.unity`. The scene contains an `Island Scene Generator` object that creates:

- a Unity `Terrain` shaped like a small island
- painted sand, grass, and rock terrain layers
- an ocean plane
- simple rocks and palm-like props
- a third-person humanoid player
- collectible crystals, a golden finish beacon, and an in-game HUD
- patrolling enemies that damage the player
- health, win/lose screens, sound effects, a minimap, and saved best time
- a camera and sun light

Select `Island Scene Generator` and use the component values to change the seed, island size, water level, and prop counts. The context menu item `Regenerate Island` rebuilds the scene.

Press Play to run around the island:

- `WASD` or arrow keys to move
- `Left Shift` to run
- `Space` to jump
- click in the Game view to lock mouse look
- `Escape` to release the cursor

Goal:

- collect every crystal around the island
- run to the golden beacon on the hilltop to win
- avoid island sentries, because they damage your health
- beat your best saved time
