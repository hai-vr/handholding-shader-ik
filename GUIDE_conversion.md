# Conversion guide

You have a custom avatar and you want to dance with someone who also has a custom avatar?

Each of you will need to create a new, custom avatar that has been modified to use the *Handholding Shader IK* (**HSIK**). It may be difficult depending on your experience.

## How will the avatars work in VRChat?

When inside VRChat, one of the dancers is able to become the *"follower"* by making a hand gesture like *Rock and Roll*. They will lend their right hand to the *"leader"* .

The dancer who is lending their hand will lose control of their real arm, and a fake arm will follow the movement of the other avatar's left hand.

In terms of dancing, the *"leader"* has the most freedom while dancing, as their movement will not feel constrained by the *"follower"*.

The two dancers can alternatively become *"leader"* and *"follower"* by making the gesture. When both dancers are making the gesture, then both hands of both avatars will be handholding, each dancer being in control of the other dancer's right hand. 

## How does the shader look like?

This shader only contains an IK system. This system is meant to be inserted within an existing aesthetic shader such as *Poiyomi Toon Shader*, *Silent's Cel Shading Shader*...

In order for your avatar to keep its aesthetic, you need to create a copy of the aesthetic shader of your choice, and integrate *HSIK* with it. This may be difficult depending on your shader experience.

For now, **we provide a simple unlit shader** that will look okay in some worlds.

In the future we may provide existing shaders that have been modified for use with *HSIK*.

If you are up for the task, read our [Advanced shader guide](GUIDE_advanced_shader.md).

## How can I convert my avatar?

If not already done, reduce the avatar to use a few materials and ideally one mesh. Having many materials and meshes will make the job extremely difficult.

This repository has an example scene located in `Assets/ExampleModel/ExampleModelScene.unity` that you can use as a reference, and try the Editor Script conversion tool. There is another example scene located in `Assets/Possum/PossumScene.unity`.

First of all, create a copy of the avatar and make backups.

All of our examples will convert the **right** arm, therefore the guide will be written with the right arm in mind.

#### Make the real arm invisible

Before creating the fake arm, we need to create a material to make the right arm invisible when the fake arm is visible.

We will use `Assets/UnlitHideArmShader.shader` as part of this guide. Create a material with that shader, set a texture, and apply that material to your body.

If your body has multiple materials, create as many materials as there are different materials on your right arm and apply them.

On the material, keep the `Enable fake arm` setting to `1` for the time being.

#### Generate new meshes with the Editor Script

On your avatar's body, open the cog menu next to the Skinned Mesh Renderer. Use the script `Construct Fake Arm [Right] : LMT`. 

This will generate two new meshes in the `Assets/Generated` folder which you can move to another location.

![](Documentation/guide_conversion_editor_script.png)

After generating, the avatar's right arm should now be invisible.

#### Apply the material to the fake arm

Open the scene `Assets/ExampleModel/ExampleModelScene.unity`, copy one of the special point lights over to your scene, and place it anywhere within reach of the avatar's right arm.

In order to make the avatar look nice even if shaders are turned off in the safety settings, the fake arm mesh is very small by default. When shaders are turned on, the shader will scale the fake arm mesh to make it visible. Let's apply that shader on the fake arm.

We will use `Assets/ExampleUnlitHandholdingShaderIK/UnlitHandholdingShaderIK.shader` as part of this guide. Create a material with that shader, set a texture.

Expand the Armature hierarchy of your avatar until you get to the Right shoulder. There should be a *FakeRightArm* GameObject with a MeshRenderer in it.

In the inspector, apply that material to the MeshRenderer material slot. The fake arm should now be visible.

The script has removed all materials were are not assigned to the meshes of the right arm. If you have multiple materials, create as many materials as necessary. Open that material in the inspector. In the case of multiple materials, select all of them.

Choose a random number between 0.0001 and 0.9999 for this avatar (this number must be different for the other avatar you will be holding hands with). Do not use more than 4 digits for the decimal part.

- Set the material's *Target light intensity* to the negative value of that random number.  
- Set the *Intensity* value of the black light to the positive value of that random number. 

The fake arm should normally be moving in a weird way towards the light in Edit mode. You can move the light around in Edit mode to simulate the arm.

On the material, tweak the *Arm length* and *Extra forearm length* values until the arm flexing looks right, and the avatar's palm is correctly moving with the light.

*The section may be incomplete, check back soon.*

*For the time being you may open the example scene `Assets/ExampleModel/ExampleModelScene.unity` for guidance.*

#### Add a light to your other arm's palm of the same avatar

Parent the black light you created to the other arm's wrist, in our case the Left wrist. Set the light's transform values to 0, and scale to 1.

Place your editor's camera looking at the avatar from the front. Zoom into the light, then tweak the position of the light so that it hovers over the palm by about the length of the thumb.

I find that this position is versatile and will make the hand being grabbed by the partner's avatar look good in many different situations.

#### Add a gesture animation to enable or disable the fake arm

For handholding, you have two strategies:

- **Make a gesture to turn it on, handholding is off by default. This is recommended for dancing.**

- Make a gesture to turn it off, handholding is on by default. This is what we use to showcase avatars in our sample worlds.

For personal avatars, you will want to enable handholding on demand, therefore off by default. We will use that as part of this guide.

Select all materials used to hide the fake arm, and set *Enable fake arm* to 0.

Select all materials used in the fake arm, and set *Enable fake arm* to 0.

The process is similar to creating gesture animations for shape keys (facial expressions). Create an animation that: 
- sets the *_EnableFakeArm* of the *Body*'s SkinnedMeshRender to 1,
- and the *_EnableFakeArm* of the *FakeRightArm*'s MeshRenderer to 1.

Duplicate the frames to the next frame.

Set that animation in the AnimatorController of your avatar, to the gesture of your liking. I like to use RockAndRoll for dancing as it's not often a hand position that would naturally occur while dancing.

#### Hold hands with another avatar

In order to hold hands, you will need another avatar, probably the personal avatar of your dance partner.

As part of this guide, we set the *Target light intensity* to a negative value, which means it will ignore the black light of your own avatar which has the same value. This is good, as this means you will be able to hold hands with any other avatar that uses this feature.

Since creating an avatar with this feature is difficult, you have other alternatives:

- *Only add a light to the other avatar:* The avatar with the fake arm is the *follower* who will lend their arm to the *leader*. You can make the choice that only one of the two avatars will be able to become the *leader*, by only adding one light to the other avatar's left hand. Be wary that the *leader*'s VR experience is going to be drastically different from the *follower*s, as will be both of your dancing capabilities. The *leader*, being in control of the arm, can move with more freedom.

- *Upload two copies of your avatar with different light intensity values, and make at least one of them public:* By uploading a duplicate public avatar, you can let your dance partner try your avatar. If you make both of your avatars public, it will allow any other two other users to try handholding with your avatar.
