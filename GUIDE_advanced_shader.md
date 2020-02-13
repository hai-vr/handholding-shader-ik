# Advanced shader guide

This an advanced guide for experienced users that want to integrate Handholding Shader IK into an existing shader (such as *Poiyomi Toon Shader* or *Silent's Cel Shading Shader*). You will need some experience with shaders.

## Create a shader to hide the real arm of the main avatar

The file `ExampleUnlitHideArmShader/UnlitHideArmShader.shader` contains the minimal changes that have been applied to an Unlit shader. Edit your main shader to integrate the same changes, which summarizes to:

- Create a copy of the shader that is used by the materials of your avatar.
- Add a property to enable the fake arm.
- **For every shader pass:**
    - Add vertex color information for use by the geometry shader.
    - Add a geometry shader or edit the existing one.
    - In the geometry shader:
        - if the fake RIGHT arm is enabled AND the values of green and blue are zero, return immediately.
        - if the fake LEFT arm is enabled AND the values of green and red are zero, return immediately.

## Create a shader to support the fake arm

The file `ExampleUnlitHandholdingShaderIK/UnlitHandholdingShaderIK.shader` contains the minimal changes that have been applied to an Unlit shader. Edit your main shader to integrate the same changes, which summarizes to:

- Create a copy of the shader that is used by the materials of your avatar.
- **Only keep the shader pass that has `"Lightmode"="ForwardBase"` in the Tags, or add it if none exists. Delete all the other shader passes that have a different `"Lightmode"`.**
- Add a property to enable the fake arm.
- Add a property to select the black light intensity value.
- Add other properties if you need them.
- Expose vertex color information to the vertex shader.
- Find a good insertion point in the existing the vertex shader.
- Scale the input vertex by 1024 in order to make it visible to users who have shaders enabled.
- Include `HaiHandholdingShaderIK/HaiHandholdingShaderIK.cginc`
- Call the `transformArm` function (see example shader).
- Pass the resulting value of that function call to the rest of the shader.
- Add a geometry shader or edit the existing one.
- In the geometry shader, return immediately if the fake arm is disabled.
