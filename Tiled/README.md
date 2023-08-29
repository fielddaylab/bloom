# Map Editing

Map editing is done using [Tiled Map Editor](https://thorbjorn.itch.io/tiled).
1. Install Tiled
2. Open `zavala.tiled_project`

### Creating a Map
1. Open `MapTemplate.tmx`
2. Save As your desired map name.
3. Alternately, create a copy of `MapTemplate.tmx` in your file browser and rename it.

### Exporting a Map
1. Select `File > Export As`
2. Save your file as a `json` file (not `tmj`).

**Note**: Once you've done this once for a given map, you can repeat this by pressing `Ctrl + E`.

### Editing a Map

#### Resizing

To resize your map, select `Map > Resize Map`.

#### Tile Type Layer

This lets you paint different terrain types onto your map.
Use the `Type` tileset. **Any other tilesets will not be recognized in this layer.**

#### Tile Height Layer

This lets you specify elevation changes in your map.
Use the `Height` tileset.

#### Objects Layer

This lets you place buildings, obstructions, and roads.
Use the `Objects` tileset.

To place objects, make sure you are in `Insert Tile` mode by pressing `T`.
Objects do not snap to the hex grid (limitation of Tiled), so make sure to place them reasonably close to the center of the desired tile.

To select and delete objects, make sure you are in `Select Object` mode by pressing `S`.
You can edit the name of your object in the property editor when an object is selected.
This name is used for referencing the object in Leaf scripts.

You can also place `Points` via `Insert Point` mode (press `I`).
These can also be named and referenced in Leaf scripts, but do not provide any functionality on their own.
Their positions, however, might be useful (ex. tiles you want to indicate for tutorial purposes - "build a digester here")