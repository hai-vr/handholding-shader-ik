#ifndef HAI_IK_ARM_EXPERIMENT
    #define HAI_IK_ARM_EXPERIMENT

float3x3 rotation(float angle, float3 axis)
{
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    float c, s;
    sincos(angle, s, c);
    float t = 1 - c;
    float tx = t * x;
    float ty = t * y;

    return float3x3(
        tx*x + c,
        tx*y - s*z,
        tx*z + s*y,
        tx*y + s*z,
        ty*y + c,
        ty*z - s*x,
        tx*z - s*y,
        ty*z + s*x,
        t*z*z + c
    );
}

float4 maybeFindMatchingLightAsLocalPosition(
    float targetLightIntensity, // when set to a negative value, it will match any black light
    float maxDistance)
{
    float4 target = float4(0, 0, 0, 0);
    float distSq = 1e+15;
    bool candidateFound = false;

    uint i;
    for (i = 0; i < 4; i++)
    {
        half4 lightColor = unity_LightColor[i];
        if (
            lightColor.r == 0 && lightColor.g == 0 && lightColor.b == 0
            && (unity_4LightPosX0[i] != 0 || unity_4LightPosY0[i] != 0 || unity_4LightPosZ0[i] != 0)
            && ((targetLightIntensity < 0 && abs(lightColor.a + targetLightIntensity) >= 0.0001) ||
                targetLightIntensity == 0 ||
                (targetLightIntensity > 0 && abs(lightColor.a - targetLightIntensity) < 0.0001))
        )
        {
            float4 current = mul ( unity_WorldToObject, float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1));
            float currentDistSq = current.x * current.x + current.y * current.y + current.z * current.z;

            if ((!candidateFound || currentDistSq < distSq) && sqrt(currentDistSq) <= maxDistance)
            {
                target = current;
                distSq = currentDistSq;
                candidateFound = true;
            }
        }
    }

    return target;
}

float3 calculateVirtualTarget(
    float3 computationLocalPos,
    float totalArmLength,
    float extraGrabLength,
    float flexBackLength,
    float defaultLength,
    float flexBackRatio,
    float4 orElseDefaultLocalPosition)
{
    float distToComputation = sqrt(computationLocalPos.x * computationLocalPos.x + computationLocalPos.y * computationLocalPos.y + computationLocalPos.z * computationLocalPos.z);
    if (distToComputation < totalArmLength)
    {
        return computationLocalPos;
    }
    else if (distToComputation < totalArmLength + extraGrabLength)
    {
        return normalize(computationLocalPos) * totalArmLength;
    }
    else if (distToComputation < totalArmLength + extraGrabLength + flexBackLength)
    {
        float lerpFactor = (distToComputation - totalArmLength - extraGrabLength) / defaultLength;
        return lerp(
            normalize(computationLocalPos) * totalArmLength,
            normalize(computationLocalPos) * totalArmLength * flexBackRatio,
            lerpFactor
        );
    }
    else if (distToComputation < totalArmLength + extraGrabLength + flexBackLength + defaultLength)
    {
        float lerpFactor = (distToComputation - totalArmLength - flexBackLength - extraGrabLength) / defaultLength;
        return lerp(
            normalize(computationLocalPos) * totalArmLength * flexBackRatio,
            orElseDefaultLocalPosition,
            lerpFactor
        );
    }
    else
    {
        // this function doesn't need to know, but this branch should not be entered in normal conditions
        // as it is already past the maxDistance when searching for lights
        return orElseDefaultLocalPosition;
    }
}

#define HAI_pi float(3.14159265359)

float acos_c(float input) { return acos(clamp(input, -1, 1)); }
float asin_c(float input) { return asin(clamp(input, -1, 1)); }

