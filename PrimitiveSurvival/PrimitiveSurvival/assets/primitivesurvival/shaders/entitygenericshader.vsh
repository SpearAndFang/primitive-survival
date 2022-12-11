#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
layout(location = 2) in vec4 colorIn;
layout(location = 3) in int flags;
layout(location = 4) in int jointId;

uniform vec3 rgbaAmbientIn;
uniform vec4 rgbaLightIn;
uniform vec4 rgbaFogIn;
uniform float fogMinIn;
uniform float fogDensityIn;
uniform vec4 renderColor;
uniform int addRenderFlags;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

uniform int extraGlow;
uniform int skipRenderJointId;
uniform int skipRenderJointId2;
uniform mat4 elementTransforms[35];

out vec2 uv;
out vec4 color;
out vec4 rgbaFog;
out float fogAmount;


out vec3 normal;
#if SSAOLEVEL > 0
out vec4 fragPosition;
out vec4 gnormal;
#endif


#include vertexflagbits.ash
#include shadowcoords.vsh
#include fogandlight.vsh
#include vertexwarp.vsh

void main(void)
{
	mat4 animModelMat = modelMatrix * elementTransforms[jointId];
	vec4 worldPos = animModelMat * vec4(vertexPositionIn, 1.0);
	
	worldPos = applyVertexWarping(flags | addRenderFlags, worldPos);
	
	// Cheap fix to "not render" head in first person mode
	if (jointId == skipRenderJointId || jointId == skipRenderJointId2) {
		worldPos.y -= 10000;
	}
	
	vec4 cameraPos = viewMatrix * worldPos;

	uv = uvIn;
	int renderFlags = extraGlow + flags;
	color = renderColor * colorIn * applyLight(rgbaAmbientIn, rgbaLightIn, renderFlags, cameraPos);
	rgbaFog = rgbaFogIn;
	
	// Distance fade out
	color.a *= clamp(20 * (1.05 - length(worldPos.xz) / viewDistance) - 5, -1, 1);
	
	gl_Position = projectionMatrix * cameraPos;
	calcShadowMapCoords(viewMatrix, worldPos);
	
	fogAmount = getFogLevel(worldPos, fogMinIn, fogDensityIn);
	
	normal = unpackNormal(renderFlags);
	normal = (animModelMat * vec4(normal.x, normal.y, normal.z, 0)).xyz;
	
	#if SSAOLEVEL > 0
		fragPosition = cameraPos;
		gnormal = viewMatrix * vec4(normal, 0);
	#endif
    

}
