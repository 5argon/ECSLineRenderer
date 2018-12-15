# ECS LineRenderer

Pure ECS approach to render a line. One `Entity` per one line segment.

## How to use

Create an `Entity` containing `LineSegment : IComponentData` (from-to point information and line width) and `LineStyle : ISharedComponentData`, which holds reference type things like the line's material.

That `Entity` would be attached with `MeshInstanceRenderer` and `LocalToWorld` matrix to enable rendering. That matrix is then will be updated by my systems.

## How to include with GitHub functionality of Unity Package Manager

Add this line `"com.e7.ecs.linerenderer": "git://github.com/5argon/ECSLineRenderer.git",` to your packages.json

It does not update automatically when I push fixes to this repo. You must remove the lock in your Packages folder.

## Idea

Generate a quat mesh that goes 1 unit in the Z axis. Using `MeshInstanceRenderer` component and the `TransformSystem`, we could use z scale as line length, x scale as line width, position as line from, and rotation as line to. Assuming that this is only one segment of a line.

To construct complex lines, we create more `LineSegment` entity. They should be render instanced as they are using the same mesh. What's left is to wait for `MeshInstanceRendererSystem` to support material property block so we could change line color without a new material.

All lines are rotated to face the main camera in billboard rendering style.

## Info

- Line width is in Unity's standard grid unit.
- If position from and to are the same the system will not update the line. (Stays at previous position)
- It came with a bonus `LineSegmentComponent` and `LineStyleComponent` so you could play with it in the scene/edit mode with hybrid ECS.

### Systems

- `LineSegmentRegistrationSystem` : The logic which you create `LineSegment` entity should come before this system.
- `LineSegmentTransformSystem` : Update your `LineSegment` from-to location before this system's update. It will update `LocalToWorld` matrix.

## Limitations

It could not do fancy things that `LineRenderer` can do. Currently just :

- No multiple segment (per entity).
- Line width.
- Line styling by only the material.

## TODO 

- I would like to support material property block but I don't want to copy Unity's code and modify into my own, I would have to always check the system on every update and copy again if it changes. For now I want to leave it to a default `MeshInstanceRendererSystem`.

- To support arbitrary vertices rounded end cap it will break instancing as that uses a new mesh, also we could not pre generate all the meshes at `OnCreateManager`. Maybe I will just pre-generate a set of limited options to choose from instead. (e.g. 0~8 vertex rounded edge and nothing more)

- You cannot change `LineStyle` later, the copy to `MesnInstanceRenderer` component is only at the creation of entity with required components.

5argon