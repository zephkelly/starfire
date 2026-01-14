# Minimap System Setup

## Creating Default Config Assets

To use the minimap, you need to create configuration assets in Unity:

### Step 1: Create Blip Config
1. Right-click in Project window → Create → Starfire → UI → Minimap Blip Config
2. Name it `DefaultMinimapBlipConfig`
3. Configure detection level styles:
   - **Presence Style**: Small dot (6px), unknown color, 70% alpha
   - **Silhouette Style**: Medium (8px), use faction color
   - **Full Style**: Large (10px), use faction color and icons
4. Set relationship colors (red for hostile, yellow for neutral, green for allied, etc.)

### Step 2: Create Style Config
1. Right-click in Project window → Create → Starfire → UI → Minimap Style Config
2. Name it `DefaultMinimapStyleConfig`
3. Configure:
   - **Radius**: 100 (pixels)
   - **Screen Position**: (0.95, 0.95) for top-right corner
   - **Screen Offset**: (-10, -10) to pad from edge
   - **Background Color**: Dark blue with 80% alpha
   - **Border Color**: Cyan/teal
   - **Range Rings**: 2 rings, subtle color

### Step 3: Create Main Config
1. Right-click in Project window → Create → Starfire → UI → Minimap Config
2. Name it `DefaultMinimapConfig`
3. Assign the Style Config and Blip Config references
4. Configure behavior:
   - **Orientation Mode**: NorthUp (minimap fixed, player icon rotates)
   - **Edge Behavior**: HardCutoff
   - **Use Sensor Polling Rate**: true
   - **Use Sensor Range**: true
   - **Zoom Level**: 1.0

## Adding to Scene

### Option A: Add to Player
1. Select the Player GameObject
2. Add Component → MinimapManager
3. Assign the DefaultMinimapConfig

### Option B: Standalone
1. Create empty GameObject named "MinimapManager"
2. Add Component → MinimapManager
3. Assign the DefaultMinimapConfig
4. Leave Target Entity empty (will auto-find player)

## Requirements

The minimap requires:
- A player entity with `PlayerSetup` component (for auto-detection)
- The player entity must have a **Sensor Module** attached
- The player entity should have a **Transponder Module** (for faction relationship detection)

## Configuration Reference

### MinimapConfig (Main)
| Property | Description |
|----------|-------------|
| Orientation Mode | NorthUp (fixed map) or ShipUp (rotating map) |
| Edge Behavior | HardCutoff, ClampToEdge, or FadeAtEdge |
| Use Sensor Polling Rate | Sync updates to sensor refresh rate |
| Use Sensor Range | Match display range to sensor detection range |
| Zoom Level | 1 = sensor range fills display |
| Minimum Display Level | Hide contacts below this detection level |
| Show [Relationship] | Filter by faction relationship |

### MinimapStyleConfig (Visuals)
| Property | Description |
|----------|-------------|
| Radius | Size in pixels |
| Screen Position | Anchor point (0-1) |
| Screen Offset | Pixel offset from anchor |
| Show Range Rings | Display distance markers |
| Show Sweep Effect | Rotating radar sweep line |
| Sync Sweep To Sensor | Match sweep to polling rate |
| Show Stale Data Indicator | Warning when data is old |

### MinimapBlipConfig (Contacts)
| Property | Description |
|----------|-------------|
| Presence/Silhouette/Full Style | Visual settings per detection level |
| Relationship Colors | Color by faction relationship |
| Pulse New Contacts | Animation when contact appears |
| Fade On Loss | Animation when contact disappears |
