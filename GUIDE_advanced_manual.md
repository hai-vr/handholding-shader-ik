# Advanced modification guide

This an advanced guide for experienced users. You will need some experience with shaders.

If not already done, reduce the avatar to use a few materials and ideally one mesh. Having many materials and meshes will make the job extremely difficult.

## Text editor: Create a shader to hide the real arm of the main avatar

The file `ExampleUnlitHideArmShader/UnlitHideArmShader.shader` contains the minimal changes that have been applied to an Unlit shader. Edit your main shader to integrate the same changes, which summarizes to:

- Create a copy of the shader that is used by the materials of your avatar.
- Add a property to enable the fake arm.
- **For every shader pass:**
    - Add vertex color information for use by the geometry shader.
    - Add a geometry shader or edit the existing one.
    - In the geometry shader, return immediately if the fake arm is enabled AND the values of green and blue are zero.

## Text editor: Create a shader to show the fake arm

The file `ExampleUnlitHandholdingShaderIK/UnlitHandholdingShaderIK.shader` contains the minimal changes that have been applied to an Unlit shader. Edit your main shader to integrate the same changes, which summarizes to:

- Create a copy of the shader that is used by the materials of your avatar.
- **Only keep the shader pass that has `"Lightmode"="ForwardBase"` in the Tags. Delete all the other shader passes.**
- Add a property to enable the fake arm.
- Add a property to select the black light intensity value.
- Add other properties if you need them.
- Expose vertex color information to the vertex shader.
- Find a good insertion point in the existing the vertex shader.
- Scale the input vertex by 100 in order to make it visible to users who have shaders enabled.
- Include `HaiHandholdingShaderIK/HaiHandholdingShaderIK.cginc`
- Call the `transformArm` function (see example shader).
- Pass the resulting value of that function call to the rest of the shader.
- Add a geometry shader or edit the existing one.
- In the geometry shader, return immediately if the fake arm is disabled.

## Blender: Mark the arm of main avatar

In a copy of your main avatar:

- Select the vertex groups of hand & fingers, forearm and upper arm. Don't select the shoulder.
- Use Vertex painting to set these vertices RED (1.0, 0.0, 0.0).
- Save the model.
- Export this model to FBX.

## Blender: Create a fake arm

Create another copy of your main avatar:

- Pose the fingers of the hand to be just a little cramped instead of flat. You can use CATS plugin for that using Start pose mode/Apply as rest pose.

- In Edit mode:
    - Select the vertex groups of hand & fingers, forearm, upper arm, AND the shoulder.
    - Invert the selection.
    - Delete the faces.
    - Rotate the hand so that the orientation is different, as if you were grabbing a vertical pole in the subway or in the bus.
      - I recommend selecting the vertices of the hand and perform a proportional editing to twist the forearm and a very small part of the upper arm.
      - Alternatively, you may use Pose mode for that.
    - Select the vertex group of hand & fingers and forearm.
      - Set the vertex color to RED (1.0, 0.0, 0.0).
    - Select the vertex group of upper arm.
      - Set the vertex color to GREEN (0.0, 1.0, 0.0).
    - Select all vertices.
      - Perform a vertex paint smoothing.

- In Object mode:
    - Select the arm bone of the armature.
    - Use the 3D cursor and copy the values of the head of the bone (which is the rotation point between the arm and the shoulder) into the 3D cursor.
    - Set origin to the 3D cursor.
    - Clear parent and keep transform.
    - Move the object's transform position to (0, 0, 0).
    - Delete all unused materials from this object's material data.
    - Delete the armature.
    
- Save this model.

- (OPTIONAL STEP HERE WILL BE DESCRIBED LATER)

- In Edit mode:
    - Set the 3D cursor position to (0, 0, 0).
    - Scale the entire mesh in edit mode to 0.01. This will allow the mesh to remain invisible to others when shaders are hidden in the safety settings.
    
- Export this model to FBX with the same export settings as your main avatar.

## Unity: Add the fake arm

- Add the fake arm to the root of the scene.
- Give the fake arm a good name, so that we can use it later for gesture animations.
- Drag and drop the fake arm to be a child of the right upper arm.
- Set the position transform of the fake arm to (0, 0, 0).
- Now, drag and drop the fake arm to be a child of the shoulder instead.
- Disable cast shadows.
- Disable receive shadows.
- Set Override Anchor to be the hips of the avatar.
- Use **INSERT PLUGIN NAME** and multiply the bounding box values of the MeshRenderer by 100.

## Unity: Duplicate your materials

- Create two duplicates of all materials involved with the arm.
- For one of the duplicates, use the shader to hide the real arm of the main avatar.
- For the other duplicate, use the shader to show the fake arm.
- Apply the materials accordingly, to the main avatar and the fake arm.

## Choose black light intensity values

We will later add one black light per avatar with a different intensity value for each.

Choose a random value between 0.1000 and 0.9999. When both avatars have fake arms, don't use the same random value for both avatars.

## Unity: Configure the fake arm

- Create a black point light with the following settings:
  - realtime
  - radius: 0.9 (**it is important to keep the radius very small**)
  - color: (0, 0, 0, 255)
  - not important
  - intensity: the random value you chose
- Put that black light close to the right arm.
- Edit the materials to show the fake arm, and temporaily enable the fake arm in the material properties.
- Set the black light intensity in the material shader properties to the random value you chose.
- The fake arm should now be pointing towards the fake arm even when the SDK is in edit mode, try to move the light around.
- Slowly edit the arm length values until the arm movement makes sense.
- In all materials, disable the fake arm.

## Unity: Gesture animation

- Create a gesture animation (I recommend the rock and roll gesture).
- Set the main avatar skinned mesh renderer material property to enable the fake arm.
- Set the fake arm mesh renderer material property to enable the fake arm.
- Add that gesture animation to your animator controller.
- Make sure the avatar uses that animator controller.

## Unity: Create the black light on the OTHER avatar

- Create or copy the same black light as we created earlier to the OTHER avatar, with the same light intensity.
- Drag and drop the black light to be a child of the OPPOSITE hand (for instance, the left hand). That way, when both avatars are facing each other, the avatars will be holding hands (as opposed to a handshake).
- Move the black light to hover just above the palm of the hand, by about the length of the thumb.
  - If you are in possession of both the MAIN and OTHER avatar, you may try to duplicate the MAIN avatar and move it around to face that OTHER avatar in order to test it.

## Do this for both avatars

- 
