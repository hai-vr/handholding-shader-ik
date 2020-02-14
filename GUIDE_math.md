# Math behind the shader

This guide is targeting shader developers who wish to create a derivative of this shader, or enhance its functionality.

Handholding Shader IK has been created with dancing in mind, meaning it works nicely for most dancing applications, especially while dancing face to face.

That means its algorithm may not be suitable for other applications. However, with a few tweaks, you may be able to adapt it for other uses.

# Breakdown

The arm is a MeshRenderer with the origin located at the arm/shoulder rotation point, parented to the shoulder. It is on the horizontal plane so that the arm is going along the X axis, assuming the Z axis is going upwards.

The aim is to rotate the forearm and the arm in such a way that the hand looks is roughly near a target point in world space.

#### `// find the target`

- First I'm going to convert that real target point from world space to local space and do everything in local space from now on, because the IK has to work no matter what the orientation of the upper body is (standing up, leaning, lying down).

#### `// calculate a virtual target within reach of the arm`

- Then using this real target in local space, we can calculate a target within the range of the arm. There is a small algorithm that alters the position of virtual target if the target is outside the range of the arm.

    - Calculate the distance between the origin (shoulder) and the real target.
    - If the distance is lower than the total arm length, keep it.
    - Else, if the distance is lower than the total arm length + `extraGrabLength`, set the target distance be exactly the length of the arm.
    - Else, if the distance is lower than the total arm length + `extraGrabLength + flexBackLength`, lerp between the above position, towards the target distance being exactly the length of the arm multiplied by `flexBackRatio`.
    - Else, if the distance is lower than the total arm length + `extraGrabLength + flexBackLength + defaultLength`, lerp between the above position, towards the position described in `orElseDefaultLocalPosition`.
    - Else, the target will be `orElseDefaultLocalPosition`.

#### `// calculate rotation angles`

- I have 3 known distances: The distance between the origin (shoulder) and the target, the length of my arm `upperarmLength`, and the length of my forearm `forearmLength`, which forms a triangle. I can use these known lengths to calculate two angles `forearmFlexAngle` which is the only obtuse angle between the forearm and the arm, and `entirearmFlexAngle` which is the angle between the target and the elbow (using the [law of cosines](https://en.wikipedia.org/wiki/Law_of_cosines)).

- Since I know the X, Y, and Z coordinates of the target in local space, I'm going to calculate `pitch` which is the angle between the horizon and the target in a plane that includes the Z axis (using [basic trigonometry for right triangles](https://en.wikipedia.org/wiki/Trigonometric_functions#Right-angled_triangle_definitions)).

- I can rotate the forearm (isolated using vertex colors) by `forearmFlexAngle` around the elbow (rotation point is the known arm length), and the entire arm by `pitch + entirearmFlexAngle` ; so we took care of the pitch.

- We can take care of the yaw by calculating the angle between the X axis and the target in the horizon plane as `yaw`, and rotate the entire arm by that angle.

- For effect, I'll roll the rotation of the arm around the axis between the target and the origin. For now, `roll` is a function of the yaw `yaw` which looks good in most of the cases except when the hand is on the hip, or behind the back, and some others.

# Left arm

To support the left arm, the target is being emulated as a right arm by rotating the local position.

All the calculations are done as if it were a right arm.

We rotate the arm before applying the transformations, and rotate the arm again in the opposite direction after transformations are complete.

This strategy might be usable so that the shader might be used on any axis.