float4 transformArm(
    float4 vertex, // input vertex position
    float4 vertexColor, // input vertex color: hand and forearm are red (or blue), upperarm is green, the rest must be white
    float targetLightIntensity, // when set to a negative value, any black light will be matched
    float4 orElseDefaultLocalPosition, // hand rest position when no light matches or when it is too far
    float upperarmLength, // length of the upper arm
    float forearmLength, // length of the forearm up to the palm of the hand
    float extraGrabLength, // arm will point towards the target even when out of reach, up to this extra length limit
    float flexBackLength, // arm will lerp back away from the target over the course of this length
    float defaultLength, // arm will lerp towards the rest position over the course of this length
    float flexBackRatio, // arm will lerp back away from the target by this ratio of the total arm length (upperarmLength + forearmLength)
    bool isLeftArm) // when set to true, will mirror some rotations (for use with left arm)
{
    if (vertexColor.r > 0.7 && vertexColor.g > 0.7 && vertexColor.b > 0.7)
    {
        // ignore all vertices that are painted white
        return vertex;
    }

    float3 HAI_Y_AXIS = float3(0, 1, 0);
    float3 HAI_Z_AXIS = float3(0, 0, 1);

    bool isForearm = false;
    float forearmRatio = 0;
    if (vertexColor.r >= 0.2) // HAND + FOREARM
    {
        forearmRatio = vertexColor.r;
        isForearm = true;
    }

    // find the target
    float totalArmLength = upperarmLength + forearmLength;
    float4 maybeLocalPos = maybeFindMatchingLightAsLocalPosition(
        targetLightIntensity,
        totalArmLength + extraGrabLength + flexBackLength + defaultLength
    );
    bool lightFound = maybeLocalPos.w != 0;

    float3 targetLocalPos;
    if (lightFound) {
        float3 computationLocalPos = maybeLocalPos.xyz;
        if (isLeftArm) {
            computationLocalPos = mul( rotation(HAI_pi, HAI_Z_AXIS), computationLocalPos);
        }

        // calculate a virtual target within reach of the arm (it is assumed that orElseDefaultLocalPosition is within reach)
        targetLocalPos = calculateVirtualTarget(
             computationLocalPos,
             totalArmLength,
             extraGrabLength,
             flexBackLength,
             defaultLength,
             flexBackRatio,
             orElseDefaultLocalPosition
         );

    } else {
        targetLocalPos = orElseDefaultLocalPosition;
    }

    // calculate target attributes
    float distToTargetSq = targetLocalPos.x * targetLocalPos.x + targetLocalPos.y * targetLocalPos.y + targetLocalPos.z * targetLocalPos.z;
    float distToTarget = sqrt(distToTargetSq);

    // calculate rotation angles
    float forearmFlexAngle = acos_c((distToTargetSq - upperarmLength * upperarmLength - forearmLength * forearmLength) / (-2 * upperarmLength * forearmLength));
    float entirearmFlexAngle = asin_c((forearmLength * sin(forearmFlexAngle)) / distToTarget);

    float pitch = -atan(targetLocalPos.z / sqrt( targetLocalPos.x * targetLocalPos.x + targetLocalPos.y * targetLocalPos.y ));
    float yaw = (targetLocalPos.x < 0 ? HAI_pi : 0) + atan(targetLocalPos.y / targetLocalPos.x);
    float roll = (isLeftArm ? 1 : -1) * HAI_pi * 0.2 - yaw;

    // transform the vertex
    float3 outputVertex = vertex;
    if (isLeftArm) {
        outputVertex = mul( rotation(HAI_pi, HAI_Z_AXIS), outputVertex);
    }

    if (isForearm)
    {
        float4 ARMVEC = float4(upperarmLength, 0, 0, 0);
        outputVertex = lerp(outputVertex, mul( rotation(forearmFlexAngle + HAI_pi, HAI_Y_AXIS), outputVertex - ARMVEC ) + ARMVEC, forearmRatio);
    }

    outputVertex = mul( rotation(pitch + entirearmFlexAngle, HAI_Y_AXIS), outputVertex);
    outputVertex = mul( rotation(yaw, HAI_Z_AXIS), outputVertex);
    outputVertex = mul( rotation(roll, normalize( float4(targetLocalPos.xyz, 0) )), outputVertex);

    outputVertex = lerp(outputVertex, vertex, vertexColor.b);

    if (isLeftArm) {
        outputVertex = mul( rotation(HAI_pi, HAI_Z_AXIS), outputVertex);
    }

    return float4(outputVertex, 1);
}

// overload for backwards compatibility
float4 transformArm(float4 vertex, float4 vertexColor, float targetLightIntensity, bool mustFindClosestMatch_DEPRECATED, float4 orElseDefaultLocalPosition, float upperarmLength, float forearmLength, float extraGrabLength, bool isLeftArm)
{
    return transformArm(vertex, vertexColor, targetLightIntensity, orElseDefaultLocalPosition, upperarmLength, forearmLength, extraGrabLength, extraGrabLength, extraGrabLength, 0.95, isLeftArm);
}

#endif
