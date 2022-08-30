[![openupm](https://img.shields.io/npm/v/uk.me.paraskos.oliver.unity-fbx-collider-importer?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/uk.me.paraskos.oliver.unity-fbx-collider-importer/)

# Collider Importer

Based on https://github.com/bzgeb/AutomaticColliderGeneration

For assets which have colliders included in the FBX or Blender file with a naming convention matching [Unreal Engine FBX Static Mesh Pipeline](https://docs.unrealengine.com/4.26/en-US/WorkingWithContent/Importing/FBX/StaticMeshes/#collision)

In all cases colliders should be uniformly scaled.

### UBX_[RenderMeshName]_##
	

A Box must be created using a regular rectangular 3D object. You should not move the vertices around or deform it in any way to make it something other than a rectangular prism, only the mesh bounds are respected.

### UCP_[RenderMeshName]_##
	

A Capsule must be a cylindrical object capped with hemispheres. It does not need to have many segments (8 is a good number) at all because it is converted into a true capsule for collision. Like boxes, you should not move the individual vertices around.

### USP_[RenderMeshName]_##
	

A Sphere does not need to have many segments (8 is a good number) at all because it is converted into a true sphere for collision. Like boxes, you should not move the individual vertices around.

### UCX_[RenderMeshName]_##
	

A Convex object can be any completely closed convex 3D shape. For example, a box can also be a convex object. The diagram below illustrates what is convex and what is not: 
