// #define EDITOR

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_toon, specular_disabled, shadows_disabled;

varying vec3 world_normal;

void vertex() {
	world_normal = NORMAL;
}

void fragment() {
#ifdef EDITOR
	ALBEDO = vec3(1, 1, 1);
#else 
	ALBEDO = abs(world_normal);
#endif
}
