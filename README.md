# Unity Snippets
Repository with some handy unity snippets and tools, use at your own risk :).

## Tools:
1. Attributes:
	- HelpBoxAttribute - add an explanation to a property visible in the inspector
	- ReadOnlyAttribute - add a value in the inspector that is visible but not editable
2. Properties:
	- MinMaxRangeProperty - add a range of values, instead of using Vector2.xy for that
3. Utils:
	- Circlelizer - quickly position elements in a circle
	- ColliderUtility - remove all colliders from objects and re-add bounding box, sphere or capsule colliders
	- MeshVisualizer - show mesh vertices, edges, normals and uvs
	- MissingComponentUtility - remove all missing components from GameObjects and Prefabs automatically
	- Notes - add documentation notes directly to your game objects
	- PivotPointUtility - like 'Center on Children' but then on the bottom center ;)
	- PrefabReplacer - replaces any object in your scene with a prefab selected from a set
	- ScreenshotUtility - capture screenshots in either the game view or the scene view at the resolution of the game view
	- StagedScreenshotUtility - stage your prefabs and turn them into transparent 2D sprites automatically

## To be added soon:
1. ClassLevelTooltips - allows you to add a classlevel tooltip which shows up when you hover over the component name in the inspector
2. ExtensionClasses - useful additional methods for common classes (collections, materials, (rect) transform)
