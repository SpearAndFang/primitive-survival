#version 330 core
#extension GL_ARB_explicit_attrib_location: enable


in vec2 uv;
out vec4 outColor;
uniform float time;
  
void main () {
	vec2 position = ( uv.xy ) / 4.0;

	float color = 0.0;
	color += sin( position.x * cos( time / 15.0 ) * 80.0 ) + cos( position.y * cos( time / 15.0 ) * 10.0 );
	color += sin( position.y * sin( time / 10.0 ) * 40.0 ) + cos( position.x * sin( time / 25.0 ) * 40.0 );
	color += sin( position.x * sin( time / 5.0 ) * 10.0 ) + sin( position.y * sin( time / 4444.2222) * 80.0 );
	color *= sin( time / 10.0 ) * 0.5;

	outColor = vec4( vec3( color, color * 0.5, sin( color + time / 3.0 ) * 0.75 ), 0.3 );

}

/*

https://shadered.org/app?fork=m35xa6rIYS

#version 330

uniform vec2 uResolution;
uniform float uTime;

//out vec4 outColor;

void main()
{
	vec3 Color = vec3(0.1, 0.4, 0.1);
	float col = -0.2;
	vec2 a = (gl_FragCoord.xy / 1.0 - uResolution/2) / min(uResolution/2, uResolution.y);
	for(float i=3.0;i<25.0;i++)
	{
	  float si = (sin(uTime + i));
	  float co = sin(cos(uTime * i * 0.07))*0.3;
	  col += 0.01 / abs(length(a+vec2(si/3,co)) - 01.5);
	}
	gl_FragColor = vec4(vec3(Color * col*2), 1);
}
*/