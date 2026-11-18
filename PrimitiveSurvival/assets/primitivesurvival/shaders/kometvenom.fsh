uniform float psVenomStrength = 1.0;

void main()
{
	vec2 p = uv / 4.0;
	float c = sin(p.x * cos(time / 15.0) * 80.0) + cos(p.y * cos(time / 15.0) * 10.0);
	c += sin(p.y * sin(time / 10.0) * 40.0) + cos(p.x * sin(time / 25.0) * 40.0);
	c += sin(p.x * sin(time / 5.0) * 10.0) + sin(p.y * sin(time / 4444.2222) * 80.0);
	c *= sin(time / 10.0) * 0.5;
	vec3 tint = vec3(c, c * 0.5, sin(c + time / 3.0) * 0.75);
	vec4 s = texture(scene, uv);
	color = vec4(clamp(mix(s.rgb, tint, 0.3 * psVenomStrength), 0.0, 1.0), s.a);
}
