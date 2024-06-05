
## Description ##
Interface containing the base implementation of all CelestialBodies. Stars, planets, moons, and asteroid are all considered CelestialBodies, and must have a [[CelestialBehaviour]] component and an [[OrbitingController]].


## Properties ##
| CelestialBodyType | Returns the [[CelestialBodyType]] (read only) |
| ---- | ---- |
| OrbitingController | Returns the [[OrbitingController]] (read only) |
| ParentOrbitingBody | Returns the current parent orbiting body of this object. Returns null if object has no parent. (read only) |
| ChildOrbitingBody | Returns the current child orbiting body of this object. Returns null if object has no child. (read only) |
| MaxOrbitRadius | Returns the maximum radius that any object can orbit the celestial body. Currently set in the Awake method of each class which inherits [[CelestialBehaviour]]. (read only) |
| IsOrbiting | Returns a boolean which is true if the object is currently orbiting another [[ICelestialBody]]. (read only) |
| Mass | Returns a float of the mass of the rigidbody attached to the celestial body object. For stars, this mass is multiplied by 10 in order to make. (read only) |
| CelestialName | Returns a string containing the randomly generated name of the celestial body. |


## Methods ##
| SetupCelestialBehaviour() | By providing a [[CelestialBodyType]], a radius, and a name, any class extending [[CelestialBehaviour]] can have its properties set on initilisation. Since objects are unloaded with the chunks, the GameObjects must be pooled and then setup on demand. |
| --- | --- |
| SetOrbitingBody() | Sets the parent orbiting body of a celestial body. |
