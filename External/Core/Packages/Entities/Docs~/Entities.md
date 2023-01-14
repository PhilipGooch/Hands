# Entities

Entities is a lightweight implementation of Entity Component System. For more in-depth documentation on how Entity Component System works, refer to [Entity Component System | Entities | 0.17.0-preview.42 (unity3d.com)](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/index.html)

### Entities 

Entities are referenced using the Entity structure that contains entity id.  Methods manipulating entities are:

* EntityStore.AddEntity() - creates an entity without data
* EntityStore.AddEntity(EntityArchetype) - creates an entity with specified data layout (Archetype)
* EntityStore.RemoveEntity(Entity) - deletes an entity
* EntityStore.GetEntityReference(Entity) - returns a reference to entity allowing access to multiple data components

### Object Components

Although primary purpose of entity is storing BURST compatible data components, it also provides a container to store managed objects. It is a fast way to achieve functionality similar to Unity GetComponent<T>, allowing to group MonoBehaviors scattered across multiple GameObject that form single logical entity, or keeping reference to non MonoBehavior objects. 

* EntityStore.GetComponentObject<T>(Entity) - get a reference to a managed component
* EntityReference..GetComponentObject<T>() - get a reference to a managed component 
* EntityObjectStore.GetComponentObject<T>() - get a reference to a managed component
* EntityStore.AddComponentObject<T>(Entity,T) - add a reference to managed component
* EntityStore.RemoveComponentObject<T>(Entity,T) - unlists component from an eneity.
* EntityStore.GetObjectStore(Entity) - returns EntityObjectStore for an entity speeding up the access to multiple object components.

Currently component objects are limited to one instance per type per entity. 

### Data Components

Entities store data in stucts called data components. Data components are accessed either using EntityStore.GetComponentData<T> or EntityReference.GetComponentData<T>. EntityReference caches all data needed to access components so is a preferred way when multiple components need to be accessed.

* EntityStore.GetComponentData<T> (Entity) - returns a reference to a data component
* EntityReference.GetComponentData<T>() - returns a reference to a data component
* EntityStore.AddComponents(Entity, ComponentTypeList) - allocates space to store listed components for an entity

Before accessing data components, they must be added using EntityStore.AddComponents, or be part of the entity archetype passed to EntityStore.AddEntity.

### EntityArchetype & ComponentTypeList

A set of all components on entity defines it's data structure and is called Archetype. All entities of same archetype are stored in a memory chunk together. ComponentType is a burst compatible reference to managed type of a structure - it's represented by it's hash, a collection of ComponentTypes (or ComponentTypeList) is unordered set of types used to define entity data layout or EntityArchetype. 

* ComponentTypeList.Create - allocates empty type list
* ComponentTypeList.AddType<T> - adds a type to a list
* ComponentTypeList.AddType(ComponentType) - adds a type to a list
* ComponentType.FromType<T>() - returns BURST compatible type reference for a managed structure type
* EntityStore.RegisterArchetype(maxEntities, ComponentTypeList) - registers an archetype (or template) for an entity defining a list of components forming entity data layout,
* EntityStore.UnregisterArchetype(EntityArchetype) - removes archetype and deletes all entities.

### Tips

Although components can be added using EntityStore.AddComponents, the preferred way is creating entities from EntityArchetype as if avoids moving the entity between memory chunks.

Use EntityStore.GetComponentData <T> when single data component is accessed by a system. EntityStore.GetEntityReference and EntityReference.GetComponentData speed up component access when multiple data components are needed.

Don't cache EntityReference - it may become invalid if components are added to the entity.

## Queries

Entity store can be queried for entities containing certain data components using EntityStore.Query. This returns EntityQuery that can be executed by calling EntityQuery.Execute to retrieve EntityQueryResults. Recommended pattern is caching an EntityQuery on initialize and executing it each frame to access the most recent EntityQueryResults. EntityQueryResults should not be cached as they are invalidated when entityies are created/delete, components added.

* EntityStore.Query<T> - returns a EntityQuery that can be used to list all entities having data component T
* EntityStore.Query<T1,T2....T7> - query having all of the required components
* EntityQuery.Execute - retrieve EntityQeury results matching the query,
* EntityQueryResults.count - number of results returned by query
* EntityQueryResults.GetEntityReference(index) - return a reference to item in the query
* EntityQueryResults.GetComponentData<T>(index) - return a reference to a data component of specified item in the query
* EntityQueryResults.GetObjectData<T>(index) - retrieve component object for specified item in the query