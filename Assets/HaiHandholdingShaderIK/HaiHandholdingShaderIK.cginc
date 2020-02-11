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

float4 findMatchingLightAsLocalPosition(
    float targetLightIntensity, // when set to a negative value, it will match any black light
    bool mustFindClosestMatch,
    float4 orElseDefaultLocalPosition,
    float maxDistance)
{
    float4 target = orElseDefaultLocalPosition;
    float distSq = 1e+15;
    bool candidateFound = false;

    uint i;
    for (i = 0; i < 4; i++)
    {
        half4 lightColor = unity_LightColor[i];
        if (
            lightColor.r == 0 && lightColor.g == 0 && lightColor.b == 0
            && (targetLightIntensity < 0 || abs(lightColor.a - targetLightIntensity) < 0.001)
        )
        {
            float4 current = mul ( unity_WorldToObject, float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1));
            float currentDistSq = current.x * current.x + current.y * current.y + current.z * current.z;

            if ((!candidateFound || currentDistSq < distSq) && sqrt(currentDistSq) <= maxDistance)
            {
                if (!mustFindClosestMatch) {
                    return current;
                }

                target = current;
                distSq = currentDistSq;
                candidateFound = true;
            }
        }
    }

    return target;
}

#define HAI_pi float(3.14159265359)

float4 transformArm(
    float4 vertex, // input vertex position
    float4 vertexColor, // input vertex color: hand and forearm are red (or blue), upperarm is green, the rest must be white
    float targetLightIntensity, // when set to a negative value, any black light will be matched
    bool mustFindClosestMatch, // when set to true, target will be the closest distance to the shoulder instead of the first match
    float4 orElseDefaultLocalPosition, // hand rest position when no light matches or when it is too far
    float upperarmLength, // length of the upper arm
    float forearmLength, // length of the forearm up to the palm of the hand
    float extraGrabLength, // arm will point towards the target even when out of reach, up to this extra length limit
    bool isLeftArm) // when set to true, will mirror some rotations (for use with left arm)
{
    if (vertexColor.r > 0.7 && vertexColor.g > 0.7 && vertexColor.b > 0.7)
    {
        // ignore all vertices that are painted white
        return vertex;
    }

    bool isForearm = false;
    if (vertexColor.g == 0 && vertexColor.b == 0) // HAND + FOREARM
    {
        isForearm = true;
    }

    // calculate target attributes
    float4 targetLocalPos = findMatchingLightAsLocalPosition(
        targetLightIntensity,
        mustFindClosestMatch,
        orElseDefaultLocalPosition,
        upperarmLength + forearmLength + extraGrabLength
    );

    float distToTargetSq = targetLocalPos.x * targetLocalPos.x + targetLocalPos.y * targetLocalPos.y + targetLocalPos.z * targetLocalPos.z;
    float distToTarget = sqrt(distToTargetSq);

    // calculate rotation angles
    bool isArmFlexSolved = false;
    float forearmFlexAngle = acos((distToTargetSq - upperarmLength * upperarmLength - forearmLength * forearmLength) / (-2 * upperarmLength * forearmLength));
    float entirearmFlexAngle = asin((forearmLength * sin(forearmFlexAngle)) / distToTarget);

    if (isnan(forearmFlexAngle))
    {
        forearmFlexAngle = HAI_pi;
        entirearmFlexAngle = 0;
    }
    if (isnan(entirearmFlexAngle))
    {
        forearmFlexAngle = 0;
        entirearmFlexAngle = 0;
    }
    else
    {
        isArmFlexSolved = true;
    }

    float pitch = -atan(targetLocalPos.z / sqrt( targetLocalPos.x * targetLocalPos.x + targetLocalPos.y * targetLocalPos.y ));
    float yaw = (targetLocalPos.x < 0 ? HAI_pi : 0) + atan(targetLocalPos.y / targetLocalPos.x);
    float roll = (isLeftArm ? yaw : -yaw) - HAI_pi * 0.2;

    // transform the vertex
    float3 outputVertex = vertex;

    float3 HAI_Y_AXIS = float3(0, 1, 0);
    float3 HAI_Z_AXIS = float3(0, 0, 1);
    if (isArmFlexSolved && isForearm)
    {
        float4 ARMVEC = float4(upperarmLength, 0, 0, 0);
        outputVertex = mul( rotation(forearmFlexAngle + HAI_pi, HAI_Y_AXIS), outputVertex - ARMVEC ) + ARMVEC;
    }

    outputVertex = mul( rotation(pitch + entirearmFlexAngle, HAI_Y_AXIS), outputVertex);
    outputVertex = mul( rotation(yaw, HAI_Z_AXIS), outputVertex);
    outputVertex = mul( rotation(roll, normalize( float4(targetLocalPos.xyz, 0) )), outputVertex);

    return float4(outputVertex, 1);
}

#endif
